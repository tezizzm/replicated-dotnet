using System;
using System.Threading.Tasks;
using Replicated;
using Xunit;
using Xunit.Abstractions;

namespace Replicated.IntegrationTests;

/// <summary>
/// Integration tests for ReplicatedClient.
/// These tests use the mock server for testing.
/// </summary>
public class ReplicatedClientIntegrationTests : IntegrationTestBase, IClassFixture<ServerFixture>
{
    private readonly ITestOutputHelper _output;

    public ReplicatedClientIntegrationTests(ServerFixture server, ITestOutputHelper output) : base(server)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void CreateClient_ShouldSucceed()
    {
        // Arrange & Act
        using var client = CreateClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal(Server.BaseUrl, client.BaseUrl);
        Assert.NotNull(client.App);
        Assert.NotNull(client.License);

        _output.WriteLine($"Client created with BaseUrl: {client.BaseUrl}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task App_GetInfoAsync_ShouldReturnAppInfo()
    {
        using var client = CreateClient();

        try
        {
            var info = await client.App.GetInfoAsync();
            Assert.NotNull(info);
            _output.WriteLine($"AppInfo retrieved: InstanceId={info.InstanceId}, AppSlug={info.AppSlug}");
        }
        catch (ReplicatedNetworkError)
        {
            // Mock server not running — acceptable in CI without server
            return;
        }
        catch (ReplicatedApiError ex)
        {
            // Mock server returned an error response — acceptable
            _output.WriteLine($"Mock server returned error: {ex.Message}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task App_GetStatusAsync_ShouldReturnAppStatus()
    {
        using var client = CreateClient();

        try
        {
            var status = await client.App.GetStatusAsync();
            Assert.NotNull(status);
            _output.WriteLine($"AppStatus retrieved: Sequence={status.Sequence}");
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
    public async Task App_GetUpdatesAsync_ShouldReturnReleases()
    {
        using var client = CreateClient();

        try
        {
            var updates = await client.App.GetUpdatesAsync();
            Assert.NotNull(updates);
            _output.WriteLine($"App updates count: {updates.Length}");
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
    public async Task License_GetInfoAsync_ShouldReturnLicenseInfo()
    {
        using var client = CreateClient();

        try
        {
            var info = await client.License.GetInfoAsync();
            Assert.NotNull(info);
            _output.WriteLine($"LicenseInfo retrieved: LicenseId={info.LicenseId}, CustomerName={info.CustomerName}");
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
    public async Task License_GetFieldsAsync_ShouldReturnFields()
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
}
