using System;
using Microsoft.Extensions.DependencyInjection;
using Replicated;
using Xunit;

namespace Replicated.Tests;

public class ServiceCollectionExtensionsTests
{
    // ── AddReplicatedClient (no args) ─────────────────────────────────────────

    [Fact]
    public void AddReplicatedClient_NoArgs_RegistersIReplicatedClient()
    {
        var services = new ServiceCollection();
        services.AddReplicatedClient();

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IReplicatedClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddReplicatedClient_NoArgs_IsSingleton()
    {
        var services = new ServiceCollection();
        services.AddReplicatedClient();

        var provider = services.BuildServiceProvider();
        var a = provider.GetRequiredService<IReplicatedClient>();
        var b = provider.GetRequiredService<IReplicatedClient>();

        Assert.Same(a, b);
    }

    // ── AddReplicatedClient (custom baseUrl) ──────────────────────────────────

    [Fact]
    public void AddReplicatedClient_WithCustomBaseUrl_UsesCustomUrl()
    {
        var services = new ServiceCollection();
        services.AddReplicatedClient(baseUrl: "http://test:3000");

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IReplicatedClient>();

        Assert.NotNull(client);
        Assert.Equal("http://test:3000", client.BaseUrl);
    }

    // ── AddReplicatedClient (null services) ───────────────────────────────────

    [Fact]
    public void AddReplicatedClient_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddReplicatedClient());
    }

    [Fact]
    public void AddReplicatedClient_NullServices_WithBaseUrl_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddReplicatedClient(baseUrl: "http://test:3000"));
    }

    // ── AddReplicatedClient (retry policy) ───────────────────────────────────

    [Fact]
    public void AddReplicatedClient_WithRetryPolicy_RegistersIReplicatedClient()
    {
        var services = new ServiceCollection();
        services.AddReplicatedClient(retryPolicy: new RetryPolicy { MaxRetries = 5 });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IReplicatedClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddReplicatedClient_WithDisabledRetries_RegistersIReplicatedClient()
    {
        var services = new ServiceCollection();
        services.AddReplicatedClient(retryPolicy: new RetryPolicy { MaxRetries = 0 });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IReplicatedClient>();

        Assert.NotNull(client);
    }

    // ── AddReplicatedClient (builder delegate) ────────────────────────────────

    [Fact]
    public void AddReplicatedClient_BuilderDelegate_RegistersIReplicatedClient()
    {
        var services = new ServiceCollection();
        services.AddReplicatedClient(b => b.WithBaseUrl("http://test:3000"));

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IReplicatedClient>();

        Assert.NotNull(client);
        Assert.Equal("http://test:3000", client.BaseUrl);
    }

    [Fact]
    public void AddReplicatedClient_BuilderDelegate_IsSingleton()
    {
        var services = new ServiceCollection();
        services.AddReplicatedClient(b => b.WithBaseUrl("http://test:3000"));

        var provider = services.BuildServiceProvider();
        var a = provider.GetRequiredService<IReplicatedClient>();
        var b = provider.GetRequiredService<IReplicatedClient>();

        Assert.Same(a, b);
    }

    [Fact]
    public void AddReplicatedClient_NullDelegate_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddReplicatedClient((Action<ReplicatedClientBuilder>)null!));
    }
}
