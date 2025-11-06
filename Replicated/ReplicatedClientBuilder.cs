using System;
using System.Collections.Generic;
using Replicated.Configuration;
using Replicated.Validation;

namespace Replicated;

/// <summary>
/// Builder class for creating ReplicatedClient instances with fluent configuration.
/// </summary>
public class ReplicatedClientBuilder
{
    private string? _publishableKey;
    private string? _appSlug;
    private string? _baseUrl;
    private TimeSpan? _timeout;
    private string? _stateDirectory;
    private RetryPolicy? _retryPolicy;
    private bool _mergeWithEnvironment = false;

    /// <summary>
    /// Sets the publishable key.
    /// </summary>
    /// <param name="publishableKey">The Replicated publishable key.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ReplicatedClientBuilder WithPublishableKey(string publishableKey)
    {
        _publishableKey = publishableKey ?? throw new ArgumentNullException(nameof(publishableKey));
        return this;
    }

    /// <summary>
    /// Sets the application slug.
    /// </summary>
    /// <param name="appSlug">The application slug.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ReplicatedClientBuilder WithAppSlug(string appSlug)
    {
        _appSlug = appSlug ?? throw new ArgumentNullException(nameof(appSlug));
        return this;
    }

    /// <summary>
    /// Sets the base URL.
    /// </summary>
    /// <param name="baseUrl">The base URL for the API.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ReplicatedClientBuilder WithBaseUrl(string baseUrl)
    {
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        return this;
    }

    /// <summary>
    /// Sets the request timeout.
    /// </summary>
    /// <param name="timeout">The request timeout.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ReplicatedClientBuilder WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Sets the state directory.
    /// </summary>
    /// <param name="stateDirectory">The custom state directory.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ReplicatedClientBuilder WithStateDirectory(string stateDirectory)
    {
        _stateDirectory = stateDirectory ?? throw new ArgumentNullException(nameof(stateDirectory));
        return this;
    }

    /// <summary>
    /// Sets the retry policy.
    /// </summary>
    /// <param name="retryPolicy">The retry policy configuration.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ReplicatedClientBuilder WithRetryPolicy(RetryPolicy retryPolicy)
    {
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        return this;
    }

    /// <summary>
    /// Configures retry policy with custom settings.
    /// </summary>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 3).</param>
    /// <param name="initialDelay">Initial delay before first retry (default: 1 second).</param>
    /// <param name="maxDelay">Maximum delay between retries (default: 30 seconds).</param>
    /// <param name="backoffMultiplier">Multiplier for exponential backoff (default: 2.0).</param>
    /// <param name="useJitter">Whether to add jitter to prevent synchronized retries (default: true).</param>
    /// <returns>The builder instance for method chaining.</returns>
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

    /// <summary>
    /// Disables automatic retries.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public ReplicatedClientBuilder WithoutRetries()
    {
        _retryPolicy = new RetryPolicy { MaxRetries = 0 };
        return this;
    }

    /// <summary>
    /// Merges configuration with environment variables. Any explicitly set values will override environment variables.
    /// </summary>
    /// <returns>The builder instance for method chaining.</returns>
    public ReplicatedClientBuilder FromEnvironment()
    {
        _mergeWithEnvironment = true;
        return this;
    }

    /// <summary>
    /// Builds the ReplicatedClient instance with the configured options.
    /// </summary>
    /// <returns>A configured ReplicatedClient instance.</returns>
    /// <exception cref="ArgumentException">Thrown when required configuration is missing or invalid.</exception>
    public ReplicatedClient Build()
    {
        // Resolve configuration with precedence: explicit values > environment variables > defaults
        string? publishableKey;
        string? appSlug;
        string? baseUrl;
        TimeSpan timeout;
        string? stateDirectory;
        RetryPolicy? retryPolicy;

        if (_mergeWithEnvironment)
        {
            // Merge explicit values with environment variables
            publishableKey = _publishableKey ?? EnvironmentConfigReader.GetPublishableKey();
            appSlug = _appSlug ?? EnvironmentConfigReader.GetAppSlug();
            baseUrl = _baseUrl ?? EnvironmentConfigReader.GetBaseUrl() ?? "https://replicated.app";
            timeout = _timeout ?? EnvironmentConfigReader.GetTimeout(TimeSpan.FromSeconds(30));
            stateDirectory = _stateDirectory ?? EnvironmentConfigReader.GetStateDirectory();
            retryPolicy = _retryPolicy ?? EnvironmentConfigReader.GetRetryPolicy();
        }
        else
        {
            // Use explicit values only, with defaults for baseUrl and timeout
            publishableKey = _publishableKey;
            appSlug = _appSlug;
            baseUrl = _baseUrl ?? "https://replicated.app";
            timeout = _timeout ?? TimeSpan.FromSeconds(30);
            stateDirectory = _stateDirectory;
            retryPolicy = _retryPolicy;
        }

        // Validate that required parameters are present
        if (string.IsNullOrWhiteSpace(publishableKey))
        {
            throw new ArgumentException(
                "Publishable key is required. Call WithPublishableKey() or enable FromEnvironment() to read from REPLICATED_PUBLISHABLE_KEY environment variable.");
        }

        if (string.IsNullOrWhiteSpace(appSlug))
        {
            throw new ArgumentException(
                "App slug is required. Call WithAppSlug() or enable FromEnvironment() to read from REPLICATED_APP_SLUG environment variable.");
        }

        // Validate timeout if explicitly set
        if (_timeout.HasValue)
        {
            InputValidator.ValidateTimeout(_timeout.Value);
        }
        else if (_mergeWithEnvironment)
        {
            // Validate timeout from environment
            InputValidator.ValidateTimeout(timeout);
        }

        // Validate baseUrl if explicitly set
        if (_baseUrl != null)
        {
            InputValidator.ValidateBaseUrl(_baseUrl);
        }
        else if (_mergeWithEnvironment && baseUrl != "https://replicated.app")
        {
            // Validate baseUrl from environment if different from default
            InputValidator.ValidateBaseUrl(baseUrl);
        }

        // Validate retry policy if set
        if (retryPolicy != null)
        {
            retryPolicy.Validate();
        }

        return new ReplicatedClient(
            publishableKey: publishableKey,
            appSlug: appSlug,
            baseUrl: baseUrl,
            timeout: timeout,
            stateDirectory: stateDirectory,
            retryPolicy: retryPolicy);
    }
}
