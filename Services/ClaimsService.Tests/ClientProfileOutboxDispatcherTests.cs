using System.Net;
using System.Net.Http;
using System.Text.Json;
using ClaimsService.Dtos;
using ClaimsService.Models;
using ClaimsService.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ClaimsService.Tests;

public sealed class ClientProfileOutboxDispatcherTests
{
    [Fact]
    public async Task DeliversAndMarksAnOutboxMessageOnce()
    {
        var store = new FakeOutboxStore(CreateMessage());
        var handler = new TrackingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        using var provider = CreateProvider(store, handler);
        var dispatcher = CreateDispatcher(provider);

        await dispatcher.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => store.MarkProcessedCount == 1);
        await StopDispatcherAsync(dispatcher);

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(0, store.RecordFailureCount);
    }

    [Fact]
    public async Task KeepsRunningWhenSqlIsUnavailableDuringStartup()
    {
        var store = new FakeOutboxStore(null, new TimeoutException("Test SQL unavailable."), alwaysFailPoll: true);
        using var provider = CreateProvider(store, new TrackingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent))));
        var dispatcher = CreateDispatcher(provider);

        await dispatcher.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => store.PollFailureCount >= 2);

        Assert.False(dispatcher.ExecuteTask?.IsCompleted);
        await StopDispatcherAsync(dispatcher);
    }

    [Fact]
    public async Task RecoversAfterTransientSqlFailureDuringPolling()
    {
        var store = new FakeOutboxStore(null, new TimeoutException("Test SQL timeout."));
        using var provider = CreateProvider(store, new TrackingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent))));
        var dispatcher = CreateDispatcher(provider);

        await dispatcher.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => store.SuccessfulPollCount >= 2 && store.PollFailureCount == 1);
        await StopDispatcherAsync(dispatcher);

        Assert.True(store.PollCount >= 2);
    }

    [Fact]
    public async Task RetriesPollingAfterSqlCommandTimeout()
    {
        var store = new FakeOutboxStore(
            null,
            new TimeoutException("Test command timeout."),
            failAfterFirstSuccessfulPoll: true);
        using var provider = CreateProvider(store, new TrackingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent))));
        var dispatcher = CreateDispatcher(provider);

        await dispatcher.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => store.SuccessfulPollCount >= 2 && store.PollFailureCount == 1);
        await StopDispatcherAsync(dispatcher);

        Assert.Equal(1, store.PollFailureCount);
        Assert.True(store.PollCount >= 2);
    }

    [Fact]
    public async Task StopsPromptlyWhenShutdownOccursDuringDatabaseRetryDelay()
    {
        var store = new FakeOutboxStore(null, new TimeoutException("Test SQL timeout."), alwaysFailPoll: true);
        using var provider = CreateProvider(store, new TrackingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent))));
        var dispatcher = CreateDispatcher(provider);

        await dispatcher.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => store.PollFailureCount > 0);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await dispatcher.StopAsync(timeout.Token);

        Assert.False(timeout.IsCancellationRequested);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task DoesNotMarkMessageProcessedWhenDeliveryFails()
    {
        var store = new FakeOutboxStore(CreateMessage());
        var handler = new TrackingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        using var provider = CreateProvider(store, handler);
        var dispatcher = CreateDispatcher(provider);

        await dispatcher.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => store.RecordFailureCount == 1);
        await StopDispatcherAsync(dispatcher);

        Assert.Equal(0, store.MarkProcessedCount);
        Assert.Equal(1, store.AttemptCount);
    }

    [Fact]
    public async Task DoesNotSwallowInvalidOutboxPayloads()
    {
        var message = CreateMessage();
        message.Payload = "{";
        var store = new FakeOutboxStore(message);
        using var provider = CreateProvider(store, new TrackingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent))));
        var dispatcher = CreateDispatcher(provider);

        await dispatcher.StartAsync(CancellationToken.None);
        var executeTask = dispatcher.ExecuteTask
            ?? throw new InvalidOperationException("The dispatcher did not start.");

        await Assert.ThrowsAsync<JsonException>(() => executeTask);
        Assert.Equal(0, store.MarkProcessedCount);
    }

    [Fact]
    public async Task RetriesDeliveryIfProcessedMarkerPersistenceFailsAfterSuccessfulDelivery()
    {
        var store = new FakeOutboxStore(CreateMessage(), failFirstProcessedWrite: true);
        var handler = new TrackingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));
        using var provider = CreateProvider(store, handler);
        var dispatcher = CreateDispatcher(provider);

        await dispatcher.StartAsync(CancellationToken.None);
        await WaitUntilAsync(() => store.MarkProcessedCount == 1);
        await StopDispatcherAsync(dispatcher);

        Assert.Equal(2, handler.RequestCount);
        Assert.Equal(1, store.MarkProcessedCount);
    }

    private static OutboxMessage CreateMessage() => new()
    {
        Id = 1,
        EventType = ClientProfileEventTypes.Upsert,
        Payload = JsonSerializer.Serialize(
            new ClientProfileSyncMessage("TEST-POLICY", "Test Client", "test@example.invalid")),
        NextAttemptAtUtc = DateTime.UtcNow
    };

    private static ServiceProvider CreateProvider(
        FakeOutboxStore store,
        TrackingHandler handler)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IClientProfileOutboxStore>(store);
        services.AddHttpClient("clients-service", client =>
            client.BaseAddress = new Uri("http://clients-service/"))
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider();
    }

    private static ClientProfileOutboxDispatcher CreateDispatcher(IServiceProvider provider) =>
        new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<ILogger<ClientProfileOutboxDispatcher>>());

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }

    private static async Task StopDispatcherAsync(ClientProfileOutboxDispatcher dispatcher)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await dispatcher.StopAsync(timeout.Token);
        Assert.False(timeout.IsCancellationRequested);
    }

    private sealed class FakeOutboxStore(
        OutboxMessage? pendingMessage,
        Exception? pollFailure = null,
        bool alwaysFailPoll = false,
        bool failFirstProcessedWrite = false,
        bool failAfterFirstSuccessfulPoll = false) : IClientProfileOutboxStore
    {
        private readonly object _lock = new();
        private int _pollCount;
        private int _successfulPollCount;
        private int _pollFailureCount;
        private int _markProcessedCount;
        private int _recordFailureCount;
        private int _attemptCount;
        private bool _processed;
        private bool _pollFailureReturned;
        private bool _processedWriteFailureReturned;
        private DateTime _nextAttemptAtUtc = pendingMessage?.NextAttemptAtUtc ?? DateTime.UtcNow;

        public int PollCount => Volatile.Read(ref _pollCount);
        public int SuccessfulPollCount => Volatile.Read(ref _successfulPollCount);
        public int PollFailureCount => Volatile.Read(ref _pollFailureCount);
        public int MarkProcessedCount => Volatile.Read(ref _markProcessedCount);
        public int RecordFailureCount => Volatile.Read(ref _recordFailureCount);
        public int AttemptCount => Volatile.Read(ref _attemptCount);

        public Task<OutboxMessage?> GetNextMessageAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _pollCount);

            var shouldFail = alwaysFailPoll ||
                (failAfterFirstSuccessfulPoll
                    ? SuccessfulPollCount > 0 && !_pollFailureReturned
                    : !_pollFailureReturned);
            if (pollFailure is not null && shouldFail)
            {
                _pollFailureReturned = true;
                Interlocked.Increment(ref _pollFailureCount);
                throw pollFailure;
            }

            Interlocked.Increment(ref _successfulPollCount);
            lock (_lock)
            {
                return Task.FromResult(
                    !_processed && pendingMessage is not null
                        ? new OutboxMessage
                        {
                            Id = pendingMessage.Id,
                            EventType = pendingMessage.EventType,
                            Payload = pendingMessage.Payload,
                            NextAttemptAtUtc = _nextAttemptAtUtc,
                            AttemptCount = _attemptCount
                        }
                        : null);
            }
        }

        public Task MarkProcessedAsync(long id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (failFirstProcessedWrite && !_processedWriteFailureReturned)
            {
                _processedWriteFailureReturned = true;
                throw new TimeoutException("Test processed-marker timeout.");
            }

            lock (_lock)
            {
                _processed = true;
            }

            Interlocked.Increment(ref _markProcessedCount);
            return Task.CompletedTask;
        }

        public Task<OutboxRetryState> RecordFailureAsync(
            long id,
            string error,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var attemptCount = Interlocked.Increment(ref _attemptCount);
            Interlocked.Increment(ref _recordFailureCount);
            _nextAttemptAtUtc = DateTime.UtcNow.AddSeconds(2);
            return Task.FromResult(new OutboxRetryState(attemptCount, 2));
        }
    }

    private sealed class TrackingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        private int _requestCount;

        public int RequestCount => Volatile.Read(ref _requestCount);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requestCount);
            return send(request, cancellationToken);
        }
    }
}
