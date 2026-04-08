using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Replicated;

/// <summary>
/// Extension methods for registering <see cref="ReplicatedClient"/> with the
/// Microsoft dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IReplicatedClient"/> as a singleton using the default in-cluster configuration.
    /// Connects to the Replicated service at <c>http://replicated:3000</c> (or the
    /// <c>REPLICATED_SDK_ENDPOINT</c> environment variable when set).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">Optional base URL override. Defaults to <c>http://replicated:3000</c>.</param>
    /// <param name="timeout">Optional request timeout. Defaults to 30 seconds.</param>
    /// <param name="retryPolicy">Optional retry policy. Defaults to 3 retries with exponential backoff.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddReplicatedClient();
    ///
    /// // With custom retry policy:
    /// builder.Services.AddReplicatedClient(retryPolicy: new RetryPolicy { MaxRetries = 5 });
    /// </code>
    /// </example>
    public static IServiceCollection AddReplicatedClient(
        this IServiceCollection services,
        string? baseUrl = null,
        TimeSpan timeout = default,
        RetryPolicy? retryPolicy = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddSingleton<IReplicatedClient>(sp =>
            new ReplicatedClient(
                baseUrl: baseUrl,
                timeout: timeout == default ? TimeSpan.FromSeconds(30) : timeout,
                retryPolicy: retryPolicy,
                logger: sp.GetService<ILogger<ReplicatedClient>>()));

        return services;
    }

    /// <summary>
    /// Registers <see cref="IReplicatedClient"/> as a singleton using a
    /// <see cref="ReplicatedClientBuilder"/> factory delegate.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">A delegate that configures the builder.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddReplicatedClient(b => b
    ///     .FromEnvironment()
    ///     .WithTimeout(TimeSpan.FromSeconds(60)));
    /// </code>
    /// </example>
    public static IServiceCollection AddReplicatedClient(
        this IServiceCollection services,
        Action<ReplicatedClientBuilder> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        services.AddSingleton<IReplicatedClient>(sp =>
        {
            var builder = new ReplicatedClientBuilder();
            var logger = sp.GetService<ILogger<ReplicatedClient>>();
            if (logger != null) builder.WithLogger(logger);
            configure(builder);
            return builder.Build();
        });

        return services;
    }
}
