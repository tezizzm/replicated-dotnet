using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Replicated;
using Xunit;

namespace Replicated.Tests;

// Minimal ILogger implementation that captures log entries for assertions.
internal class CapturingLogger : ILogger
{
    public record Entry(LogLevel Level, string Message);
    public List<Entry> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add(new Entry(logLevel, formatter(state, exception)));
}

// Minimal ILogger<T> wrapper.
internal sealed class CapturingLogger<T> : CapturingLogger, ILogger<T> { }

public class LoggingTests
{
    private static InstanceTagsRequest EmptyTags()
        => new InstanceTagsRequest(false, new Dictionary<string, string>());

    private static ReplicatedHttpClientAsync CreateClient(
        HttpMessageHandler handler,
        ILogger? logger,
        RetryPolicy? retryPolicy = null)
        => new ReplicatedHttpClientAsync(
            "http://test-replicated:3000",
            TimeSpan.FromSeconds(5),
            handler,
            retryPolicy ?? new RetryPolicy { MaxRetries = 0 },
            logger);

    private static SimpleHandler OkHandler(string body = "{}")
        => new SimpleHandler(HttpStatusCode.OK, body);

    private static SimpleHandler ErrorHandler(HttpStatusCode code)
        => new SimpleHandler(code, "{}");

    // ── Debug logging ─────────────────────────────────────────────────────────

    [Fact]
    public async Task TypedPostAsync_LogsDebugOnSuccess()
    {
        var logger = new CapturingLogger();
        var client = CreateClient(OkHandler(), logger);

        await client.TypedPostAsync("/test", EmptyTags(),
            ReplicatedJsonContext.Default.InstanceTagsRequest);

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("/test"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("200"));
    }

    [Fact]
    public async Task TypedPostAsync_NoLogger_DoesNotThrow()
    {
        var client = CreateClient(OkHandler(), logger: null);
        var ex = await Record.ExceptionAsync(() => client.TypedPostAsync("/test",
            EmptyTags(), ReplicatedJsonContext.Default.InstanceTagsRequest));
        Assert.Null(ex);
    }

    [Fact]
    public async Task TypedPostAsync_LogsRequestBeforeResponse()
    {
        var logger = new CapturingLogger();
        var client = CreateClient(OkHandler(), logger);

        await client.TypedPostAsync("/api/v1/app/instance-tags", EmptyTags(),
            ReplicatedJsonContext.Default.InstanceTagsRequest);

        // First debug entry should be the request, second the response.
        var debugEntries = logger.Entries.FindAll(e => e.Level == LogLevel.Debug);
        Assert.True(debugEntries.Count >= 2);
        Assert.Contains("/api/v1/app/instance-tags", debugEntries[0].Message);
        Assert.Contains("/api/v1/app/instance-tags", debugEntries[1].Message);
    }

    [Fact]
    public async Task TypedPostAsync_LogsElapsedTime()
    {
        var logger = new CapturingLogger();
        var client = CreateClient(OkHandler(), logger);

        await client.TypedPostAsync("/test", EmptyTags(),
            ReplicatedJsonContext.Default.InstanceTagsRequest);

        Assert.Contains(logger.Entries,
            e => e.Level == LogLevel.Debug && e.Message.Contains("ms"));
    }

    // ── Warning logging ───────────────────────────────────────────────────────

    [Fact]
    public async Task TypedPostAsync_NetworkError_LogsWarning()
    {
        var logger = new CapturingLogger();
        var client = CreateClient(new ThrowingHandler(), logger);

        await Assert.ThrowsAsync<ReplicatedNetworkError>(() =>
            client.TypedPostAsync("/test", EmptyTags(),
                ReplicatedJsonContext.Default.InstanceTagsRequest));

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task TypedPostAsync_Retry_LogsWarning()
    {
        var logger = new CapturingLogger();
        // Allow one retry so we see a warning, but fail on the final attempt.
        var policy = new RetryPolicy { MaxRetries = 1, InitialDelay = TimeSpan.FromMilliseconds(1) };
        var client = CreateClient(ErrorHandler(HttpStatusCode.InternalServerError), logger, policy);

        await Assert.ThrowsAsync<ReplicatedApiError>(() =>
            client.TypedPostAsync("/test", EmptyTags(),
                ReplicatedJsonContext.Default.InstanceTagsRequest));

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("retry"));
    }

    [Fact]
    public async Task TypedPostAsync_RateLimit_LogsWarning()
    {
        var logger = new CapturingLogger();
        var policy = new RetryPolicy { MaxRetries = 1, InitialDelay = TimeSpan.FromMilliseconds(1) };
        var client = CreateClient(ErrorHandler((HttpStatusCode)429), logger, policy);

        await Assert.ThrowsAsync<ReplicatedRateLimitError>(() =>
            client.TypedPostAsync("/test", EmptyTags(),
                ReplicatedJsonContext.Default.InstanceTagsRequest));

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("retry"));
    }

    // ── Builder / DI wiring ───────────────────────────────────────────────────

    [Fact]
    public void ReplicatedClientBuilder_WithLogger_DoesNotThrow()
    {
        var logger = new CapturingLogger();
        var ex = Record.Exception(() =>
            new ReplicatedClientBuilder()
                .WithBaseUrl("http://test-replicated:3000")
                .WithLogger(logger)
                .Build());
        Assert.Null(ex);
    }

    [Fact]
    public void ReplicatedClientBuilder_WithLogger_NullThrows()
        => Assert.Throws<ArgumentNullException>(() =>
            new ReplicatedClientBuilder().WithLogger(null!));

    [Fact]
    public void AddReplicatedClient_ResolvesLoggerFromDI()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddReplicatedClient();

        // Should resolve without throwing even when logging is registered.
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IReplicatedClient>();
        Assert.NotNull(client);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class SimpleHandler(HttpStatusCode code, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(code)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("simulated network failure");
    }
}
