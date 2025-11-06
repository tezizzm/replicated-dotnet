using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Replicated.Validation;

/// <summary>
/// Static class for input validation and sanitization.
/// </summary>
public static class InputValidator
{
    private static readonly Regex PublishableKeyRegex = new Regex(@"^replicated_pk_[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
    private static readonly Regex AppSlugRegex = new Regex(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
    private static readonly Regex MetricNameRegex = new Regex(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);
    private static readonly Regex VersionRegex = new Regex(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);
    private static readonly Regex ChannelRegex = new Regex(@"^[a-zA-Z0-9_ -]+$", RegexOptions.Compiled);

    /// <summary>
    /// Validates a publishable key.
    /// </summary>
    public static void ValidatePublishableKey(string publishableKey, string paramName = "publishableKey")
    {
        if (string.IsNullOrWhiteSpace(publishableKey))
            throw new ArgumentException("Publishable key cannot be null or empty", paramName);
        if (!PublishableKeyRegex.IsMatch(publishableKey))
            throw new ArgumentException("Publishable key must start with 'replicated_pk_' followed by alphanumeric characters, underscores, or hyphens", paramName);
    }

    /// <summary>
    /// Validates an app slug.
    /// </summary>
    public static void ValidateAppSlug(string appSlug, string paramName = "appSlug")
    {
        if (string.IsNullOrWhiteSpace(appSlug))
            throw new ArgumentException("App slug cannot be null or empty", paramName);
        if (!AppSlugRegex.IsMatch(appSlug))
            throw new ArgumentException("App slug must contain only alphanumeric characters, underscores, or hyphens", paramName);
    }

    /// <summary>
    /// Validates a base URL.
    /// </summary>
    public static void ValidateBaseUrl(string baseUrl, string paramName = "baseUrl")
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty", paramName);
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException("Base URL must be a valid HTTP or HTTPS URL", paramName);
        if (uri.Scheme != "https")
            throw new ArgumentException("Base URL must use HTTPS protocol", paramName);
    }

    /// <summary>
    /// Validates a timeout value.
    /// </summary>
    public static void ValidateTimeout(TimeSpan timeout, string paramName = "timeout")
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero", paramName);
        if (timeout.TotalHours > 1)
            throw new ArgumentException("Timeout cannot exceed 1 hour", paramName);
    }

    /// <summary>
    /// Validates and sanitizes an email address.
    /// </summary>
    public static string ValidateAndSanitizeEmail(string emailAddress, string paramName = "emailAddress")
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
            throw new ArgumentException("Email address cannot be null or empty", paramName);

        // Trim and lowercase
        var sanitized = emailAddress.Trim().ToLowerInvariant();

        // Basic email format validation
        if (sanitized.Length > 254)
            throw new ArgumentException("Email address is too long (max 254 characters)", paramName);

        var parts = sanitized.Split('@');
        if (!sanitized.Contains('@') || parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
            throw new ArgumentException("Invalid email address format", paramName);

        return sanitized;
    }

    /// <summary>
    /// Validates a channel name.
    /// </summary>
    public static void ValidateChannel(string channel, string paramName = "channel")
    {
        if (string.IsNullOrWhiteSpace(channel))
            throw new ArgumentException("Channel cannot be null or empty", paramName);
        if (!ChannelRegex.IsMatch(channel))
            throw new ArgumentException("Channel can only contain alphanumeric characters, underscores, hyphens, and spaces", paramName);
    }

    /// <summary>
    /// Validates a customer name.
    /// </summary>
    public static void ValidateCustomerName(string? name, string paramName = "name")
    {
        if (name != null && name.Length > 255)
            throw new ArgumentException("Customer name cannot exceed 255 characters", paramName);
    }

    /// <summary>
    /// Validates a metric name.
    /// </summary>
    public static void ValidateMetricName(string metricName, string paramName = "metricName")
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be null or empty", paramName);
        if (metricName.Length > 100)
            throw new ArgumentException("Metric name cannot exceed 100 characters", paramName);
        if (!MetricNameRegex.IsMatch(metricName))
            throw new ArgumentException("Metric name can only contain alphanumeric characters and underscores", paramName);
    }

    /// <summary>
    /// Validates an instance status.
    /// </summary>
    public static void ValidateInstanceStatus(string status, string paramName = "status")
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Instance status cannot be null or empty", paramName);

        // Validate against enum values (case-insensitive)
        var validStatuses = Enum.GetNames(typeof(InstanceStatus))
            .Select(s => s.ToLowerInvariant())
            .ToList();

        if (!validStatuses.Contains(status.ToLowerInvariant()))
            throw new ArgumentException($"Invalid status. Instance status must be one of: {string.Join(", ", validStatuses)}", paramName);
    }

    /// <summary>
    /// Validates a version string.
    /// </summary>
    public static void ValidateVersion(string version, string paramName = "version")
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version cannot be null or empty", paramName);
        if (version.Length > 50)
            throw new ArgumentException("Version cannot exceed 50 characters", paramName);
        if (!VersionRegex.IsMatch(version))
            throw new ArgumentException("Version can only contain alphanumeric characters, dots, underscores, and hyphens", paramName);
    }

    /// <summary>
    /// Validates an HTTP method.
    /// </summary>
    public static void ValidateHttpMethod(string method, string paramName = "method")
    {
        if (string.IsNullOrWhiteSpace(method))
            throw new ArgumentException("HTTP method cannot be null or empty", paramName);

        var validMethods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" };
        if (!validMethods.Contains(method.ToUpperInvariant()))
            throw new ArgumentException($"HTTP method must be one of: {string.Join(", ", validMethods)}", paramName);
    }

    /// <summary>
    /// Validates a URL path.
    /// </summary>
    public static void ValidateUrlPath(string url, string paramName = "url")
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL path cannot be null or empty", paramName);
        if (!url.StartsWith("/"))
            throw new ArgumentException("URL path must start with '/'", paramName);
    }

    /// <summary>
    /// Validates HTTP headers.
    /// </summary>
    public static void ValidateHeaders(Dictionary<string, string>? headers, string paramName = "headers")
    {
        if (headers == null)
            return;

        foreach (var header in headers)
        {
            if (string.IsNullOrWhiteSpace(header.Key))
                throw new ArgumentException("Header key cannot be null or empty", paramName);
            if (header.Value == null)
                throw new ArgumentException($"Header value for '{header.Key}' cannot be null", paramName);
        }
    }

    /// <summary>
    /// Validates HTTP parameters.
    /// </summary>
    public static void ValidateParameters(Dictionary<string, object>? parameters, string paramName = "parameters")
    {
        if (parameters == null)
            return;

        foreach (var param in parameters)
        {
            if (string.IsNullOrWhiteSpace(param.Key))
                throw new ArgumentException("Parameter key cannot be null or empty", paramName);
        }
    }
}

