using System;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Replicated.Configuration;
using Replicated.Services;
using Replicated.Validation;

namespace Replicated;

/// <summary>
/// Client for the Replicated in-cluster SDK API.
/// Connects to the Replicated service running alongside your application in the cluster
/// (default: <c>http://replicated:3000</c>).
/// </summary>
public class ReplicatedClient : IReplicatedClient, IHttpClientContext, IDisposable, IAsyncDisposable
{
    private readonly ReplicatedHttpClientAsync _httpClient;
    private bool _disposed;

    /// <summary>The base URL of the Replicated in-cluster service.</summary>
    public string BaseUrl { get; }

    /// <summary>The request timeout.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>Application endpoints — info, status, updates, custom metrics, instance tags.</summary>
    public AppService App { get; }

    /// <summary>License endpoints — info and fields.</summary>
    public LicenseService License { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplicatedClient"/> class.
    /// </summary>
    /// <param name="baseUrl">
    /// Base URL for the Replicated in-cluster service.
    /// Defaults to <c>http://replicated:3000</c>, or the value of the
    /// <c>REPLICATED_SDK_ENDPOINT</c> environment variable when set.
    /// </param>
    /// <param name="timeout">
    /// Request timeout. Defaults to 30 seconds, or the value of the
    /// <c>REPLICATED_TIMEOUT</c> environment variable when set.
    /// </param>
    /// <param name="retryPolicy">Optional retry policy. Defaults to 3 retries with exponential backoff.</param>
    /// <param name="logger">Optional logger. When provided, HTTP requests, responses, and retries are logged.</param>
    public ReplicatedClient(
        string? baseUrl = null,
        TimeSpan timeout = default,
        RetryPolicy? retryPolicy = null,
        ILogger? logger = null)
    {
        var resolvedBaseUrl = baseUrl
            ?? EnvironmentConfigReader.GetBaseUrl()
            ?? Constants.DefaultBaseUrl;

        var resolvedTimeout = timeout != default
            ? timeout
            : EnvironmentConfigReader.GetTimeout(TimeSpan.FromSeconds(30));

        InputValidator.ValidateBaseUrl(resolvedBaseUrl);
        InputValidator.ValidateTimeout(resolvedTimeout);

        var resolvedRetryPolicy = retryPolicy ?? EnvironmentConfigReader.GetRetryPolicy();
        resolvedRetryPolicy?.Validate();

        BaseUrl = resolvedBaseUrl;
        Timeout = resolvedTimeout;

        _httpClient = new ReplicatedHttpClientAsync(
            resolvedBaseUrl, Timeout, resolvedRetryPolicy, logger);

        App = new AppService(this);
        License = new LicenseService(this);
    }

    // ── IHttpClientContext explicit implementation ─────────────────────────────

    Task<TResp> IHttpClientContext.GetAsync<TResp>(string path, JsonTypeInfo<TResp> responseTypeInfo,
        CancellationToken cancellationToken)
        => _httpClient.TypedGetAsync(path, responseTypeInfo, cancellationToken);

    Task<TResp> IHttpClientContext.PostAsync<TReq, TResp>(
        string path, TReq body, JsonTypeInfo<TReq> reqType, JsonTypeInfo<TResp> respType,
        CancellationToken cancellationToken)
        => _httpClient.TypedPostAsync(path, body, reqType, respType, cancellationToken);

    Task IHttpClientContext.PostAsync<TReq>(string path, TReq body, JsonTypeInfo<TReq> reqType,
        CancellationToken cancellationToken)
        => _httpClient.TypedPostAsync(path, body, reqType, cancellationToken);

    Task IHttpClientContext.PatchAsync<TReq>(string path, TReq body, JsonTypeInfo<TReq> reqType,
        CancellationToken cancellationToken)
        => _httpClient.TypedPatchAsync(path, body, reqType, cancellationToken);

    Task IHttpClientContext.DeleteAsync(string path, CancellationToken cancellationToken)
        => _httpClient.TypedDeleteAsync(path, cancellationToken);

    // ── Disposal ──────────────────────────────────────────────────────────────

    /// <summary>Disposes the client and releases all resources.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Disposes the client.</summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>Disposes the client asynchronously.</summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Disposes the client asynchronously.</summary>
    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (!_disposed && disposing)
        {
            if (_httpClient != null)
                await _httpClient.DisposeAsync();
            _disposed = true;
        }
    }
}
