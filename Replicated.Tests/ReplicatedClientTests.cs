using System;
using System.Threading.Tasks;
using Replicated;
using Replicated.Services;
using Xunit;

namespace Replicated.Tests;

public class ReplicatedClientTests
{
    [Fact]
    public void Constructor_Default_CreatesClientWithDefaultBaseUrl()
    {
        var client = new ReplicatedClient();

        Assert.NotNull(client);
        Assert.Equal("http://replicated:3000", client.BaseUrl);
    }

    [Fact]
    public void Constructor_WithCustomBaseUrl_StoresBaseUrl()
    {
        var client = new ReplicatedClient(baseUrl: "http://custom-host:3000");

        Assert.Equal("http://custom-host:3000", client.BaseUrl);
    }

    [Theory]
    [InlineData("http://replicated:3000")]
    [InlineData("https://replicated.app")]
    [InlineData("http://localhost:9090")]
    public void Constructor_WithValidBaseUrls_ShouldNotThrow(string baseUrl)
    {
        var client = new ReplicatedClient(baseUrl: baseUrl);
        Assert.Equal(baseUrl, client.BaseUrl);
    }

    [Fact]
    public void Constructor_WithInvalidBaseUrl_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ReplicatedClient(baseUrl: "ftp://bad-scheme"));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("   ")]
    public void Constructor_WithInvalidBaseUrls_ThrowsArgumentException(string url)
    {
        Assert.Throws<ArgumentException>(() => new ReplicatedClient(baseUrl: url));
    }

    [Fact]
    public void Constructor_WithZeroTimeout_UsesDefault()
    {
        // TimeSpan.Zero == default, so the constructor treats it as "not specified" and uses 30s
        var client = new ReplicatedClient(timeout: TimeSpan.Zero);
        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
    }

    [Fact]
    public void Constructor_WithNegativeTimeout_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ReplicatedClient(timeout: TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void Constructor_WithTimeoutExceedingOneHour_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ReplicatedClient(timeout: TimeSpan.FromHours(2)));
    }

    [Fact]
    public void App_Property_IsNotNull()
    {
        var client = new ReplicatedClient();

        Assert.NotNull(client.App);
    }

    [Fact]
    public void App_Property_IsAppService()
    {
        var client = new ReplicatedClient();

        Assert.IsType<AppService>(client.App);
    }

    [Fact]
    public void License_Property_IsNotNull()
    {
        var client = new ReplicatedClient();

        Assert.NotNull(client.License);
    }

    [Fact]
    public void License_Property_IsLicenseService()
    {
        var client = new ReplicatedClient();

        Assert.IsType<LicenseService>(client.License);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var client = new ReplicatedClient();

        client.Dispose(); // Should not throw
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        var client = new ReplicatedClient();

        await client.DisposeAsync(); // Should not throw
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
    public void Timeout_ReflectsConstructorValue()
    {
        var timeout = TimeSpan.FromSeconds(45);
        var client = new ReplicatedClient(timeout: timeout);

        Assert.Equal(timeout, client.Timeout);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(3600)]
    public void Timeout_ShouldMatchConstructorValue(int seconds)
    {
        var timeout = TimeSpan.FromSeconds(seconds);
        var client = new ReplicatedClient(timeout: timeout);

        Assert.Equal(timeout, client.Timeout);
    }

    [Fact]
    public void ReplicatedClientBuilder_BuildsClientWithDefaults()
    {
        var client = new ReplicatedClientBuilder().Build();

        Assert.NotNull(client);
        Assert.Equal("http://replicated:3000", client.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
    }

    [Fact]
    public void ReplicatedClientBuilder_WithBaseUrl_SetsBaseUrl()
    {
        var client = new ReplicatedClientBuilder()
            .WithBaseUrl("http://test:3000")
            .Build();

        Assert.Equal("http://test:3000", client.BaseUrl);
    }
}
