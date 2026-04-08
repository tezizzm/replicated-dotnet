using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Replicated;
using Xunit;

namespace Replicated.IntegrationTests;

/// <summary>
/// Integration tests using the mock server for comprehensive HTTP error testing.
/// These tests exercise real HTTP client behavior with controlled responses via
/// the X-Test-Status header injection mechanism.
/// </summary>
public class MockServerIntegrationTests : IntegrationTestBase, IClassFixture<ServerFixture>
{
    public MockServerIntegrationTests(ServerFixture server) : base(server)
    {
    }

    [Fact]
    public async Task App_GetInfo_With401_ShouldThrowAuthError()
    {
        var client = CreateClient("401");

        var exception = await Assert.ThrowsAsync<ReplicatedAuthError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(401, exception.HttpStatus);
        Assert.Contains("Unauthorized", exception.Message);
    }

    [Fact]
    public async Task App_GetInfo_With429_ShouldThrowRateLimitError()
    {
        var client = CreateClient("429");

        var exception = await Assert.ThrowsAsync<ReplicatedRateLimitError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(429, exception.HttpStatus);
        Assert.Contains("Rate limit exceeded", exception.Message);
    }

    [Fact]
    public async Task App_GetInfo_With400_ShouldThrowApiError()
    {
        var client = CreateClient("400");

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(400, exception.HttpStatus);
    }

    [Fact]
    public async Task App_GetInfo_With500_ShouldThrowApiError()
    {
        var client = CreateClient("500");

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(500, exception.HttpStatus);
        Assert.Contains("Internal Server Error", exception.Message);
    }

    [Fact]
    public async Task App_GetInfo_With503_ShouldThrowApiError()
    {
        var client = CreateClient("503");

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(503, exception.HttpStatus);
        Assert.Contains("Service Unavailable", exception.Message);
    }

    [Fact]
    public async Task License_GetInfo_With401_ShouldThrowAuthError()
    {
        var client = CreateClient("401");

        var exception = await Assert.ThrowsAsync<ReplicatedAuthError>(async () =>
            await client.License.GetInfoAsync());

        Assert.Equal(401, exception.HttpStatus);
        Assert.Contains("Unauthorized", exception.Message);
    }

    [Fact]
    public async Task App_SendMetrics_With500_ShouldThrowApiError()
    {
        var client = CreateClient("500");

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.App.SendCustomMetricsAsync(new Dictionary<string, double>
            {
                ["test_metric"] = 1.0
            }));

        Assert.Equal(500, exception.HttpStatus);
    }

    [Fact]
    public async Task App_GetInfo_With404_ShouldThrowApiError()
    {
        var client = CreateClient("404");

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(404, exception.HttpStatus);
    }
}
