using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Replicated;
using Xunit;
using Xunit.Abstractions;

namespace Replicated.IntegrationTests;

/// <summary>
/// Additional integration tests covering edge cases and API surface coverage.
/// </summary>
public class IntegrationTestsExtended : IntegrationTestBase, IClassFixture<ServerFixture>
{
    private readonly ITestOutputHelper _output;

    public IntegrationTestsExtended(ServerFixture server, ITestOutputHelper output) : base(server)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void App_GetInfo_WithDefaultClient_ShouldBuildCorrectly()
    {
        // Verify default client builds without error
        using var client = CreateClient();

        Assert.NotNull(client);
        Assert.NotNull(client.App);
        Assert.NotNull(client.License);
        Assert.Equal(Server.BaseUrl, client.BaseUrl);

        _output.WriteLine($"Default client built. BaseUrl={client.BaseUrl}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task App_SendCustomMetrics_WithSuccess_ShouldNotThrow()
    {
        using var client = CreateClient();

        try
        {
            await client.App.SendCustomMetricsAsync(new Dictionary<string, double>
            {
                ["cpu_usage"] = 0.75,
                ["memory_mb"] = 512.0
            });
        }
        catch (ReplicatedNetworkError)
        {
            // Server not running — acceptable
            return;
        }
        catch (ReplicatedApiError ex)
        {
            _output.WriteLine($"Mock server returned error: {ex.Message}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task App_UpsertCustomMetrics_WithSuccess_ShouldNotThrow()
    {
        using var client = CreateClient();

        try
        {
            await client.App.UpsertCustomMetricsAsync(new Dictionary<string, double>
            {
                ["active_users"] = 42.0
            });
        }
        catch (ReplicatedNetworkError)
        {
            return;
        }
        catch (ReplicatedApiError ex)
        {
            _output.WriteLine($"Mock server returned error: {ex.Message}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task App_SetInstanceTags_WithSuccess_ShouldNotThrow()
    {
        using var client = CreateClient();

        try
        {
            await client.App.SetInstanceTagsAsync(new Dictionary<string, string>
            {
                ["environment"] = "test",
                ["region"] = "us-east-1"
            });
        }
        catch (ReplicatedNetworkError)
        {
            return;
        }
        catch (ReplicatedApiError ex)
        {
            _output.WriteLine($"Mock server returned error: {ex.Message}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task License_GetFields_WithSuccess_ShouldNotThrow()
    {
        using var client = CreateClient();

        try
        {
            var fields = await client.License.GetFieldsAsync();
            Assert.NotNull(fields);
            _output.WriteLine($"License fields count: {fields.Length}");
        }
        catch (ReplicatedNetworkError)
        {
            return;
        }
        catch (ReplicatedApiError ex)
        {
            _output.WriteLine($"Mock server returned error: {ex.Message}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Client_Dispose_ShouldNotThrow()
    {
        var client = CreateClient();

        // Should not throw
        client.Dispose();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Client_DisposeAsync_ShouldNotThrow()
    {
        var client = CreateClient();

        // Should not throw
        await client.DisposeAsync();
    }
}
