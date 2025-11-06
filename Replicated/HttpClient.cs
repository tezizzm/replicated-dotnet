using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Polly;
using Replicated.Validation;

namespace Replicated;

/// <summary>
/// Base HTTP client for making requests to the Replicated API.
/// </summary>
public abstract class ReplicatedHttpClientBase
{
    /// <summary>
    /// Gets the base URL.
    /// </summary>
    protected string BaseUrl { get; }

    /// <summary>
    /// Gets the timeout.
    /// </summary>
    protected TimeSpan Timeout { get; }

    /// <summary>
    /// Gets or sets the default headers.
    /// </summary>
    protected Dictionary<string, string>? DefaultHeaders { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplicatedHttpClientBase"/> class.
    /// </summary>
    /// <param name="baseUrl">The base URL for the API.</param>
    /// <param name="timeout">Request timeout.</param>
    /// <param name="headers">Default headers to include with requests.</param>
    protected ReplicatedHttpClientBase(
        string baseUrl = "https://replicated.app",
        TimeSpan timeout = default,
        Dictionary<string, string>? headers = null)
    {
        BaseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        Timeout = timeout == default ? TimeSpan.FromSeconds(30) : timeout;
        DefaultHeaders = headers;
    }

    /// <summary>
    /// Makes an HTTP request.
    /// </summary>
    public abstract Dictionary<string, object> MakeRequest(
        string method,
        string url,
        Dictionary<string, string>? headers = null,
        Dictionary<string, object>? jsonData = null,
        Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Makes an asynchronous HTTP request.
    /// </summary>
    public abstract Task<Dictionary<string, object>> MakeRequestAsync(
        string method,
        string url,
        Dictionary<string, string>? headers = null,
        Dictionary<string, object>? jsonData = null,
        Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Builds a query string from parameters efficiently.
    /// </summary>
    protected static string BuildQueryString(Dictionary<string, object> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return string.Empty;
        
        var sb = new StringBuilder(parameters.Count * 20); // Estimate average param length
        bool first = true;
        
        foreach (var kvp in parameters)
        {
            if (!first)
                sb.Append('&');
            first = false;
            
            sb.Append(Uri.EscapeDataString(kvp.Key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(kvp.Value?.ToString() ?? string.Empty));
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Builds request headers from default and provided headers.
    /// </summary>
    protected Dictionary<string, string> BuildHeaders(Dictionary<string, string>? headers)
    {
        var requestHeaders = new Dictionary<string, string>();

        // Add default headers first
        if (DefaultHeaders != null)
        {
            foreach (var header in DefaultHeaders)
            {
                requestHeaders[header.Key] = header.Value;
            }
        }

        // Override with provided headers
        if (headers != null)
        {
            foreach (var header in headers)
            {
                requestHeaders[header.Key] = header.Value;
            }
        }

        return requestHeaders;
    }

    /// <summary>
    /// Recursively converts System.Text.Json JsonElement values in a deserialized object graph
    /// into plain .NET types (Dictionary<string, object>, List<object>, string, double, bool, null).
    /// This ensures callers can cast nested objects to Dictionary<string, object> safely.
    /// </summary>
    private static object? ConvertJsonElement(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement elem)
        {
            switch (elem.ValueKind)
            {
                case JsonValueKind.Object:
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in elem.EnumerateObject())
                    {
                        dict[prop.Name] = ConvertJsonElement(prop.Value);
                    }
                    return dict;
                }
                case JsonValueKind.Array:
                {
                    var list = new List<object>();
                    foreach (var item in elem.EnumerateArray())
                    {
                        list.Add(ConvertJsonElement(item));
                    }
                    return list;
                }
                case JsonValueKind.String:
                    return elem.GetString();
                case JsonValueKind.Number:
                    // Prefer double for general numeric representation
                    if (elem.TryGetInt64(out var l)) return l;
                    if (elem.TryGetDouble(out var d)) return d;
                    return elem.ToString();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
            }
        }

        if (value is Dictionary<string, object> dictValue)
        {
            var result = new Dictionary<string, object>();
            foreach (var kvp in dictValue)
            {
                result[kvp.Key] = ConvertJsonElement(kvp.Value);
            }
            return result;
        }

        if (value is IEnumerable<object> listValue)
        {
            var result = new List<object>();
            foreach (var item in listValue)
            {
                result.Add(ConvertJsonElement(item));
            }
            return result;
        }

        return value;
    }

    private static Dictionary<string, object> NormalizeJsonDictionary(Dictionary<string, object> input)
    {
        var normalized = new Dictionary<string, object>();
        foreach (var kvp in input)
        {
            var converted = ConvertJsonElement(kvp.Value);
            // Store nulls as null, others as objects
            normalized[kvp.Key] = converted as object;
        }
        return normalized;
    }

    /// <summary>
    /// Handles HTTP response and raises appropriate exceptions (async version for better performance).
    /// </summary>
    protected async Task<Dictionary<string, object>> HandleResponseAsync(HttpResponseMessage response)
    {
        Dictionary<string, object>? jsonBody = null;
        var headers = new Dictionary<string, string>();

        // Extract headers
        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        foreach (var header in response.Content.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        // Try to parse JSON body asynchronously
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(responseBody))
        {
            try
            {
                jsonBody = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody, ReplicatedHttpClientAsync.JsonSerializerOptions);
                if (jsonBody != null)
                {
                    jsonBody = NormalizeJsonDictionary(jsonBody);
                }
            }
            catch
            {
                // If JSON parsing fails, jsonBody remains null
            }
        }

        if (response.IsSuccessStatusCode)
        {
            return jsonBody ?? new Dictionary<string, object>();
        }

        // Determine error message
        var errorMessage = jsonBody?.TryGetValue("message", out var msg) == true 
            ? msg?.ToString() 
            : $"HTTP {response.StatusCode}";
        var errorCode = jsonBody?.TryGetValue("code", out var code) == true 
            ? code?.ToString() 
            : null;

        // Throw appropriate exception based on status code
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ReplicatedAuthError(
                errorMessage ?? "Authentication failed",
                (int)response.StatusCode,
                responseBody,
                jsonBody,
                headers,
                errorCode);
        }

        if (response.StatusCode == (HttpStatusCode)429)
        {
            throw new ReplicatedRateLimitError(
                errorMessage ?? "Rate limit exceeded",
                (int)response.StatusCode,
                responseBody,
                jsonBody,
                headers,
                errorCode);
        }

        if (response.StatusCode >= HttpStatusCode.InternalServerError)
        {
            throw new ReplicatedApiError(
                errorMessage ?? $"Server error: {response.StatusCode}",
                (int)response.StatusCode,
                responseBody,
                jsonBody,
                headers,
                errorCode);
        }

        // Other client errors (4xx, except 401 and 429)
        throw new ReplicatedApiError(
            errorMessage ?? $"Client error: {response.StatusCode}",
            (int)response.StatusCode,
            responseBody,
            jsonBody,
            headers,
            errorCode);
    }

    /// <summary>
    /// Handles HTTP response and raises appropriate exceptions (synchronous version).
    /// </summary>
    protected Dictionary<string, object> HandleResponse(HttpResponseMessage response)
    {
        Dictionary<string, object>? jsonBody = null;
        var headers = new Dictionary<string, string>();

        // Extract headers
        foreach (var header in response.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        foreach (var header in response.Content.Headers)
        {
            headers[header.Key] = string.Join(", ", header.Value);
        }

        // Try to parse JSON body (async version)
        var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!string.IsNullOrEmpty(responseBody))
        {
            try
            {
                jsonBody = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody, ReplicatedHttpClientAsync.JsonSerializerOptions);
                if (jsonBody != null)
                {
                    jsonBody = NormalizeJsonDictionary(jsonBody);
                }
            }
            catch
            {
                // If JSON parsing fails, jsonBody remains null
            }
        }

        if (response.IsSuccessStatusCode)
        {
            return jsonBody ?? new Dictionary<string, object>();
        }

        // Determine error message
        var errorMessage = jsonBody?.TryGetValue("message", out var msg) == true 
            ? msg?.ToString() 
            : $"HTTP {response.StatusCode}";
        var errorCode = jsonBody?.TryGetValue("code", out var code) == true 
            ? code?.ToString() 
            : null;

        // Throw appropriate exception based on status code
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new ReplicatedAuthError(
                errorMessage ?? "Authentication failed",
                (int)response.StatusCode,
                responseBody,
                jsonBody,
                headers,
                errorCode);
        }

