using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Replicated.Configuration;
using Replicated.Resources;
using Replicated.Services;
using Replicated.Validation;

namespace Replicated;

/// <summary>
/// Client for the Replicated SDK.
/// </summary>
public class ReplicatedClient : IReplicatedClient, IDisposable, IAsyncDisposable
{
    private readonly ReplicatedHttpClientAsync _httpClient;
    private readonly StateManager _stateManager;
    private readonly string _machineId;
    private bool _disposed = false;

    /// <summary>
    /// Gets the publishable key.
    /// </summary>
    public string PublishableKey { get; }

    /// <summary>
    /// Gets the app slug.
    /// </summary>
    public string AppSlug { get; }

    /// <summary>
    /// Gets the base URL.
    /// </summary>
    public string BaseUrl { get; }

    /// <summary>
    /// Gets the timeout.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Gets the state directory.
    /// </summary>
    public string? StateDirectory { get; }

    /// <summary>
    /// Gets the machine ID.
    /// </summary>
    public string MachineId => _machineId;

    /// <summary>
    /// Gets the HTTP client.
    /// </summary>
    internal ReplicatedHttpClientAsync httpClient => _httpClient;

    /// <summary>
    /// Gets the state manager.
    /// </summary>
    public StateManager StateManager => _stateManager;

