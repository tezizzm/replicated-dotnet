using System;
using System.Collections.Generic;

namespace Replicated.Configuration;

/// <summary>
/// Helper class for reading configuration from environment variables.
/// </summary>
internal static class EnvironmentConfigReader
{
    /// <summary>
    /// Reads the publishable key from environment variables.
    /// </summary>
    /// <returns>The publishable key if found, null otherwise.</returns>
    public static string? GetPublishableKey()
    {
        return Environment.GetEnvironmentVariable(EnvironmentVariables.PublishableKey);
    }

    /// <summary>
    /// Reads the app slug from environment variables.
    /// </summary>
    /// <returns>The app slug if found, null otherwise.</returns>
    public static string? GetAppSlug()
    {
        return Environment.GetEnvironmentVariable(EnvironmentVariables.AppSlug);
    }

    /// <summary>
    /// Reads the base URL from environment variables.
    /// </summary>
    /// <returns>The base URL if found, null otherwise.</returns>
    public static string? GetBaseUrl()
    {
        return Environment.GetEnvironmentVariable(EnvironmentVariables.BaseUrl);
    }

    /// <summary>
    /// Reads the timeout from environment variables.
    /// </summary>
    /// <param name="defaultTimeout">The default timeout to use if not specified.</param>
    /// <returns>The timeout if found and valid, otherwise the default.</returns>
    public static TimeSpan GetTimeout(TimeSpan defaultTimeout)
    {
        var timeoutStr = Environment.GetEnvironmentVariable(EnvironmentVariables.Timeout);
        if (string.IsNullOrWhiteSpace(timeoutStr))
            return defaultTimeout;

        if (int.TryParse(timeoutStr, out var seconds) && seconds > 0)
            return TimeSpan.FromSeconds(seconds);

        // Try parsing as TimeSpan format (e.g., "00:05:00")
        if (TimeSpan.TryParse(timeoutStr, out var parsedTimeout))
            return parsedTimeout;

        return defaultTimeout;
    }

    /// <summary>
    /// Reads the state directory from environment variables.
    /// </summary>
    /// <returns>The state directory if found, null otherwise.</returns>
    public static string? GetStateDirectory()
    {
        return Environment.GetEnvironmentVariable(EnvironmentVariables.StateDirectory);
    }

    /// <summary>
    /// Creates a RetryPolicy from environment variables.
    /// Only creates a policy if at least one retry-related environment variable is set.
    /// </summary>
    /// <returns>A RetryPolicy configured from environment variables, or null if none are set.</returns>
    public static RetryPolicy? GetRetryPolicy()
    {
        var maxRetriesStr = Environment.GetEnvironmentVariable(EnvironmentVariables.MaxRetries);
        var initialDelayStr = Environment.GetEnvironmentVariable(EnvironmentVariables.RetryInitialDelay);
        var maxDelayStr = Environment.GetEnvironmentVariable(EnvironmentVariables.RetryMaxDelay);
        var backoffMultiplierStr = Environment.GetEnvironmentVariable(EnvironmentVariables.RetryBackoffMultiplier);
        var useJitterStr = Environment.GetEnvironmentVariable(EnvironmentVariables.RetryUseJitter);
        var jitterPercentageStr = Environment.GetEnvironmentVariable(EnvironmentVariables.RetryJitterPercentage);
        var retryOnRateLimitStr = Environment.GetEnvironmentVariable(EnvironmentVariables.RetryOnRateLimit);
        var retryOnServerErrorStr = Environment.GetEnvironmentVariable(EnvironmentVariables.RetryOnServerError);
        var retryOnNetworkErrorStr = Environment.GetEnvironmentVariable(EnvironmentVariables.RetryOnNetworkError);

        // If no retry-related environment variables are set, return null
        if (string.IsNullOrWhiteSpace(maxRetriesStr) &&
            string.IsNullOrWhiteSpace(initialDelayStr) &&
            string.IsNullOrWhiteSpace(maxDelayStr) &&
            string.IsNullOrWhiteSpace(backoffMultiplierStr) &&
            string.IsNullOrWhiteSpace(useJitterStr) &&
            string.IsNullOrWhiteSpace(jitterPercentageStr) &&
            string.IsNullOrWhiteSpace(retryOnRateLimitStr) &&
            string.IsNullOrWhiteSpace(retryOnServerErrorStr) &&
            string.IsNullOrWhiteSpace(retryOnNetworkErrorStr))
        {
            return null;
        }

        var policy = new RetryPolicy();

        // Parse max retries
        if (int.TryParse(maxRetriesStr, out var maxRetries) && maxRetries >= 0)
        {
            policy.MaxRetries = maxRetries;
        }

        // Parse initial delay (in milliseconds)
        if (int.TryParse(initialDelayStr, out var initialDelayMs) && initialDelayMs > 0)
        {
            policy.InitialDelay = TimeSpan.FromMilliseconds(initialDelayMs);
        }

        // Parse max delay (in milliseconds)
        if (int.TryParse(maxDelayStr, out var maxDelayMs) && maxDelayMs > 0)
        {
            policy.MaxDelay = TimeSpan.FromMilliseconds(maxDelayMs);
        }

        // Parse backoff multiplier
        if (double.TryParse(backoffMultiplierStr, out var backoffMultiplier) && backoffMultiplier > 0)
        {
            policy.BackoffMultiplier = backoffMultiplier;
        }

        // Parse use jitter
        if (bool.TryParse(useJitterStr, out var useJitter))
        {
            policy.UseJitter = useJitter;
        }

        // Parse jitter percentage
        if (double.TryParse(jitterPercentageStr, out var jitterPercentage) && jitterPercentage >= 0 && jitterPercentage <= 1)
        {
            policy.JitterPercentage = jitterPercentage;
        }

        // Parse retry on rate limit
        if (bool.TryParse(retryOnRateLimitStr, out var retryOnRateLimit))
        {
            policy.RetryOnRateLimit = retryOnRateLimit;
        }

        // Parse retry on server error
        if (bool.TryParse(retryOnServerErrorStr, out var retryOnServerError))
        {
            policy.RetryOnServerError = retryOnServerError;
        }

        // Parse retry on network error
        if (bool.TryParse(retryOnNetworkErrorStr, out var retryOnNetworkError))
        {
            policy.RetryOnNetworkError = retryOnNetworkError;
        }

        return policy;
    }
}

