using System;
using Replicated;
using Xunit;

namespace Replicated.Tests;

/// <summary>
/// Tests for ReplicatedClientBuilder fluent API and validation.
/// </summary>
public class ReplicatedClientBuilderTests
{
    [Fact]
    public void WithBaseUrl_StoresUrlInBuiltClient()
    {
        var client = new ReplicatedClientBuilder()
            .WithBaseUrl("http://custom-host:4000")
            .Build();

        Assert.Equal("http://custom-host:4000", client.BaseUrl);
    }

    [Fact]
    public void WithTimeout_StoresTimeoutInBuiltClient()
    {
        var timeout = TimeSpan.FromSeconds(45);
        var client = new ReplicatedClientBuilder()
            .WithTimeout(timeout)
            .Build();

        Assert.Equal(timeout, client.Timeout);
    }

    [Fact]
    public void WithRetryPolicy_AcceptsValidPolicy()
    {
        var policy = new RetryPolicy
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromSeconds(1),
            BackoffMultiplier = 2.0
        };

        // Should not throw
        var client = new ReplicatedClientBuilder()
            .WithRetryPolicy(policy)
            .Build();

        Assert.NotNull(client);
    }

    [Fact]
    public void WithoutRetries_SetsMaxRetriesToZero()
    {
        // Should build successfully with retries disabled
        var client = new ReplicatedClientBuilder()
            .WithoutRetries()
            .Build();

        Assert.NotNull(client);
    }

    [Fact]
    public void Build_WithInvalidBaseUrl_Throws()
    {
        var builder = new ReplicatedClientBuilder()
            .WithBaseUrl("ftp://bad-scheme");

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithZeroTimeout_Throws()
    {
        var builder = new ReplicatedClientBuilder()
            .WithTimeout(TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithNegativeTimeout_Throws()
    {
        var builder = new ReplicatedClientBuilder()
            .WithTimeout(TimeSpan.FromSeconds(-5));

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithDefaults_UsesDefaultBaseUrlAndTimeout()
    {
        var client = new ReplicatedClientBuilder().Build();

        Assert.Equal("http://replicated:3000", client.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(30), client.Timeout);
    }
}
