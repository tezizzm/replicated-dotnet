using System;
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
    public async Task Unauthorized_ShouldThrowAuthError()
    {
        // Test 401 Unauthorized response
        var client = CreateClient("401");

        var exception = await Assert.ThrowsAsync<ReplicatedAuthError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(401, exception.HttpStatus);
    }

    [Fact]
    public async Task RateLimit_ShouldThrowRateLimitError()
    {
        // Test 429 Rate Limit response
        var client = CreateClient("429");

        var exception = await Assert.ThrowsAsync<ReplicatedRateLimitError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(429, exception.HttpStatus);
    }

    [Fact]
    public async Task ServerError_ShouldThrowApiError()
    {
        // Test 500 Internal Server Error response
        var client = CreateClient("500");

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(500, exception.HttpStatus);
    }

    [Fact]
    public async Task ClientError_ShouldThrowApiError()
    {
        // Test 400 Bad Request response
        var client = CreateClient("400");

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(400, exception.HttpStatus);
    }
}
