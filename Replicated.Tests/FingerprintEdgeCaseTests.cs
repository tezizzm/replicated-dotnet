using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Replicated;
using Replicated.Services;
using Xunit;

namespace Replicated.Tests;

/// <summary>
/// Tests for AppService using a manual MockHttpClientContext.
/// </summary>
public class AppServiceTests
{
    // ── Mock ──────────────────────────────────────────────────────────────────

    private sealed class MockHttpClientContext : IHttpClientContext
    {
        private readonly object? _getResponse;
        public string? LastPath { get; private set; }
        public string? LastMethod { get; private set; }

        public MockHttpClientContext(object? getResponse = null)
            => _getResponse = getResponse;

        public Task<TResp> GetAsync<TResp>(string path, JsonTypeInfo<TResp> responseTypeInfo,
            CancellationToken cancellationToken = default)
        {
            LastPath = path;
            LastMethod = "GET";
            return Task.FromResult((TResp)_getResponse!);
        }

        public Task<TResp> PostAsync<TReq, TResp>(string path, TReq body, JsonTypeInfo<TReq> reqType,
            JsonTypeInfo<TResp> respType, CancellationToken cancellationToken = default)
        {
            LastPath = path;
            LastMethod = "POST";
            return Task.FromResult(default(TResp)!);
        }

        public Task PostAsync<TReq>(string path, TReq body, JsonTypeInfo<TReq> reqType,
            CancellationToken cancellationToken = default)
        {
            LastPath = path;
            LastMethod = "POST";
            return Task.CompletedTask;
        }

        public Task PatchAsync<TReq>(string path, TReq body, JsonTypeInfo<TReq> reqType,
            CancellationToken cancellationToken = default)
        {
            LastPath = path;
            LastMethod = "PATCH";
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            LastPath = path;
            LastMethod = "DELETE";
            return Task.CompletedTask;
        }
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetInfoAsync_ReturnsAppInfo()
    {
        var expected = new AppInfo(
            InstanceId: "inst-1",
            AppSlug: "my-app",
            AppName: "My App",
            AppStatus: "ready",
            HelmChartUrl: null,
            CurrentRelease: null);

        var ctx = new MockHttpClientContext(expected);
        var svc = new AppService(ctx);

        var result = await svc.GetInfoAsync();

        Assert.Equal(expected, result);
        Assert.Equal("/api/v1/app/info", ctx.LastPath);
        Assert.Equal("GET", ctx.LastMethod);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsAppStatus()
    {
        var expected = new AppStatus(
            UpdatedAt: "2024-01-01T00:00:00Z",
            Sequence: 1,
            Resources: System.Array.Empty<ResourceState>());

        var ctx = new MockHttpClientContext(expected);
        var svc = new AppService(ctx);

        var result = await svc.GetStatusAsync();

        Assert.Equal(expected, result);
        Assert.Equal("/api/v1/app/status", ctx.LastPath);
    }

    [Fact]
    public async Task GetUpdatesAsync_ReturnsAppReleaseArray()
    {
        var expected = new[]
        {
            new AppRelease(VersionLabel: "1.2.0", CreatedAt: "2024-01-01", ReleaseNotes: "notes")
        };

        var ctx = new MockHttpClientContext(expected);
        var svc = new AppService(ctx);

        var result = await svc.GetUpdatesAsync();

        Assert.Equal(expected, result);
        Assert.Equal("/api/v1/app/updates", ctx.LastPath);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsAppReleaseArray()
    {
        var expected = new[]
        {
            new AppRelease(VersionLabel: "1.0.0", CreatedAt: "2023-01-01", ReleaseNotes: null)
        };

        var ctx = new MockHttpClientContext(expected);
        var svc = new AppService(ctx);

        var result = await svc.GetHistoryAsync();

        Assert.Equal(expected, result);
        Assert.Equal("/api/v1/app/history", ctx.LastPath);
    }

    [Fact]
    public async Task SendCustomMetricsAsync_CallsPostWithCorrectPath()
    {
        var ctx = new MockHttpClientContext();
        var svc = new AppService(ctx);

        await svc.SendCustomMetricsAsync(new Dictionary<string, double> { ["cpu"] = 0.75 });

        Assert.Equal("/api/v1/app/custom-metrics", ctx.LastPath);
        Assert.Equal("POST", ctx.LastMethod);
    }

    [Fact]
    public async Task UpsertCustomMetricsAsync_CallsPatchWithCorrectPath()
    {
        var ctx = new MockHttpClientContext();
        var svc = new AppService(ctx);

        await svc.UpsertCustomMetricsAsync(new Dictionary<string, double> { ["mem"] = 1024.0 });

        Assert.Equal("/api/v1/app/custom-metrics", ctx.LastPath);
        Assert.Equal("PATCH", ctx.LastMethod);
    }

    [Fact]
    public async Task DeleteCustomMetricAsync_CallsDeleteWithMetricNameInPath()
    {
        var ctx = new MockHttpClientContext();
        var svc = new AppService(ctx);

        await svc.DeleteCustomMetricAsync("myMetric");

        Assert.Contains("myMetric", ctx.LastPath);
        Assert.Equal("DELETE", ctx.LastMethod);
    }

    [Fact]
    public async Task SetInstanceTagsAsync_CallsPostWithCorrectPath()
    {
        var ctx = new MockHttpClientContext();
        var svc = new AppService(ctx);

        await svc.SetInstanceTagsAsync(new Dictionary<string, string> { ["env"] = "prod" });

        Assert.Equal("/api/v1/app/instance-tags", ctx.LastPath);
        Assert.Equal("POST", ctx.LastMethod);
    }

    // ── CancellationToken tests ───────────────────────────────────────────────

    [Fact]
    public async Task GetInfoAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        // Use the real HTTP client (no mock server running), so it will throw either
        // OperationCanceledException or ReplicatedNetworkError — we only care about
        // cancellation propagation, so use a cancelling mock instead.
        var ctx = new CancellingMockContext(cts.Token);
        var svc = new AppService(ctx);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            svc.GetInfoAsync(cts.Token));
    }

    [Fact]
    public async Task SendCustomMetricsAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        var ctx = new CancellingMockContext(cts.Token);
        var svc = new AppService(ctx);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            svc.SendCustomMetricsAsync(new Dictionary<string, double> { ["x"] = 1 }, cts.Token));
    }

    // Mock that throws OperationCanceledException when token is cancelled.
    private sealed class CancellingMockContext : IHttpClientContext
    {
        private readonly System.Threading.CancellationToken _token;

        public CancellingMockContext(System.Threading.CancellationToken token)
            => _token = token;

        public Task<TResp> GetAsync<TResp>(string path, JsonTypeInfo<TResp> responseTypeInfo,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(default(TResp)!);
        }

        public Task<TResp> PostAsync<TReq, TResp>(string path, TReq body, JsonTypeInfo<TReq> reqType,
            JsonTypeInfo<TResp> respType, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(default(TResp)!);
        }

        public Task PostAsync<TReq>(string path, TReq body, JsonTypeInfo<TReq> reqType,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task PatchAsync<TReq>(string path, TReq body, JsonTypeInfo<TReq> reqType,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
