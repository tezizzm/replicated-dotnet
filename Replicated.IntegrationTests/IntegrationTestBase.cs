using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
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
    /// </summary>
    /// <param name="statusCode">Optional status code to inject as X-Test-Status header for error testing.</param>
    /// <param name="retryPolicy">Optional retry policy for the client.</param>
    /// <returns>A configured ReplicatedClient instance.</returns>
    protected ReplicatedClient CreateClient(string? statusCode = null, RetryPolicy? retryPolicy = null)
    {
        var baseUrl = Server.BaseUrl!;
        
        // Use unique app slug per test to ensure state isolation
        // This prevents tests from interfering with each other's cached state
        var uniqueAppSlug = $"test_app_{Guid.NewGuid():N}";
        var uniqueStateDir = Path.Combine(Path.GetTempPath(), $"replicated_test_{Guid.NewGuid():N}");
        
        // Create client with builder
        var builder = new ReplicatedClientBuilder()
            .WithPublishableKey("replicated_pk_test_key")
            .WithAppSlug(uniqueAppSlug)
            .WithStateDirectory(uniqueStateDir)
            .WithBaseUrl(baseUrl);
        
        if (retryPolicy != null)
        {
            builder.WithRetryPolicy(retryPolicy);
        }
        
        var client = builder.Build();
        
        // If status code is specified, inject it as a custom header via reflection
        if (!string.IsNullOrEmpty(statusCode))
        {
            // Get the internal HTTP client and inject custom headers
            var httpClientField = typeof(ReplicatedClient).GetField("_httpClient", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (httpClientField?.GetValue(client) is ReplicatedHttpClientAsync httpClient)
            {
                // Get the DefaultHeaders property from ReplicatedHttpClientBase
                var defaultHeadersProp = typeof(ReplicatedHttpClientBase).GetProperty("DefaultHeaders",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (defaultHeadersProp != null && defaultHeadersProp.CanWrite)
                {
                    // Always create a new dictionary to ensure we're setting the property correctly
                    var newHeaders = new Dictionary<string, string>();
                    
                    // Copy existing headers if any
                    var existingHeaders = defaultHeadersProp.GetValue(httpClient) as Dictionary<string, string>;
                    if (existingHeaders != null)
                    {
                        foreach (var kvp in existingHeaders)
                        {
                            newHeaders[kvp.Key] = kvp.Value;
                        }
                    }
                    
                    // Add the test status header
                    newHeaders["X-Test-Status"] = statusCode;
                    
                    // Use the setter method directly for more reliable setting
                    var setter = defaultHeadersProp.GetSetMethod(nonPublic: true);
                    if (setter != null)
                    {
                        setter.Invoke(httpClient, new object[] { newHeaders });
                    }
                    else
                    {
                        // Fallback to SetValue
                        defaultHeadersProp.SetValue(httpClient, newHeaders, null);
                    }
                    
                    // Verify the header was set correctly immediately after setting
                    var verify = defaultHeadersProp.GetValue(httpClient) as Dictionary<string, string>;
                    if (verify == null)
                    {
                        throw new InvalidOperationException(
                            $"Failed to inject X-Test-Status header: DefaultHeaders is null after setting");
                    }
                    if (!verify.ContainsKey("X-Test-Status"))
                    {
                        throw new InvalidOperationException(
                            $"Failed to inject X-Test-Status header: Key not found. Headers present: {string.Join(", ", verify.Keys)}");
                    }
                    if (verify["X-Test-Status"] != statusCode)
                    {
                        throw new InvalidOperationException(
                            $"Failed to inject X-Test-Status header: Expected '{statusCode}', got '{verify["X-Test-Status"]}'");
                    }
                    
                    // Verify we're working with the same instance that will be used for requests
                    // by checking the httpClient field in ReplicatedClient
                    var httpClientFieldCheck = typeof(ReplicatedClient).GetField("_httpClient", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var httpClientFromClient = httpClientFieldCheck?.GetValue(client) as ReplicatedHttpClientAsync;
                    if (httpClientFromClient != httpClient)
                    {
                        throw new InvalidOperationException(
                            $"HTTP client instance mismatch! Reflection got different instance than client field.");
                    }
                    
                    // Final verification: read DefaultHeaders from the client's httpClient
                    var finalVerify = defaultHeadersProp.GetValue(httpClientFromClient) as Dictionary<string, string>;
                    if (finalVerify == null || !finalVerify.ContainsKey("X-Test-Status") || finalVerify["X-Test-Status"] != statusCode)
                    {
                        throw new InvalidOperationException(
                            $"X-Test-Status header lost after verification! Expected '{statusCode}', " +
                            $"got: {(finalVerify?.TryGetValue("X-Test-Status", out var v) == true ? v : "null")}");
                    }
                }
            }
        }
        
        return client;
    }

    /// <summary>
    /// Creates a ReplicatedClient with a specific app slug and state directory.
    /// Used for tests that need shared state across multiple clients (e.g., state persistence tests).
    /// </summary>
    /// <param name="appSlug">The app slug to use.</param>
    /// <param name="stateDirectory">The state directory to use.</param>
    /// <param name="statusCode">Optional status code to inject as X-Test-Status header for error testing.</param>
    /// <param name="retryPolicy">Optional retry policy for the client.</param>
    /// <returns>A configured ReplicatedClient instance.</returns>
    protected ReplicatedClient CreateClientWithSharedState(
        string appSlug,
        string stateDirectory,
        string? statusCode = null,
        RetryPolicy? retryPolicy = null)
    {
        var baseUrl = Server.BaseUrl!;
        
        // Create client with builder using provided app slug and state directory
        var builder = new ReplicatedClientBuilder()
            .WithPublishableKey("replicated_pk_test_key")
            .WithAppSlug(appSlug)
            .WithStateDirectory(stateDirectory)
            .WithBaseUrl(baseUrl);
        
        if (retryPolicy != null)
        {
            builder.WithRetryPolicy(retryPolicy);
        }
        
        var client = builder.Build();
        
        // Inject status code header if specified (same logic as CreateClient)
        if (!string.IsNullOrEmpty(statusCode))
        {
            var httpClientField = typeof(ReplicatedClient).GetField("_httpClient", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (httpClientField?.GetValue(client) is ReplicatedHttpClientAsync httpClient)
            {
                var defaultHeadersProp = typeof(ReplicatedHttpClientBase).GetProperty("DefaultHeaders",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (defaultHeadersProp != null && defaultHeadersProp.CanWrite)
                {
                    var newHeaders = new Dictionary<string, string>();
                    var existingHeaders = defaultHeadersProp.GetValue(httpClient) as Dictionary<string, string>;
                    if (existingHeaders != null)
                    {
                        foreach (var kvp in existingHeaders)
                        {
                            newHeaders[kvp.Key] = kvp.Value;
                        }
                    }
                    newHeaders["X-Test-Status"] = statusCode;
                    
                    var setter = defaultHeadersProp.GetSetMethod(nonPublic: true);
                    if (setter != null)
                    {
                        setter.Invoke(httpClient, new object[] { newHeaders });
                    }
                    else
                    {
                        defaultHeadersProp.SetValue(httpClient, newHeaders, null);
                    }
                }
            }
        }
        
        return client;
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