using ClaimsService.Data;
using ClaimsService.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimsService.Services;

public sealed class ClientProfileOutboxStore(ClaimsDbContext context) : IClientProfileOutboxStore
{
    private const int MaximumRetryDelaySeconds = 60;

    public Task<OutboxMessage?> GetNextMessageAsync(CancellationToken cancellationToken) =>
        context.OutboxMessages
            .AsNoTracking()
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task MarkProcessedAsync(long id, CancellationToken cancellationToken)
    {
        var message = await context.OutboxMessages.SingleAsync(
            item => item.Id == id,
            cancellationToken);

        message.ProcessedAtUtc = DateTime.UtcNow;
        message.LastError = null;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OutboxRetryState> RecordFailureAsync(
        long id,
        string error,
        CancellationToken cancellationToken)
    {
        var message = await context.OutboxMessages.SingleAsync(
            item => item.Id == id,
            cancellationToken);

        message.AttemptCount++;
        message.LastError = error.Length <= 2048 ? error : error[..2048];
        var delaySeconds = (int)Math.Min(
            MaximumRetryDelaySeconds,
            Math.Pow(2, Math.Min(message.AttemptCount, 6)));
        message.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
        await context.SaveChangesAsync(cancellationToken);

        return new OutboxRetryState(message.AttemptCount, delaySeconds);
    }
}
