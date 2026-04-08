using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Replicated;
using Replicated.Services;
using Xunit;

namespace Replicated.Tests;

/// <summary>
/// Tests for LicenseService using a manual MockHttpClientContext.
/// </summary>
public class LicenseServiceTests
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
    public async Task GetInfoAsync_ReturnsLicenseInfo()
    {
        var expected = new LicenseInfo(
            LicenseId: "lic-123",
            LicenseType: "prod",
            CustomerName: "Acme Corp",
            CustomerEmail: "admin@acme.com",
            ChannelName: "Stable",
            Entitlements: System.Array.Empty<LicenseEntitlement>());

        var ctx = new MockHttpClientContext(expected);
        var svc = new LicenseService(ctx);

        var result = await svc.GetInfoAsync();

        Assert.Equal(expected, result);
        Assert.Equal("/api/v1/license/info", ctx.LastPath);
        Assert.Equal("GET", ctx.LastMethod);
    }

    [Fact]
    public async Task GetFieldsAsync_ReturnsLicenseFieldArray()
    {
        var expected = new[]
        {
            new LicenseField(Name: "seats", Description: "Number of seats", Value: "50", Signature: null)
        };

        var ctx = new MockHttpClientContext(expected);
        var svc = new LicenseService(ctx);

        var result = await svc.GetFieldsAsync();

        Assert.Equal(expected, result);
        Assert.Equal("/api/v1/license/fields", ctx.LastPath);
        Assert.Equal("GET", ctx.LastMethod);
    }

    [Fact]
    public async Task GetFieldAsync_CallsGetWithFieldNameInPath()
    {
        var expected = new LicenseField(
            Name: "my-field",
            Description: "A field",
            Value: "42",
            Signature: null);

        var ctx = new MockHttpClientContext(expected);
        var svc = new LicenseService(ctx);

        var result = await svc.GetFieldAsync("my-field");

        Assert.Equal(expected, result);
        Assert.Contains("my-field", ctx.LastPath);
        Assert.Equal("GET", ctx.LastMethod);
    }

    [Fact]
    public async Task GetInfoAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new System.Threading.CancellationTokenSource();
        cts.Cancel();

        var ctx = new CancellingMockContext();
        var svc = new LicenseService(ctx);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            svc.GetInfoAsync(cts.Token));
    }

    private sealed class CancellingMockContext : IHttpClientContext
    {
        public Task<TResp> GetAsync<TResp>(string path, JsonTypeInfo<TResp> responseTypeInfo,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(default(TResp)!);
        }

        public Task<TResp> PostAsync<TReq, TResp>(string path, TReq body, JsonTypeInfo<TReq> reqType,
            JsonTypeInfo<TResp> respType, CancellationToken cancellationToken = default)
            => Task.FromResult(default(TResp)!);

        public Task PostAsync<TReq>(string path, TReq body, JsonTypeInfo<TReq> reqType,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task PatchAsync<TReq>(string path, TReq body, JsonTypeInfo<TReq> reqType,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
