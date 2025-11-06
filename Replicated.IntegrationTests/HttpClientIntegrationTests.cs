using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Replicated;
using Xunit;

namespace Replicated.IntegrationTests;

public class HttpClientIntegrationTests : IntegrationTestBase, IClassFixture<ServerFixture>
{
    public HttpClientIntegrationTests(ServerFixture server) : base(server)
    {
    }

    [Fact]
    public void Unauthorized_ShouldThrowAuthError()
    {
        // Test 401 Unauthorized response
        var client = CreateClient("401");
        Assert.Throws<ReplicatedAuthError>(() => client.Customer.GetOrCreate("install@example.com"));
    }

    [Fact]
    public async Task RateLimit_With429_ShouldThrowRateLimitError()
    {
        // Test 429 Rate Limit response
        var client = CreateClient("429");
        await Assert.ThrowsAsync<ReplicatedRateLimitError>(async () =>
            await client.Customer.GetOrCreateAsync("install@example.com"));
    }

    [Fact]
    public void ClientError_4xx_ShouldThrowApiError()
    {
        // Test 400 Bad Request response
        var client = CreateClient("400");
        var instance = new Replicated.Resources.Instance(client, "cust_123");
        Assert.Throws<ReplicatedApiError>(() => instance.SetVersion("1.0.0"));
    }

    [Fact]
    public async Task ServerError_5xx_ShouldThrowApiError()
    {
        // Test 500 Internal Server Error response
        var client = CreateClient("500");
        var instance = new Replicated.Resources.Instance(client, "cust_123");
        await Assert.ThrowsAsync<ReplicatedApiError>(async () => await instance.SetStatusAsync("running"));
    }
}


