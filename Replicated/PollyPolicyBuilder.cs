using System;
using System.Collections.Generic;
using Polly;
using Polly.Retry;

namespace Replicated;

/// <summary>
/// Internal class that builds Polly retry policies from our RetryPolicy configuration.
/// This keeps Polly as an internal dependency only.
/// </summary>
internal static class PollyPolicyBuilder
{
    /// <summary>
    /// Builds a Polly retry policy from our RetryPolicy configuration.
    /// </summary>
    /// <param name="policy">The retry policy configuration. If null or MaxRetries is 0, returns a no-op policy.</param>
    /// <returns>A Polly async policy that will retry based on the configuration.</returns>
    public static IAsyncPolicy<Dictionary<string, object>> BuildRetryPolicy(RetryPolicy? policy)
    {
        // If no policy or retries disabled, return no-op policy
        if (policy == null || policy.MaxRetries == 0)
        {
            return Policy.NoOpAsync<Dictionary<string, object>>();
        }

        // Validate policy configuration
        policy.Validate();

        // Build the retry policy builder with exception handling
        var policyBuilder = Policy<Dictionary<string, object>>
            .Handle<ReplicatedNetworkError>(ex => policy.RetryOnNetworkError)
            .Or<ReplicatedRateLimitError>(ex => policy.RetryOnRateLimit)
            .Or<ReplicatedApiError>(ex => 
                (ex.HttpStatus >= 500 && policy.RetryOnServerError) ||
                (ex.HttpStatus == 429 && policy.RetryOnRateLimit)
            );

        // Build delay function with exponential backoff and jitter
        Func<int, TimeSpan> delayFunc = retryAttempt =>
        {
            // Exponential backoff: delay = initialDelay * (multiplier ^ retryAttempt)
            var delay = TimeSpan.FromMilliseconds(
                policy.InitialDelay.TotalMilliseconds * 
                Math.Pow(policy.BackoffMultiplier, retryAttempt)
            );

            // Cap at maximum delay
            if (delay > policy.MaxDelay)
                delay = policy.MaxDelay;

            // Add jitter if enabled
            if (policy.UseJitter)
            {
                var random = new Random();
                var jitterRange = delay.TotalMilliseconds * policy.JitterPercentage;
                var jitter = (random.NextDouble() * 2 - 1) * jitterRange; // -jitterRange to +jitterRange
                delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
            }

            return delay;
        };

        // Handle custom retry logic - when ShouldRetry is provided, use only that
        if (policy.ShouldRetry != null)
        {
            // Use custom retry logic for all exceptions
            return Policy<Dictionary<string, object>>
                .Handle<Exception>(ex => policy.ShouldRetry(ex, 0))
                .WaitAndRetryAsync(
                    policy.MaxRetries,
                    delayFunc,
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        // Validate custom retry logic per attempt
                        // Note: Polly will call this before each retry, but we can't stop mid-retry
                        // The ShouldRetry delegate is called during Handle<> which happens before retry
                        if (outcome.Exception != null && !policy.ShouldRetry(outcome.Exception, retryCount))
                        {
                            // This shouldn't happen since Handle<> filters, but we check anyway
                        }
                    }
                );
        }

        // Standard retry policy without custom logic
        return policyBuilder.WaitAndRetryAsync(
            policy.MaxRetries,
            delayFunc
        );
    }

    /// <summary>
    /// Builds a synchronous Polly retry policy for sync operations.
    /// </summary>
    /// <param name="policy">The retry policy configuration.</param>
    /// <returns>A Polly sync policy that will retry based on the configuration.</returns>
    public static ISyncPolicy<Dictionary<string, object>> BuildSyncRetryPolicy(RetryPolicy? policy)
    {
        // If no policy or retries disabled, return no-op policy
        if (policy == null || policy.MaxRetries == 0)
        {
            return Policy.NoOp<Dictionary<string, object>>();
        }

        // Validate policy configuration
        policy.Validate();

        // Build the retry policy builder with exception handling
        var policyBuilder = Policy<Dictionary<string, object>>
            .Handle<ReplicatedNetworkError>(ex => policy.RetryOnNetworkError)
            .Or<ReplicatedRateLimitError>(ex => policy.RetryOnRateLimit)
            .Or<ReplicatedApiError>(ex => 
                (ex.HttpStatus >= 500 && policy.RetryOnServerError) ||
                (ex.HttpStatus == 429 && policy.RetryOnRateLimit)
            );

        // Build delay function with exponential backoff and jitter
        Func<int, TimeSpan> delayFunc = retryAttempt =>
        {
            var delay = TimeSpan.FromMilliseconds(
                policy.InitialDelay.TotalMilliseconds * 
                Math.Pow(policy.BackoffMultiplier, retryAttempt)
            );

            if (delay > policy.MaxDelay)
                delay = policy.MaxDelay;

            if (policy.UseJitter)
            {
                var random = new Random();
                var jitterRange = delay.TotalMilliseconds * policy.JitterPercentage;
                var jitter = (random.NextDouble() * 2 - 1) * jitterRange;
                delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
            }

            return delay;
        };

        // Handle custom retry logic - when ShouldRetry is provided, use only that
        if (policy.ShouldRetry != null)
        {
            // Use custom retry logic for all exceptions
            return Policy<Dictionary<string, object>>
                .Handle<Exception>(ex => policy.ShouldRetry(ex, 0))
                .WaitAndRetry(
                    policy.MaxRetries,
                    delayFunc
                );
        }

        // Standard retry policy
        return policyBuilder.WaitAndRetry(
            policy.MaxRetries,
            delayFunc
        );
    }
}

