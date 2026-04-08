using System;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Replicated;
using Replicated.Services;
using Xunit;

namespace Replicated.Tests;

/// <summary>
/// Validation tests for AppService and LicenseService input parameters.
/// </summary>
public class AppServiceValidationTests
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

    // ── AppService.DeleteCustomMetricAsync validation ─────────────────────────

    [Fact]
    public async Task DeleteCustomMetricAsync_NullMetricName_ThrowsArgumentException()
    {
        var ctx = new MockHttpClientContext();
        var svc = new AppService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.DeleteCustomMetricAsync(null!));
    }

    [Fact]
    public async Task DeleteCustomMetricAsync_EmptyMetricName_ThrowsArgumentException()
    {
        var ctx = new MockHttpClientContext();
        var svc = new AppService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.DeleteCustomMetricAsync(""));
    }

    [Fact]
    public async Task DeleteCustomMetricAsync_WhitespaceMetricName_ThrowsArgumentException()
    {
        var ctx = new MockHttpClientContext();
        var svc = new AppService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.DeleteCustomMetricAsync("  "));
    }

    // ── LicenseService.GetFieldAsync validation ───────────────────────────────

    [Fact]
    public async Task GetFieldAsync_NullFieldName_ThrowsArgumentException()
    {
        var ctx = new MockHttpClientContext();
        var svc = new LicenseService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.GetFieldAsync(null!));
    }

    [Fact]
    public async Task GetFieldAsync_EmptyFieldName_ThrowsArgumentException()
    {
        var ctx = new MockHttpClientContext();
        var svc = new LicenseService(ctx);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.GetFieldAsync(""));
    }
}
