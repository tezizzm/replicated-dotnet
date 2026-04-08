using System;
using Polly;
using Polly.Retry;

namespace Replicated;

/// <summary>
/// Builds Polly retry policies from <see cref="RetryPolicy"/> configuration.
/// Returns non-generic <see cref="IAsyncPolicy"/> so it is compatible with the
/// typed <c>ExecuteAsync&lt;TResult&gt;</c> overloads on <see cref="IAsyncPolicy"/>.
/// </summary>
internal static class PollyPolicyBuilder
{
    internal static IAsyncPolicy BuildRetryPolicy(
        RetryPolicy? policy,
        Action<Exception, TimeSpan, int>? onRetry = null)
    {
        if (policy == null || policy.MaxRetries == 0)
            return Policy.NoOpAsync();

        policy.Validate();

        Func<int, TimeSpan> delayFunc = retryAttempt =>
        {
            var delay = TimeSpan.FromMilliseconds(
                policy.InitialDelay.TotalMilliseconds *
                Math.Pow(policy.BackoffMultiplier, retryAttempt));

            if (delay > policy.MaxDelay)
                delay = policy.MaxDelay;

            if (policy.UseJitter)
            {
                var jitterRange = delay.TotalMilliseconds * policy.JitterPercentage;
                var jitter = (Random.Shared.NextDouble() * 2 - 1) * jitterRange;
                delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
            }

            return delay;
        };

        if (policy.ShouldRetry != null)
        {
            // Track the attempt number so the ShouldRetry predicate receives the correct value.
            // lastAttempt is updated in onRetry (which fires after the predicate for the previous
            // failure), so by the time the predicate is evaluated for retry N, lastAttempt == N-1.
            var lastAttempt = 0;
            return Policy
                .Handle<Exception>(ex => policy.ShouldRetry(ex, lastAttempt))
                .WaitAndRetryAsync(
                    policy.MaxRetries,
                    delayFunc,
                    onRetry: (ex, ts, attempt, _) =>
                    {
                        lastAttempt = attempt;
                        onRetry?.Invoke(ex, ts, attempt);
                    });
        }

        return Policy
            .Handle<ReplicatedNetworkError>(ex => policy.RetryOnNetworkError)
            .Or<ReplicatedRateLimitError>(ex => policy.RetryOnRateLimit)
            .Or<ReplicatedApiError>(ex =>
                (ex.HttpStatus >= 500 && policy.RetryOnServerError) ||
                (ex.HttpStatus == 429 && policy.RetryOnRateLimit))
            .WaitAndRetryAsync(
                policy.MaxRetries,
                delayFunc,
                onRetry: (ex, ts, attempt, _) => onRetry?.Invoke(ex, ts, attempt));
    }
}
