using ClaimsService.Models;

namespace ClaimsService.Services;

public interface IClientProfileOutboxStore
{
    Task<OutboxMessage?> GetNextMessageAsync(CancellationToken cancellationToken);

    Task MarkProcessedAsync(long id, CancellationToken cancellationToken);

    Task<OutboxRetryState> RecordFailureAsync(
        long id,
        string error,
        CancellationToken cancellationToken);
}

public sealed record OutboxRetryState(int AttemptCount, int DelaySeconds);
