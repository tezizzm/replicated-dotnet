using System;

namespace Replicated.Configuration;

/// <summary>
/// Reads SDK configuration from environment variables.
/// </summary>
internal static class EnvironmentConfigReader
{
    /// <summary>
    /// Reads the base URL from <c>REPLICATED_SDK_ENDPOINT</c>.
    /// Returns null if not set.
    /// </summary>
    public static string? GetBaseUrl()
        => Environment.GetEnvironmentVariable(EnvironmentVariables.BaseUrl);

    /// <summary>
    /// Reads the request timeout from <c>REPLICATED_TIMEOUT</c>.
    /// Accepts an integer (seconds) or TimeSpan string.
    /// Returns <paramref name="defaultTimeout"/> if not set or invalid.
    /// </summary>
    public static TimeSpan GetTimeout(TimeSpan defaultTimeout)
    {
        var raw = Environment.GetEnvironmentVariable(EnvironmentVariables.Timeout);
        if (string.IsNullOrWhiteSpace(raw)) return defaultTimeout;

        if (int.TryParse(raw, out var seconds) && seconds > 0)
            return TimeSpan.FromSeconds(seconds);

        if (TimeSpan.TryParse(raw, out var parsed))
            return parsed;

        return defaultTimeout;
    }

    /// <summary>
    /// Reads retry policy configuration from environment variables.
    /// Returns null if no retry variables are set.
    /// </summary>
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

        if (int.TryParse(maxRetriesStr, out var maxRetries) && maxRetries >= 0)
            policy.MaxRetries = maxRetries;

        if (int.TryParse(initialDelayStr, out var initialDelayMs) && initialDelayMs > 0)
            policy.InitialDelay = TimeSpan.FromMilliseconds(initialDelayMs);

        if (int.TryParse(maxDelayStr, out var maxDelayMs) && maxDelayMs > 0)
            policy.MaxDelay = TimeSpan.FromMilliseconds(maxDelayMs);

        if (double.TryParse(backoffMultiplierStr, out var backoffMultiplier) && backoffMultiplier > 0)
            policy.BackoffMultiplier = backoffMultiplier;

        if (bool.TryParse(useJitterStr, out var useJitter))
            policy.UseJitter = useJitter;

        if (double.TryParse(jitterPercentageStr, out var jitterPct) && jitterPct >= 0 && jitterPct <= 1)
            policy.JitterPercentage = jitterPct;

        if (bool.TryParse(retryOnRateLimitStr, out var retryOnRateLimit))
            policy.RetryOnRateLimit = retryOnRateLimit;

        if (bool.TryParse(retryOnServerErrorStr, out var retryOnServerError))
            policy.RetryOnServerError = retryOnServerError;

        if (bool.TryParse(retryOnNetworkErrorStr, out var retryOnNetworkError))
            policy.RetryOnNetworkError = retryOnNetworkError;

        return policy;
    }
}
