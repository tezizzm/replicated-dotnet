using System;
using System.Collections.Generic;

namespace Replicated.Validation;

/// <summary>
/// Input validation helpers for the Replicated SDK.
/// </summary>
public static class InputValidator
{
    /// <summary>
    /// Validates a base URL. Accepts both HTTP (for the in-cluster service) and HTTPS.
    /// </summary>
    public static void ValidateBaseUrl(string baseUrl, string paramName = "baseUrl")
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty.", paramName);

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("Base URL must be a valid absolute URL.", paramName);

        if (uri.Scheme != "http" && uri.Scheme != "https")
            throw new ArgumentException("Base URL must use HTTP or HTTPS.", paramName);
    }

    /// <summary>
    /// Validates a timeout value.
    /// </summary>
    public static void ValidateTimeout(TimeSpan timeout, string paramName = "timeout")
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero.", paramName);
        if (timeout.TotalHours > 1)
            throw new ArgumentException("Timeout cannot exceed 1 hour.", paramName);
    }

    /// <summary>
    /// Validates a URL path (must start with '/').
    /// </summary>
    internal static void ValidateUrlPath(string path, string paramName = "path")
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("URL path cannot be null or empty.", paramName);
        if (!path.StartsWith('/'))
            throw new ArgumentException("URL path must start with '/'.", paramName);
    }

    /// <summary>
    /// Validates HTTP headers dictionary (keys must be non-empty).
    /// </summary>
    internal static void ValidateHeaders(Dictionary<string, string>? headers)
    {
        if (headers == null) return;
        foreach (var key in headers.Keys)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Header name cannot be null or empty.");
        }
    }
}
