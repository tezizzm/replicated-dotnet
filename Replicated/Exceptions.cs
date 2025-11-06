using System;
using System.Collections.Generic;

namespace Replicated;

/// <summary>
/// Base exception class for all Replicated SDK errors.
/// </summary>
/// <remarks>
/// All exceptions thrown by the Replicated SDK inherit from this class.
/// It provides common properties for HTTP status codes, response bodies, and error codes
/// that help with debugging and error handling.
/// </remarks>
public class ReplicatedException : Exception
{
    /// <summary>
    /// HTTP status code if applicable.
    /// </summary>
    public int? HttpStatus { get; }

    /// <summary>
    /// HTTP response body if applicable.
    /// </summary>
    public string? HttpBody { get; }

    /// <summary>
    /// JSON response body if applicable.
    /// </summary>
    public Dictionary<string, object>? JsonBody { get; }

    /// <summary>
    /// HTTP response headers if applicable.
    /// </summary>
    public Dictionary<string, string>? Headers { get; }

    /// <summary>
    /// Error code if applicable.
    /// </summary>
    public string? Code { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplicatedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="httpStatus">HTTP status code.</param>
    /// <param name="httpBody">HTTP response body.</param>
    /// <param name="jsonBody">JSON response body.</param>
    /// <param name="headers">HTTP response headers.</param>
    /// <param name="code">Error code.</param>
    public ReplicatedException(
        string message,
        int? httpStatus = null,
        string? httpBody = null,
        Dictionary<string, object>? jsonBody = null,
        Dictionary<string, string>? headers = null,
        string? code = null) : base(message)
    {
        HttpStatus = httpStatus;
        HttpBody = httpBody;
        JsonBody = jsonBody;
        Headers = headers;
        Code = code;
    }

    /// <summary>
    /// Returns a string representation of the exception.
    /// </summary>
    /// <returns>A formatted string with status, code, and message.</returns>
    public override string ToString()
    {
        if (HttpStatus.HasValue && !string.IsNullOrEmpty(Code))
            return $"{HttpStatus} {Code}: {Message}";
        if (HttpStatus.HasValue)
            return $"{HttpStatus}: {Message}";
        return Message;
    }
}

/// <summary>
/// Exception thrown when the Replicated API returns an error response (typically 4xx or 5xx status codes).
/// </summary>
/// <remarks>
/// This exception is thrown when the API returns an error response that indicates a problem with the request
/// or the server. The <see cref="ReplicatedException.HttpStatus"/> property contains the HTTP status code,
/// and <see cref="ReplicatedException.JsonBody"/> or <see cref="ReplicatedException.HttpBody"/> may contain
/// additional error details from the API.
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     var customer = client.Customer.GetOrCreate("user@example.com");
/// }
/// catch (ReplicatedApiError ex)
/// {
///     Console.WriteLine($"API Error: {ex.HttpStatus} - {ex.Message}");
///     if (ex.JsonBody != null)
///         Console.WriteLine($"Error Details: {System.Text.Json.JsonSerializer.Serialize(ex.JsonBody)}");
/// }
/// </code>
/// </example>
public class ReplicatedApiError : ReplicatedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplicatedApiError"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="httpStatus">HTTP status code.</param>
    /// <param name="httpBody">HTTP response body.</param>
    /// <param name="jsonBody">JSON response body.</param>
    /// <param name="headers">HTTP response headers.</param>
    /// <param name="code">Error code.</param>
    public ReplicatedApiError(
        string message,
        int? httpStatus = null,
        string? httpBody = null,
        Dictionary<string, object>? jsonBody = null,
        Dictionary<string, string>? headers = null,
        string? code = null) : base(message, httpStatus, httpBody, jsonBody, headers, code)
    {
    }
}

/// <summary>
/// Exception thrown when an authentication or authorization error occurs (typically 401 Unauthorized or 403 Forbidden).
/// </summary>
/// <remarks>
/// This exception indicates that the request was rejected due to authentication or authorization issues.
/// Common causes include:
/// - Invalid or expired publishable key
/// - Invalid or expired dynamic token (service token)
/// - Insufficient permissions for the requested operation
/// 
/// Check that your publishable key or service token is correct and has the necessary permissions.
/// </remarks>
public class ReplicatedAuthError : ReplicatedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplicatedAuthError"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="httpStatus">HTTP status code.</param>
    /// <param name="httpBody">HTTP response body.</param>
    /// <param name="jsonBody">JSON response body.</param>
    /// <param name="headers">HTTP response headers.</param>
    /// <param name="code">Error code.</param>
    public ReplicatedAuthError(
        string message,
        int? httpStatus = null,
        string? httpBody = null,
        Dictionary<string, object>? jsonBody = null,
        Dictionary<string, string>? headers = null,
        string? code = null) : base(message, httpStatus, httpBody, jsonBody, headers, code)
    {
    }
}

/// <summary>
/// Exception thrown when a rate limit error occurs (typically 429 Too Many Requests).
/// </summary>
/// <remarks>
/// This exception is thrown when too many requests are made in a short period of time.
/// The SDK's retry policy can automatically handle rate limit errors if <see cref="RetryPolicy.RetryOnRateLimit"/> is true.
/// When this exception is thrown, you should wait before making additional requests or implement exponential backoff.
/// </remarks>
public class ReplicatedRateLimitError : ReplicatedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplicatedRateLimitError"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="httpStatus">HTTP status code.</param>
    /// <param name="httpBody">HTTP response body.</param>
    /// <param name="jsonBody">JSON response body.</param>
    /// <param name="headers">HTTP response headers.</param>
    /// <param name="code">Error code.</param>
    public ReplicatedRateLimitError(
        string message,
        int? httpStatus = null,
        string? httpBody = null,
        Dictionary<string, object>? jsonBody = null,
        Dictionary<string, string>? headers = null,
        string? code = null) : base(message, httpStatus, httpBody, jsonBody, headers, code)
    {
    }
}

/// <summary>
/// Exception thrown when a network error occurs during API communication.
/// </summary>
/// <remarks>
/// This exception is thrown when there's a problem with the network connection or communication with the API server.
/// Common causes include:
/// - Network connectivity issues
/// - DNS resolution failures
/// - Connection timeouts
/// - SSL/TLS handshake failures
/// 
/// The SDK's retry policy can automatically retry on network errors if <see cref="RetryPolicy.RetryOnNetworkError"/> is true.
/// </remarks>
public class ReplicatedNetworkError : ReplicatedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplicatedNetworkError"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="httpStatus">HTTP status code.</param>
    /// <param name="httpBody">HTTP response body.</param>
    /// <param name="jsonBody">JSON response body.</param>
    /// <param name="headers">HTTP response headers.</param>
    /// <param name="code">Error code.</param>
    public ReplicatedNetworkError(
        string message,
        int? httpStatus = null,
        string? httpBody = null,
        Dictionary<string, object>? jsonBody = null,
        Dictionary<string, string>? headers = null,
        string? code = null) : base(message, httpStatus, httpBody, jsonBody, headers, code)
    {
    }
}

