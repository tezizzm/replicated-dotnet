using System;
using System.Collections.Generic;

namespace Replicated;

/// <summary>
/// Configuration for retry behavior when making API requests.
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Maximum number of retry attempts (default: 3).
    /// Set to 0 to disable retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay before first retry (default: 1 second).
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between retries (default: 30 seconds).
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Multiplier for exponential backoff (default: 2.0).
    /// Delay = InitialDelay * (BackoffMultiplier ^ retryAttempt)
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Whether to add jitter to prevent synchronized retries (default: true).
    /// Jitter adds random variation to delay times to prevent many clients
    /// from retrying at the same time (thundering herd problem).
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Jitter percentage (0.0 to 1.0, default: 0.1 = 10%).
    /// The actual delay will vary by ±(JitterPercentage * delay).
    /// </summary>
    public double JitterPercentage { get; set; } = 0.1;

    /// <summary>
    /// Whether to retry on rate limit errors (429, default: true).
    /// </summary>
    public bool RetryOnRateLimit { get; set; } = true;

    /// <summary>
    /// Whether to retry on server errors (5xx, default: true).
    /// </summary>
    public bool RetryOnServerError { get; set; } = true;

    /// <summary>
    /// Whether to retry on network errors (default: true).
    /// </summary>
    public bool RetryOnNetworkError { get; set; } = true;

    /// <summary>
    /// Custom function to determine if an exception should be retried.
    /// If null, uses default retry logic based on exception type.
    /// Return true to retry, false to not retry.
    /// </summary>
    /// <remarks>
    /// The function receives the exception and the current attempt number (0-indexed).
    /// Return true to retry the request, false to stop retrying.
    /// </remarks>
    public Func<Exception, int, bool>? ShouldRetry { get; set; }

    /// <summary>
    /// Validates the retry policy configuration.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    internal void Validate()
    {
        if (MaxRetries < 0)
            throw new ArgumentException("MaxRetries cannot be negative", nameof(MaxRetries));

        if (InitialDelay <= TimeSpan.Zero)
            throw new ArgumentException("InitialDelay must be greater than zero", nameof(InitialDelay));

        if (MaxDelay <= TimeSpan.Zero)
            throw new ArgumentException("MaxDelay must be greater than zero", nameof(MaxDelay));

        if (InitialDelay > MaxDelay)
            throw new ArgumentException("InitialDelay cannot exceed MaxDelay", nameof(InitialDelay));

        if (BackoffMultiplier <= 0)
            throw new ArgumentException("BackoffMultiplier must be greater than zero", nameof(BackoffMultiplier));

        if (JitterPercentage < 0 || JitterPercentage > 1)
            throw new ArgumentException("JitterPercentage must be between 0.0 and 1.0", nameof(JitterPercentage));
    }
}

