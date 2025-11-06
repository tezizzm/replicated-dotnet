namespace Replicated.Configuration;

/// <summary>
/// Constants for environment variable names used by the SDK.
/// </summary>
public static class EnvironmentVariables
{
    /// <summary>
    /// Environment variable name for the publishable key.
    /// </summary>
    public const string PublishableKey = "REPLICATED_PUBLISHABLE_KEY";

    /// <summary>
    /// Environment variable name for the app slug.
    /// </summary>
    public const string AppSlug = "REPLICATED_APP_SLUG";

    /// <summary>
    /// Environment variable name for the base URL.
    /// </summary>
    public const string BaseUrl = "REPLICATED_BASE_URL";

    /// <summary>
    /// Environment variable name for the request timeout in seconds.
    /// </summary>
    public const string Timeout = "REPLICATED_TIMEOUT";

    /// <summary>
    /// Environment variable name for the custom state directory.
    /// </summary>
    public const string StateDirectory = "REPLICATED_STATE_DIRECTORY";

    /// <summary>
    /// Environment variable name for maximum retry attempts.
    /// </summary>
    public const string MaxRetries = "REPLICATED_MAX_RETRIES";

    /// <summary>
    /// Environment variable name for retry initial delay (in milliseconds).
    /// </summary>
    public const string RetryInitialDelay = "REPLICATED_RETRY_INITIAL_DELAY";

    /// <summary>
    /// Environment variable name for retry maximum delay (in milliseconds).
    /// </summary>
    public const string RetryMaxDelay = "REPLICATED_RETRY_MAX_DELAY";

    /// <summary>
    /// Environment variable name for retry backoff multiplier.
    /// </summary>
    public const string RetryBackoffMultiplier = "REPLICATED_RETRY_BACKOFF_MULTIPLIER";

    /// <summary>
    /// Environment variable name for whether to use jitter in retries.
    /// </summary>
    public const string RetryUseJitter = "REPLICATED_RETRY_USE_JITTER";

    /// <summary>
    /// Environment variable name for retry jitter percentage (0.0 to 1.0).
    /// </summary>
    public const string RetryJitterPercentage = "REPLICATED_RETRY_JITTER_PERCENTAGE";

    /// <summary>
    /// Environment variable name for whether to retry on rate limit errors.
    /// </summary>
    public const string RetryOnRateLimit = "REPLICATED_RETRY_ON_RATE_LIMIT";

    /// <summary>
    /// Environment variable name for whether to retry on server errors.
    /// </summary>
    public const string RetryOnServerError = "REPLICATED_RETRY_ON_SERVER_ERROR";

    /// <summary>
    /// Environment variable name for whether to retry on network errors.
    /// </summary>
    public const string RetryOnNetworkError = "REPLICATED_RETRY_ON_NETWORK_ERROR";
}

