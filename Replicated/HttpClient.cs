using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Replicated.Validation;

namespace Replicated;

/// <summary>
/// AOT/trim-safe async HTTP client for the Replicated in-cluster SDK API.
/// All serialization uses source-generated <see cref="ReplicatedJsonContext"/> type infos.
/// </summary>
internal sealed class ReplicatedHttpClientAsync : IDisposable, IAsyncDisposable
{
    private readonly string _baseUrl;
    private readonly CoreHttpClient _core;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly ILogger? _logger;
    private bool _disposed;

    // Production constructor — shared pooled SocketsHttpHandler via CoreHttpClient.
    internal ReplicatedHttpClientAsync(
        string baseUrl = Constants.DefaultBaseUrl,
        TimeSpan timeout = default,
        RetryPolicy? retryPolicy = null,
        ILogger? logger = null)
    {
        _logger = logger;
        _baseUrl = (baseUrl ?? Constants.DefaultBaseUrl).TrimEnd('/');
        _core = new CoreHttpClient(_baseUrl, timeout == default ? TimeSpan.FromSeconds(30) : timeout);
        var effective = retryPolicy ?? new RetryPolicy();
        _retryPolicy = PollyPolicyBuilder.BuildRetryPolicy(effective, OnRetry);
    }

    // Test constructor — injects a custom HttpMessageHandler.
    internal ReplicatedHttpClientAsync(
        string baseUrl,
        TimeSpan timeout,
        HttpMessageHandler testHandler,
        RetryPolicy? retryPolicy = null,
        ILogger? logger = null)
    {
        _logger = logger;
        _baseUrl = (baseUrl ?? Constants.DefaultBaseUrl).TrimEnd('/');
        _core = new CoreHttpClient(_baseUrl, timeout, testHandler);
        var effective = retryPolicy ?? new RetryPolicy();
        _retryPolicy = PollyPolicyBuilder.BuildRetryPolicy(effective, OnRetry);
    }

    private void OnRetry(Exception exception, TimeSpan delay, int attempt)
    {
        if (_logger == null) return;
        var status = (exception as ReplicatedApiError)?.HttpStatus;
        if (status.HasValue)
            _logger.LogWarning("Replicated retry {Attempt} in {DelayMs}ms (HTTP {Status})",
                attempt, (int)delay.TotalMilliseconds, status.Value);
        else
            _logger.LogWarning("Replicated retry {Attempt} in {DelayMs}ms ({ExceptionType})",
                attempt, (int)delay.TotalMilliseconds, exception.GetType().Name);
    }

    // ── Typed GET ─────────────────────────────────────────────────────────────

    internal async Task<TResp> TypedGetAsync<TResp>(
        string path,
        JsonTypeInfo<TResp> responseTypeInfo,
        CancellationToken cancellationToken = default)
    {
        InputValidator.ValidateUrlPath(path);
        var httpClient = _core.HttpClientInstance;

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}{path}");

