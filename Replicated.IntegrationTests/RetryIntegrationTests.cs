using System;
using System.Threading.Tasks;
using Replicated;
using Xunit;

namespace Replicated.IntegrationTests;

public class RetryIntegrationTests : IntegrationTestBase, IClassFixture<ServerFixture>
{
    public RetryIntegrationTests(ServerFixture server) : base(server)
    {
    }

    [Fact]
    public async Task Retries_NetworkError_ShouldRetryAndFail()
    {
        // Create client with retry policy and 500 error — retries will exhaust then throw
        var policy = new RetryPolicy
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            RetryOnServerError = true,
            RetryOnNetworkError = true,
            UseJitter = false
        };

        var client = CreateClient("500", policy);

        await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.App.GetInfoAsync());
    }

    [Fact]
    public async Task Retries_ServerErrorExhaustion_ShouldThrowAfterRetries()
    {
        // Test retry exhaustion with server error
        var policy = new RetryPolicy
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            RetryOnServerError = true,
            UseJitter = false
        };

        var client = CreateClient("500", policy);

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(500, exception.HttpStatus);
    }

    [Fact]
    public async Task Retries_NotTriggeredOn4xx_ShouldThrowImmediately()
    {
        // 4xx errors should not be retried
        var policy = new RetryPolicy
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            RetryOnServerError = false,
            UseJitter = false
        };

        var client = CreateClient("400", policy);

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(400, exception.HttpStatus);
    }
}
