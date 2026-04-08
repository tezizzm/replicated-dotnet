using System;
using System.Net.Http;

namespace Replicated;

/// <summary>
/// Shared internal HTTP client providing connection pooling.
/// The in-cluster Replicated service requires no authentication headers.
/// </summary>
internal sealed class CoreHttpClient : IDisposable
{
    private static readonly SocketsHttpHandler SharedSocketsHandler = new()
    {
        MaxConnectionsPerServer = 10,
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        AllowAutoRedirect = true,
    };

    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;
    private bool _disposed;

    /// <summary>Production use — shared pooled SocketsHttpHandler.</summary>
    internal CoreHttpClient(string baseUrl, TimeSpan timeout)
    {
        _httpClient = new HttpClient(SharedSocketsHandler, disposeHandler: false)
        {
            Timeout = timeout == default ? TimeSpan.FromSeconds(30) : timeout
        };
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _ownsClient = false;
    }

    /// <summary>Test use — injects a custom <see cref="HttpMessageHandler"/>.</summary>
    internal CoreHttpClient(string baseUrl, TimeSpan timeout, HttpMessageHandler testHandler)
    {
        _httpClient = new HttpClient(testHandler)
        {
            Timeout = timeout == default ? TimeSpan.FromSeconds(30) : timeout
        };
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _ownsClient = true;
    }

    /// <summary>Exposes the underlying <see cref="HttpClient"/>.</summary>
    internal HttpClient HttpClientInstance => _httpClient;

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_ownsClient)
                _httpClient.Dispose();
            _disposed = true;
        }
    }
}