        if (response.StatusCode == (HttpStatusCode)429)
        {
            throw new ReplicatedRateLimitError(
                errorMessage ?? "Rate limit exceeded",
                (int)response.StatusCode,
                responseBody,
                jsonBody,
                headers,
                errorCode);
        }

        if (response.StatusCode >= HttpStatusCode.InternalServerError)
        {
            throw new ReplicatedApiError(
                errorMessage ?? $"Server error: {response.StatusCode}",
                (int)response.StatusCode,
                responseBody,
                jsonBody,
                headers,
                errorCode);
        }

        // Other client errors (4xx, except 401 and 429)
        throw new ReplicatedApiError(
            errorMessage ?? $"Client error: {response.StatusCode}",
            (int)response.StatusCode,
            responseBody,
            jsonBody,
            headers,
            errorCode);
    }
}

/// <summary>
/// Async HTTP client implementation.
/// </summary>
public class ReplicatedHttpClientAsync : ReplicatedHttpClientBase, IAsyncDisposable
{
    // Shared SocketsHttpHandler for connection pooling (better performance than HttpClientHandler)
    private static readonly SocketsHttpHandler SharedHandler = new()
    {
        MaxConnectionsPerServer = 10,
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        AllowAutoRedirect = true
    };
    
    // Cached JsonSerializerOptions for better performance
    internal static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        MaxDepth = 64,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };
    
    private readonly System.Net.Http.HttpClient _httpClient;
    private readonly IAsyncPolicy<Dictionary<string, object>> _retryPolicy;
    private readonly ISyncPolicy<Dictionary<string, object>> _syncRetryPolicy;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplicatedHttpClientAsync"/> class.
    /// </summary>
    /// <param name="baseUrl">The base URL for the API.</param>
    /// <param name="timeout">Request timeout.</param>
    /// <param name="headers">Default headers to include with requests.</param>
    /// <param name="retryPolicy">Optional retry policy configuration. If null, uses default retry policy (3 retries, 1s initial delay).</param>
    public ReplicatedHttpClientAsync(
        string baseUrl = "https://replicated.app",
        TimeSpan timeout = default,
        Dictionary<string, string>? headers = null,
        RetryPolicy? retryPolicy = null) : base(baseUrl, timeout, headers)
    {
        // Use shared handler for connection pooling and reuse
        _httpClient = new System.Net.Http.HttpClient(SharedHandler, disposeHandler: false)
        {
            Timeout = Timeout
        };

        // Build Polly policies from our RetryPolicy configuration
        _retryPolicy = PollyPolicyBuilder.BuildRetryPolicy(
            retryPolicy ?? new RetryPolicy() // Default: 3 retries, 1s initial delay
        );
        _syncRetryPolicy = PollyPolicyBuilder.BuildSyncRetryPolicy(
            retryPolicy ?? new RetryPolicy()
        );
    }

    /// <summary>
    /// Makes a synchronous HTTP request.
    /// </summary>
    public override Dictionary<string, object> MakeRequest(
        string method,
        string url,
        Dictionary<string, string>? headers = null,
        Dictionary<string, object>? jsonData = null,
        Dictionary<string, object>? parameters = null)
    {
        // Validate inputs first (before retry policy)
        InputValidator.ValidateHttpMethod(method);
        InputValidator.ValidateUrlPath(url);
        InputValidator.ValidateHeaders(headers);
        InputValidator.ValidateParameters(parameters);

        // Execute with sync retry policy
        return _syncRetryPolicy.Execute(() =>
        {
            var fullUrl = $"{BaseUrl}{url}";
            var requestHeaders = BuildHeaders(headers);

            using var request = new HttpRequestMessage(new HttpMethod(method), fullUrl);

            // Add headers
            foreach (var header in requestHeaders)
            {
                var added = request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                if (header.Key == "X-Test-Status" && !added)
                {
                    // For test headers, throw if we can't add them - this helps debug header injection issues
                    throw new InvalidOperationException(
                        $"Failed to add X-Test-Status header '{header.Value}' to HttpRequestMessage. " +
                        $"This may indicate an issue with header injection in integration tests.");
                }
            }

            // Add query parameters
            if (parameters != null && parameters.Count > 0)
            {
                var queryString = BuildQueryString(parameters);
                request.RequestUri = new Uri($"{fullUrl}?{queryString}");
            }

            // Add JSON content
            if (jsonData != null)
            {
                var json = JsonSerializer.Serialize(jsonData, JsonSerializerOptions);
                request.Content = new StringContent(json, Encoding.UTF8, Constants.ContentTypeJson);
            }

            try
            {
                var response = _httpClient.Send(request);
                return HandleResponse(response);
            }
            catch (HttpRequestException ex)
            {
                throw new ReplicatedNetworkError($"Network error: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Makes an asynchronous HTTP request.
    /// </summary>
    public override async Task<Dictionary<string, object>> MakeRequestAsync(
        string method,
        string url,
        Dictionary<string, string>? headers = null,
        Dictionary<string, object>? jsonData = null,
        Dictionary<string, object>? parameters = null)
    {
        // Validate inputs first (before retry policy)
        InputValidator.ValidateHttpMethod(method);
        InputValidator.ValidateUrlPath(url);
        InputValidator.ValidateHeaders(headers);
        InputValidator.ValidateParameters(parameters);

        // Execute request with Polly retry policy
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var fullUrl = $"{BaseUrl}{url}";
            var requestHeaders = BuildHeaders(headers);

            using var request = new HttpRequestMessage(new HttpMethod(method), fullUrl);

            // Add headers
            foreach (var header in requestHeaders)
            {
                var added = request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                if (header.Key == "X-Test-Status" && !added)
                {
                    // For test headers, throw if we can't add them - this helps debug header injection issues
                    throw new InvalidOperationException(
                        $"Failed to add X-Test-Status header '{header.Value}' to HttpRequestMessage. " +
                        $"This may indicate an issue with header injection in integration tests.");
                }
            }

            // Add query parameters
            if (parameters != null && parameters.Count > 0)
            {
                var queryString = BuildQueryString(parameters);
                request.RequestUri = new Uri($"{fullUrl}?{queryString}");
            }

            // Add JSON content
            if (jsonData != null)
            {
                var json = JsonSerializer.Serialize(jsonData, JsonSerializerOptions);
                request.Content = new StringContent(json, Encoding.UTF8, Constants.ContentTypeJson);
            }

            try
            {
                var response = await _httpClient.SendAsync(request);
                return await HandleResponseAsync(response);
            }
            catch (HttpRequestException ex)
            {
                throw new ReplicatedNetworkError($"Network error: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Disposes the HTTP client asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
        await Task.CompletedTask;
    }
}

