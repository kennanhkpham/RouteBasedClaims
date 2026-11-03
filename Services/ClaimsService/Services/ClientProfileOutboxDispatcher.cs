using System.Net.Http.Json;
using System.Text.Json;
using ClaimsService.Data;
using ClaimsService.Dtos;
using ClaimsService.Models;
using Microsoft.EntityFrameworkCore;

namespace ClaimsService.Services;

public sealed class ClientProfileOutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<ClientProfileOutboxDispatcher> logger) : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);
    private const int MaximumRetryDelaySeconds = 60;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = await GetNextMessageAsync(stoppingToken);
            if (message is null)
            {
                await Task.Delay(IdleDelay, stoppingToken);
                continue;
            }

            var wait = message.NextAttemptAtUtc - DateTime.UtcNow;
            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, stoppingToken);
                continue;
            }

            try
            {
                await DeliverAsync(message, stoppingToken);
                await MarkProcessedAsync(message.Id, stoppingToken);
            }
            catch (HttpRequestException exception)
            {
                await RecordFailureAsync(message, exception, stoppingToken);
            }
            catch (TaskCanceledException exception) when (!stoppingToken.IsCancellationRequested)
            {
                await RecordFailureAsync(message, exception, stoppingToken);
            }
        }
    }

    private async Task<OutboxMessage?> GetNextMessageAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();

        return await context.OutboxMessages
            .AsNoTracking()
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task DeliverAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<ClientProfileSyncMessage>(outboxMessage.Payload)
            ?? throw new InvalidDataException(
                $"Outbox message {outboxMessage.Id} contains an empty client profile payload.");
        var client = httpClientFactory.CreateClient("clients-service");
        var path = $"internal/clients/{Uri.EscapeDataString(message.PolicyNumber)}";

        using var request = outboxMessage.EventType switch
        {
            ClientProfileEventTypes.Upsert => new HttpRequestMessage(HttpMethod.Put, path)
            {
                Content = JsonContent.Create(new
                {
                    fullName = message.FullName,
                    email = message.Email
                })
            },
            ClientProfileEventTypes.Delete => new HttpRequestMessage(HttpMethod.Delete, path),
            _ => throw new InvalidDataException(
                $"Outbox message {outboxMessage.Id} has unsupported event type '{outboxMessage.EventType}'.")
        };

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task MarkProcessedAsync(long id, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
        var message = await context.OutboxMessages.SingleAsync(
            item => item.Id == id,
            cancellationToken);

        message.ProcessedAtUtc = DateTime.UtcNow;
        message.LastError = null;
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordFailureAsync(
        OutboxMessage failedMessage,
        Exception exception,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
        var message = await context.OutboxMessages.SingleAsync(
            item => item.Id == failedMessage.Id,
            cancellationToken);

        message.AttemptCount++;
        message.LastError = exception.Message.Length <= 2048
            ? exception.Message
            : exception.Message[..2048];
        var delaySeconds = Math.Min(
            MaximumRetryDelaySeconds,
            Math.Pow(2, Math.Min(message.AttemptCount, 6)));
        message.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogError(
            exception,
            "Client profile outbox message {OutboxMessageId} failed on attempt {AttemptCount}; retrying after {RetryDelaySeconds} seconds.",
            message.Id,
            message.AttemptCount,
            delaySeconds);
    }
}
