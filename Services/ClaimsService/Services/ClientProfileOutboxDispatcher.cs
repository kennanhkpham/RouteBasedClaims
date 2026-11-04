using System.Net.Http.Json;
using System.Text.Json;
using ClaimsService.Dtos;
using ClaimsService.Models;
using Microsoft.Data.SqlClient;

namespace ClaimsService.Services;

public sealed class ClientProfileOutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<ClientProfileOutboxDispatcher> logger) : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consecutiveDatabaseFailures = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await GetNextMessageAsync(stoppingToken);
                if (message is null)
                {
                    consecutiveDatabaseFailures = 0;
                    await Task.Delay(IdleDelay, stoppingToken);
                    continue;
                }

                var wait = message.NextAttemptAtUtc - DateTime.UtcNow;
                if (wait > TimeSpan.Zero)
                {
                    consecutiveDatabaseFailures = 0;
                    await Task.Delay(wait, stoppingToken);
                    continue;
                }

                try
                {
                    await DeliverAsync(message, stoppingToken);
                }
                catch (HttpRequestException exception)
                {
                    await RecordFailureAsync(message, exception, stoppingToken);
                    consecutiveDatabaseFailures = 0;
                    continue;
                }
                catch (TaskCanceledException exception) when (!stoppingToken.IsCancellationRequested)
                {
                    await RecordFailureAsync(message, exception, stoppingToken);
                    consecutiveDatabaseFailures = 0;
                    continue;
                }

                await MarkProcessedAsync(message.Id, stoppingToken);
                consecutiveDatabaseFailures = 0;
            }
            catch (Exception exception) when (IsTransientDatabaseFailure(exception))
            {
                consecutiveDatabaseFailures++;
                var retryDelay = GetDatabaseRetryDelay(consecutiveDatabaseFailures);

                logger.LogWarning(
                    exception,
                    "Transient database failure in the client profile outbox dispatcher; retrying after {RetryDelaySeconds} seconds.",
                    retryDelay.TotalSeconds);

                await Task.Delay(retryDelay, stoppingToken);
            }
        }
    }

    private async Task<OutboxMessage?> GetNextMessageAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IClientProfileOutboxStore>();
        return await store.GetNextMessageAsync(cancellationToken);
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
        var store = scope.ServiceProvider.GetRequiredService<IClientProfileOutboxStore>();
        await store.MarkProcessedAsync(id, cancellationToken);
    }

    private async Task RecordFailureAsync(
        OutboxMessage failedMessage,
        Exception exception,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IClientProfileOutboxStore>();
        var retry = await store.RecordFailureAsync(
            failedMessage.Id,
            exception.Message,
            cancellationToken);

        logger.LogError(
            exception,
            "Client profile outbox message {OutboxMessageId} failed on attempt {AttemptCount}; retrying after {RetryDelaySeconds} seconds.",
            failedMessage.Id,
            retry.AttemptCount,
            retry.DelaySeconds);
    }

    private static TimeSpan GetDatabaseRetryDelay(int failureCount)
    {
        var delaySeconds = Math.Min(30, Math.Pow(2, Math.Min(failureCount - 1, 5)));
        return TimeSpan.FromSeconds(delaySeconds);
    }

    private static bool IsTransientDatabaseFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException)
            {
                return true;
            }

            if (current is SqlException sqlException &&
                (sqlException.IsTransient || sqlException.Errors.Cast<SqlError>().Any(error => error.Number == -2)))
            {
                return true;
            }
        }

        return false;
    }
}
