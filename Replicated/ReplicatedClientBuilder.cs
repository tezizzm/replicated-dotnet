using System;
using Microsoft.Extensions.Logging;
using Replicated.Configuration;
using Replicated.Validation;

namespace Replicated;

/// <summary>
/// Fluent builder for <see cref="ReplicatedClient"/>.
/// </summary>
public class ReplicatedClientBuilder
{
    private string? _baseUrl;
    private TimeSpan? _timeout;
    private RetryPolicy? _retryPolicy;
    private ILogger? _logger;
    private bool _fromEnvironment;

    /// <summary>
    /// Sets the base URL of the Replicated in-cluster service.
    /// Defaults to <c>http://replicated:3000</c>.
    /// </summary>
    public ReplicatedClientBuilder WithBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        return this;
    }

    /// <summary>Sets the request timeout.</summary>
    public ReplicatedClientBuilder WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    /// <summary>Sets the retry policy.</summary>
    public ReplicatedClientBuilder WithRetryPolicy(RetryPolicy retryPolicy)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        return this;
    }

    /// <summary>Configures retry policy with custom settings.</summary>
    public ReplicatedClientBuilder WithRetryPolicy(
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        double backoffMultiplier = 2.0,
        bool useJitter = true)
    {
        _retryPolicy = new RetryPolicy
        {
            MaxRetries = maxRetries,
            InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1),
            MaxDelay = maxDelay ?? TimeSpan.FromSeconds(30),
            BackoffMultiplier = backoffMultiplier,
            UseJitter = useJitter
        };
        return this;
    }

    /// <summary>Disables automatic retries.</summary>
    public ReplicatedClientBuilder WithoutRetries()
    {
        _retryPolicy = new RetryPolicy { MaxRetries = 0 };
        return this;
    }

    /// <summary>
    /// Sets the logger. When provided, HTTP requests, responses, and retries are logged.
    /// </summary>
    public ReplicatedClientBuilder WithLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        return this;
    }

    /// <summary>
    /// Reads base URL and retry configuration from environment variables.
    /// Explicitly set values take precedence.
    /// </summary>
    public ReplicatedClientBuilder FromEnvironment()
    {
        _fromEnvironment = true;
        return this;
    }

    /// <summary>Builds the <see cref="ReplicatedClient"/> with the configured options.</summary>
    public ReplicatedClient Build()
    {
        string resolvedBaseUrl;
        TimeSpan resolvedTimeout;
        RetryPolicy? resolvedRetryPolicy;

        if (_fromEnvironment)
        {
            resolvedBaseUrl = _baseUrl ?? EnvironmentConfigReader.GetBaseUrl() ?? Constants.DefaultBaseUrl;
            resolvedTimeout = _timeout ?? EnvironmentConfigReader.GetTimeout(TimeSpan.FromSeconds(30));
            resolvedRetryPolicy = _retryPolicy ?? EnvironmentConfigReader.GetRetryPolicy();
        }
        else
        {
            resolvedBaseUrl = _baseUrl ?? Constants.DefaultBaseUrl;
            resolvedTimeout = _timeout ?? TimeSpan.FromSeconds(30);
            resolvedRetryPolicy = _retryPolicy;
        }

        if (_timeout.HasValue)
            InputValidator.ValidateTimeout(_timeout.Value);

        if (_baseUrl != null)
            InputValidator.ValidateBaseUrl(_baseUrl);

        resolvedRetryPolicy?.Validate();

        return new ReplicatedClient(
            baseUrl: resolvedBaseUrl,
            timeout: resolvedTimeout,
            retryPolicy: resolvedRetryPolicy,
            logger: _logger);
    }
}
