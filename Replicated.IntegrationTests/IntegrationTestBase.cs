using System;
using System.Net.Http;
using System.Threading.Tasks;
using Replicated;

namespace Replicated.IntegrationTests;

/// <summary>
/// Base class for integration tests that provides common client creation functionality.
/// </summary>
public abstract class IntegrationTestBase
{
    protected readonly ServerFixture Server;

    protected IntegrationTestBase(ServerFixture server)
    {
        Server = server;
    }

    /// <summary>
    /// Creates a ReplicatedClient with optional status code injection for testing error scenarios.
    /// The X-Test-Status header is injected via the underlying System.Net.Http.HttpClient's
    /// DefaultRequestHeaders so it is sent with every request.
    /// </summary>
    /// <param name="statusCode">Optional status code to inject as X-Test-Status header for error testing.</param>
    /// <param name="retryPolicy">Optional retry policy for the client.</param>
    /// <returns>A configured ReplicatedClient instance.</returns>
    protected ReplicatedClient CreateClient(string? statusCode = null, RetryPolicy? retryPolicy = null)
    {
        var builder = new ReplicatedClientBuilder().WithBaseUrl(Server.BaseUrl!);

        if (retryPolicy != null)
        {
            builder.WithRetryPolicy(retryPolicy);
        }

        var client = builder.Build();

        if (!string.IsNullOrEmpty(statusCode))
        {
            InjectTestStatusHeader(client, statusCode);
        }

        return client;
    }

    /// <summary>
    /// Injects the X-Test-Status header into the underlying HttpClient's DefaultRequestHeaders
    /// by walking the reflection chain:
    ///   ReplicatedClient._httpClient (ReplicatedHttpClientAsync)
    ///     -> _core (CoreHttpClient)
    ///       -> HttpClientInstance (System.Net.Http.HttpClient)
    ///         -> DefaultRequestHeaders
    /// </summary>
    private static void InjectTestStatusHeader(ReplicatedClient client, string statusCode)
    {
        // ReplicatedClient._httpClient -> ReplicatedHttpClientAsync
        var asyncClientField = typeof(ReplicatedClient).GetField("_httpClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var asyncClient = asyncClientField?.GetValue(client);
        if (asyncClient == null)
            throw new InvalidOperationException("Could not retrieve _httpClient from ReplicatedClient.");

        // ReplicatedHttpClientAsync._core -> CoreHttpClient
        var coreField = asyncClient.GetType().GetField("_core",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var core = coreField?.GetValue(asyncClient);
        if (core == null)
            throw new InvalidOperationException("Could not retrieve _core from ReplicatedHttpClientAsync.");

        // CoreHttpClient.HttpClientInstance -> System.Net.Http.HttpClient
        var httpClientProp = core.GetType().GetProperty("HttpClientInstance",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var httpClient = httpClientProp?.GetValue(core) as HttpClient;
        if (httpClient == null)
            throw new InvalidOperationException("Could not retrieve HttpClientInstance from CoreHttpClient.");

        // Add the test header to DefaultRequestHeaders so it's sent with every request
        httpClient.DefaultRequestHeaders.Remove("X-Test-Status");
        httpClient.DefaultRequestHeaders.Add("X-Test-Status", statusCode);
    }

    /// <summary>
    /// Checks if the test has valid credentials for real API testing.
    /// </summary>
    /// <returns>True if credentials are available, false otherwise.</returns>
    protected bool HasCredentials()
    {
        // For integration tests with mock server, we always have "credentials"
        return true;
    }
}
