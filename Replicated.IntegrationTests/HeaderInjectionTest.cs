using System;
using System.Collections.Generic;
using System.Reflection;
using Replicated;
using Xunit;

namespace Replicated.IntegrationTests;

/// <summary>
/// Test to verify header injection is working correctly.
/// </summary>
public class HeaderInjectionTest : IntegrationTestBase, IClassFixture<ServerFixture>
{
    public HeaderInjectionTest(ServerFixture server) : base(server)
    {
    }

    [Fact]
    public void HeaderInjection_ShouldWork()
    {
        // Create client with status code injection
        var client = CreateClient("401");
        
        // Get the HTTP client via reflection
        var httpClientField = typeof(ReplicatedClient).GetField("_httpClient", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var httpClient = httpClientField?.GetValue(client) as ReplicatedHttpClientAsync;
        
        Assert.NotNull(httpClient);
        
        // Get DefaultHeaders property
        var defaultHeadersProp = typeof(ReplicatedHttpClientBase).GetProperty("DefaultHeaders",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var defaultHeaders = defaultHeadersProp?.GetValue(httpClient) as Dictionary<string, string>;
        
        Assert.NotNull(defaultHeaders);
        Assert.True(defaultHeaders.ContainsKey("X-Test-Status"));
        Assert.Equal("401", defaultHeaders["X-Test-Status"]);
        
        // Now call BuildHeaders via reflection to see if it includes the header
        // Test with null headers (like some requests)
        var buildHeadersMethod = typeof(ReplicatedHttpClientBase).GetMethod("BuildHeaders",
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        Assert.NotNull(buildHeadersMethod);
        
        var requestHeaders1 = buildHeadersMethod.Invoke(httpClient, new object[] { (Dictionary<string, string>?)null }) as Dictionary<string, string>;
        
        Assert.NotNull(requestHeaders1);
        Assert.True(requestHeaders1.ContainsKey("X-Test-Status"), 
            $"X-Test-Status not in requestHeaders (null case). Headers present: {string.Join(", ", requestHeaders1.Keys)}");
        Assert.Equal("401", requestHeaders1["X-Test-Status"]);
        
        // Test with provided headers (like GetAuthHeaders does)
        var providedHeaders = new Dictionary<string, string> { ["Authorization"] = "Bearer test_key" };
        var requestHeaders2 = buildHeadersMethod.Invoke(httpClient, new object[] { providedHeaders }) as Dictionary<string, string>;
        
        Assert.NotNull(requestHeaders2);
        Assert.True(requestHeaders2.ContainsKey("X-Test-Status"), 
            $"X-Test-Status not in requestHeaders (with provided headers). Headers present: {string.Join(", ", requestHeaders2.Keys)}");
        Assert.Equal("401", requestHeaders2["X-Test-Status"]);
        Assert.True(requestHeaders2.ContainsKey("Authorization"));
        Assert.Equal("Bearer test_key", requestHeaders2["Authorization"]);
    }
    
    [Fact]
    public void HeaderInjection_ShouldBeSentInActualRequest()
    {
        // Create client with status code injection
        var client = CreateClient("401");
        
        // Make an actual request - this should throw ReplicatedAuthError if header is sent
        // If header is NOT sent, it will return 200 (success) and no exception will be thrown
        var exception = Assert.Throws<ReplicatedAuthError>(() => 
            client.Customer.GetOrCreate("test@example.com"));
        
        Assert.Equal(401, exception.HttpStatus);
        Assert.Contains("Unauthorized", exception.Message);
    }
}