    /// <summary>
    /// Gets the customer service.
    /// </summary>
    public CustomerService Customer { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplicatedClient"/> class.
    /// </summary>
    /// <param name="publishableKey">The Replicated publishable key. If null, reads from REPLICATED_PUBLISHABLE_KEY environment variable.</param>
    /// <param name="appSlug">The application slug. If null, reads from REPLICATED_APP_SLUG environment variable.</param>
    /// <param name="baseUrl">The base URL for the API. If null, reads from REPLICATED_BASE_URL environment variable or defaults to "https://replicated.app".</param>
    /// <param name="timeout">Request timeout. If default, reads from REPLICATED_TIMEOUT environment variable or defaults to 30 seconds.</param>
    /// <param name="stateDirectory">Optional custom state directory. If null, reads from REPLICATED_STATE_DIRECTORY environment variable or uses platform-specific default.</param>
    /// <param name="retryPolicy">Optional retry policy configuration. If null, uses default retry policy (3 retries, 1s initial delay, exponential backoff).</param>
    /// <exception cref="ArgumentException">Thrown when required parameters are missing or invalid.</exception>
    /// <remarks>
    /// Configuration precedence (highest to lowest):
    /// 1. Explicit constructor parameters
    /// 2. Environment variables
    /// 3. Default values (for baseUrl, timeout, and retryPolicy)
    /// </remarks>
    public ReplicatedClient(
        string? publishableKey = null,
        string? appSlug = null,
        string? baseUrl = null,
        TimeSpan timeout = default,
        string? stateDirectory = null,
        RetryPolicy? retryPolicy = null)
    {
        // Resolve configuration with precedence: explicit parameters > environment variables > defaults
        var resolvedPublishableKey = publishableKey ?? EnvironmentConfigReader.GetPublishableKey();
        var resolvedAppSlug = appSlug ?? EnvironmentConfigReader.GetAppSlug();
        var resolvedBaseUrl = baseUrl ?? EnvironmentConfigReader.GetBaseUrl() ?? "https://replicated.app";
        var resolvedTimeout = timeout != default 
            ? timeout 
            : EnvironmentConfigReader.GetTimeout(TimeSpan.FromSeconds(30));
        var resolvedStateDirectory = stateDirectory ?? EnvironmentConfigReader.GetStateDirectory();

        // Validate that required parameters are present
        if (string.IsNullOrWhiteSpace(resolvedPublishableKey))
        {
            throw new ArgumentException(
                "Publishable key is required. Provide it via constructor parameter or set the REPLICATED_PUBLISHABLE_KEY environment variable.",
                nameof(publishableKey));
        }

        if (string.IsNullOrWhiteSpace(resolvedAppSlug))
        {
            throw new ArgumentException(
                "App slug is required. Provide it via constructor parameter or set the REPLICATED_APP_SLUG environment variable.",
                nameof(appSlug));
        }

        // Validate inputs
        InputValidator.ValidatePublishableKey(resolvedPublishableKey);
        InputValidator.ValidateAppSlug(resolvedAppSlug);
        InputValidator.ValidateBaseUrl(resolvedBaseUrl);
        InputValidator.ValidateTimeout(resolvedTimeout);
        
        // Resolve retry policy: explicit parameter > environment variables > default
        var resolvedRetryPolicy = retryPolicy ?? EnvironmentConfigReader.GetRetryPolicy();

        // Validate retry policy if provided
        resolvedRetryPolicy?.Validate();
        
        PublishableKey = resolvedPublishableKey;
        AppSlug = resolvedAppSlug;
        BaseUrl = resolvedBaseUrl;
        Timeout = resolvedTimeout;
        StateDirectory = resolvedStateDirectory;
        _machineId = Fingerprint.GetMachineFingerprint();

        _httpClient = new ReplicatedHttpClientAsync(resolvedBaseUrl, Timeout, null, resolvedRetryPolicy);
        _stateManager = new StateManager(resolvedAppSlug, resolvedStateDirectory);
        Customer = new CustomerService(this);
    }

    /// <summary>
    /// Gets authentication headers for API requests.
    /// </summary>
    /// <returns>A dictionary containing authentication headers. Uses dynamic token if available, otherwise falls back to publishable key with Bearer prefix.</returns>
    /// <remarks>
    /// The method automatically selects the best authentication method:
    /// - If a dynamic token is cached, it uses that token (without Bearer prefix)
    /// - Otherwise, it uses the publishable key with Bearer prefix
    /// </remarks>
    public Dictionary<string, string> GetAuthHeaders()
    {
        // Try to use dynamic token first, fall back to publishable key
        var dynamicToken = _stateManager.GetDynamicToken();
        if (!string.IsNullOrEmpty(dynamicToken))
        {
            // Service tokens are sent without Bearer prefix
            return new Dictionary<string, string> { [Constants.AuthorizationHeader] = dynamicToken };
        }
        else
        {
            // Publishable keys use Bearer prefix
            return new Dictionary<string, string> { [Constants.AuthorizationHeader] = $"{Constants.BearerPrefix}{PublishableKey}" };
        }
    }

    /// <summary>
    /// Makes a synchronous HTTP request to the Replicated API.
    /// </summary>
    /// <param name="method">The HTTP method (GET, POST, PUT, PATCH, DELETE, etc.).</param>
    /// <param name="url">The URL path relative to the base URL (must start with '/').</param>
    /// <param name="headers">Optional additional HTTP headers to include in the request.</param>
    /// <param name="jsonData">Optional JSON data to send in the request body.</param>
    /// <param name="parameters">Optional query parameters to append to the URL.</param>
    /// <returns>A dictionary containing the parsed JSON response from the API.</returns>
    /// <exception cref="ArgumentException">Thrown when method or URL is invalid.</exception>
    /// <exception cref="ReplicatedApiError">Thrown when the API returns an error response.</exception>
    /// <exception cref="ReplicatedNetworkError">Thrown when a network error occurs.</exception>
    /// <remarks>
    /// This method includes automatic retry logic based on the configured retry policy.
    /// All requests are authenticated using the publishable key or dynamic token.
    /// </remarks>
    public Dictionary<string, object> MakeRequest(
        string method,
        string url,
        Dictionary<string, string>? headers = null,
        Dictionary<string, object>? jsonData = null,
        Dictionary<string, object>? parameters = null)
    {
        return _httpClient.MakeRequest(method, url, headers, jsonData, parameters);
    }

    /// <summary>
    /// Makes an asynchronous HTTP request to the Replicated API.
    /// </summary>
    /// <param name="method">The HTTP method (GET, POST, PUT, PATCH, DELETE, etc.).</param>
    /// <param name="url">The URL path relative to the base URL (must start with '/').</param>
    /// <param name="headers">Optional additional HTTP headers to include in the request.</param>
    /// <param name="jsonData">Optional JSON data to send in the request body.</param>
    /// <param name="parameters">Optional query parameters to append to the URL.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary with the parsed JSON response from the API.</returns>
    /// <exception cref="ArgumentException">Thrown when method or URL is invalid.</exception>
    /// <exception cref="ReplicatedApiError">Thrown when the API returns an error response.</exception>
    /// <exception cref="ReplicatedNetworkError">Thrown when a network error occurs.</exception>
    /// <remarks>
    /// This method includes automatic retry logic based on the configured retry policy.
    /// All requests are authenticated using the publishable key or dynamic token.
    /// Prefer this method over <see cref="MakeRequest"/> for async/await scenarios.
    /// </remarks>
    public async Task<Dictionary<string, object>> MakeRequestAsync(
        string method,
        string url,
        Dictionary<string, string>? headers = null,
        Dictionary<string, object>? jsonData = null,
        Dictionary<string, object>? parameters = null)
    {
        return await _httpClient.MakeRequestAsync(method, url, headers, jsonData, parameters);
    }

    /// <summary>
    /// Disposes the client and releases all resources.
    /// </summary>
    /// <remarks>
    /// This method disposes the underlying HTTP client and cleans up resources.
    /// After disposal, the client should not be used. For async disposal, use <see cref="DisposeAsync"/>.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the client.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.DisposeAsync().AsTask().Wait();
            _disposed = true;
        }
    }

    /// <summary>
    /// Disposes the client asynchronously and releases all resources.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    /// <remarks>
    /// This method is the preferred way to dispose the client in async contexts.
    /// Use with 'await using' statement for automatic disposal:
    /// <code>
    /// await using var client = new ReplicatedClient("key", "app");
    /// // Use client...
    /// // Automatically disposed when exiting scope
    /// </code>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the client asynchronously.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (!_disposed && disposing)
        {
            if (_httpClient != null)
            {
                await _httpClient.DisposeAsync();
            }
            _disposed = true;
        }
    }
}
