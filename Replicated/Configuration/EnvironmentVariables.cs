namespace Replicated.Configuration;

/// <summary>
/// Environment variable names recognised by the Replicated SDK.
/// </summary>
public static class EnvironmentVariables
{
    /// <summary>
    /// Override the URL of the Replicated in-cluster service.
    /// Defaults to <c>http://replicated:3000</c>.
    /// </summary>
    public const string BaseUrl = "REPLICATED_SDK_ENDPOINT";

    /// <summary>Request timeout in seconds (or as a TimeSpan string).</summary>
    public const string Timeout = "REPLICATED_TIMEOUT";

    /// <summary>Maximum number of retry attempts.</summary>
    public const string MaxRetries = "REPLICATED_MAX_RETRIES";

    /// <summary>Initial retry delay in milliseconds.</summary>
    public const string RetryInitialDelay = "REPLICATED_RETRY_INITIAL_DELAY";

    /// <summary>Maximum retry delay in milliseconds.</summary>
    public const string RetryMaxDelay = "REPLICATED_RETRY_MAX_DELAY";

    /// <summary>Exponential backoff multiplier.</summary>
    public const string RetryBackoffMultiplier = "REPLICATED_RETRY_BACKOFF_MULTIPLIER";

    /// <summary>Whether to add jitter to retry delays (true/false).</summary>
    public const string RetryUseJitter = "REPLICATED_RETRY_USE_JITTER";

    /// <summary>Jitter percentage as a decimal (0.0–1.0).</summary>
    public const string RetryJitterPercentage = "REPLICATED_RETRY_JITTER_PERCENTAGE";

    /// <summary>Whether to retry on 429 rate-limit responses (true/false).</summary>
    public const string RetryOnRateLimit = "REPLICATED_RETRY_ON_RATE_LIMIT";

    /// <summary>Whether to retry on 5xx server errors (true/false).</summary>
    public const string RetryOnServerError = "REPLICATED_RETRY_ON_SERVER_ERROR";

    /// <summary>Whether to retry on network errors (true/false).</summary>
    public const string RetryOnNetworkError = "REPLICATED_RETRY_ON_NETWORK_ERROR";
}
