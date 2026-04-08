using System;
using System.Threading.Tasks;
using Replicated;
using Replicated.Services;
using Xunit;

namespace Replicated.Tests;

public class ReplicatedClientExtendedTests
{
    [Fact]
    public void BaseUrl_DefaultsToInClusterAddress()
    {
        var client = new ReplicatedClient();

        Assert.Equal("http://replicated:3000", client.BaseUrl);
    }

    [Fact]
    public void BaseUrl_CustomValueFromConstructorIsUsed()
    {
        var client = new ReplicatedClient(baseUrl: "http://my-service:5000");

        Assert.Equal("http://my-service:5000", client.BaseUrl);
    }

    [Fact]
    public void Timeout_DefaultsToThirtySeconds()
    {
        var client = new ReplicatedClient();

        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
    }

    [Fact]
    public void Timeout_CustomValueIsStoredCorrectly()
    {
        var timeout = TimeSpan.FromSeconds(60);
        var client = new ReplicatedClient(timeout: timeout);

        Assert.Equal(timeout, client.Timeout);
    }

    [Fact]
    public void App_PropertyReturnsAppService()
    {
        var client = new ReplicatedClient();

        Assert.NotNull(client.App);
        Assert.IsType<AppService>(client.App);
    }

    [Fact]
    public void License_PropertyReturnsLicenseService()
    {
        var client = new ReplicatedClient();

        Assert.NotNull(client.License);
        Assert.IsType<LicenseService>(client.License);
    }

    [Fact]
    public async Task Dispose_ThenDisposeAsync_ShouldNotThrow()
    {
        var client = new ReplicatedClient();

        client.Dispose();
        await client.DisposeAsync(); // Should not throw
    }

    [Fact]
    public async Task DisposeAsync_ThenDispose_ShouldNotThrow()
    {
        var client = new ReplicatedClient();

        await client.DisposeAsync();
        client.Dispose(); // Should not throw
    }

    [Fact]
    public void Dispose_MultipleTimes_ShouldNotThrow()
    {
        var client = new ReplicatedClient();

        client.Dispose();
        client.Dispose();
        client.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_MultipleTimes_ShouldNotThrow()
    {
        var client = new ReplicatedClient();

        await client.DisposeAsync();
        await client.DisposeAsync();
        await client.DisposeAsync();
    }
}