            var sw = Stopwatch.StartNew();
            _logger?.LogDebug("Replicated GET {Path}", path);
            try
            {
                var response = await httpClient.SendAsync(request, cancellationToken);
                sw.Stop();
                _logger?.LogDebug("Replicated GET {Path} → {StatusCode} ({ElapsedMs}ms)",
                    path, (int)response.StatusCode, sw.ElapsedMilliseconds);
                return await HandleTypedResponseAsync(response, responseTypeInfo);
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                throw;
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                _logger?.LogWarning("Replicated GET {Path} network error ({ElapsedMs}ms): {Message}",
                    path, sw.ElapsedMilliseconds, ex.Message);
                throw new ReplicatedNetworkError($"Network error: {ex.Message}");
            }
        });
    }

    // ── Typed POST (with response) ────────────────────────────────────────────

    internal async Task<TResp> TypedPostAsync<TReq, TResp>(
        string path,
        TReq requestBody,
        JsonTypeInfo<TReq> requestTypeInfo,
        JsonTypeInfo<TResp> responseTypeInfo,
        CancellationToken cancellationToken = default)
    {
        InputValidator.ValidateUrlPath(path);
        var httpClient = _core.HttpClientInstance;

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = BuildJsonRequest(HttpMethod.Post, path, requestBody, requestTypeInfo);
            return await SendAndHandleAsync(request, path, "POST", responseTypeInfo, httpClient, cancellationToken);
        });
    }

    // ── Typed POST (void response) ────────────────────────────────────────────

    internal async Task TypedPostAsync<TReq>(
        string path,
        TReq requestBody,
        JsonTypeInfo<TReq> requestTypeInfo,
        CancellationToken cancellationToken = default)
    {
        InputValidator.ValidateUrlPath(path);
        var httpClient = _core.HttpClientInstance;

        await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = BuildJsonRequest(HttpMethod.Post, path, requestBody, requestTypeInfo);
            await SendAndHandleVoidAsync(request, path, "POST", httpClient, cancellationToken);
        });
    }

    // ── Typed PATCH (void response) ───────────────────────────────────────────

    internal async Task TypedPatchAsync<TReq>(
        string path,
        TReq requestBody,
        JsonTypeInfo<TReq> requestTypeInfo,
        CancellationToken cancellationToken = default)
    {
        InputValidator.ValidateUrlPath(path);
        var httpClient = _core.HttpClientInstance;

        await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = BuildJsonRequest(HttpMethod.Patch, path, requestBody, requestTypeInfo);
            await SendAndHandleVoidAsync(request, path, "PATCH", httpClient, cancellationToken);
        });
    }

    // ── Typed DELETE (no body, void response) ─────────────────────────────────

    internal async Task TypedDeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        InputValidator.ValidateUrlPath(path);
        var httpClient = _core.HttpClientInstance;

        await _retryPolicy.ExecuteAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}{path}");
            await SendAndHandleVoidAsync(request, path, "DELETE", httpClient, cancellationToken);
        });
    }

    // ── Send helpers ──────────────────────────────────────────────────────────

    private async Task<TResp> SendAndHandleAsync<TResp>(
        HttpRequestMessage request, string path, string method,
        JsonTypeInfo<TResp> responseTypeInfo, HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("Replicated {Method} {Path}", method, path);
        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            sw.Stop();
            _logger?.LogDebug("Replicated {Method} {Path} → {StatusCode} ({ElapsedMs}ms)",
                method, path, (int)response.StatusCode, sw.ElapsedMilliseconds);
            return await HandleTypedResponseAsync(response, responseTypeInfo);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            throw;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger?.LogWarning("Replicated {Method} {Path} network error ({ElapsedMs}ms): {Message}",
                method, path, sw.ElapsedMilliseconds, ex.Message);
            throw new ReplicatedNetworkError($"Network error: {ex.Message}");
        }
    }

    private async Task SendAndHandleVoidAsync(
        HttpRequestMessage request, string path, string method, HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _logger?.LogDebug("Replicated {Method} {Path}", method, path);
        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            sw.Stop();
            _logger?.LogDebug("Replicated {Method} {Path} → {StatusCode} ({ElapsedMs}ms)",
                method, path, (int)response.StatusCode, sw.ElapsedMilliseconds);
            await HandleVoidResponseAsync(response);
        }
        catch (OperationCanceledException)
        {
            sw.Stop();
            throw;
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger?.LogWarning("Replicated {Method} {Path} network error ({ElapsedMs}ms): {Message}",
                method, path, sw.ElapsedMilliseconds, ex.Message);
            throw new ReplicatedNetworkError($"Network error: {ex.Message}");
        }
    }

    // ── Request builder ───────────────────────────────────────────────────────

    private HttpRequestMessage BuildJsonRequest<TReq>(
        HttpMethod method, string path, TReq body, JsonTypeInfo<TReq> typeInfo)
    {
        var request = new HttpRequestMessage(method, $"{_baseUrl}{path}");
        var json = JsonSerializer.Serialize(body, typeInfo);
        request.Content = new StringContent(json, Encoding.UTF8, Constants.ContentTypeJson);
        return request;
    }

    // ── Response handlers ─────────────────────────────────────────────────────

    private static async Task<T> HandleTypedResponseAsync<T>(
        HttpResponseMessage response, JsonTypeInfo<T> typeInfo)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrEmpty(body)) return default!;
            try { return JsonSerializer.Deserialize(body, typeInfo) ?? default!; }
            catch (JsonException ex)
            {
                throw new ReplicatedApiError(
                    $"Failed to parse response: {ex.Message}", (int)response.StatusCode, body);
            }
        }

        ThrowForStatus(response.StatusCode, body);
        return default!;
    }

    private static async Task HandleVoidResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        ThrowForStatus(response.StatusCode, body);
    }

    private static void ThrowForStatus(HttpStatusCode statusCode, string body)
    {
        string? message = null;
        string? code = null;

        if (!string.IsNullOrEmpty(body))
        {
            try
            {
                var err = JsonSerializer.Deserialize(body, ReplicatedJsonContext.Default.ErrorResponse);
                message = err?.Message;
                code = err?.Code;
            }
            catch { }
        }

        message ??= $"HTTP {(int)statusCode}";

        if (statusCode == HttpStatusCode.Unauthorized)
            throw new ReplicatedAuthError(message, (int)statusCode, body, null, null, code);
        if (statusCode == (HttpStatusCode)429)
            throw new ReplicatedRateLimitError(message, (int)statusCode, body, null, null, code);
        throw new ReplicatedApiError(message, (int)statusCode, body, null, null, code);
    }

    // ── Disposal ──────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (!_disposed)
        {
            _core?.Dispose();
            _disposed = true;
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
