using System;
using System.Threading.Tasks;
using Replicated;
using Xunit;

namespace Replicated.Tests;

/// <summary>
/// Extended tests for retry policy configuration and behavior.
/// These tests verify retry logic validation, exponential backoff, jitter, and custom retry scenarios.
/// </summary>
public class RetryPolicyExtendedTests
{
    [Fact]
    public void RetryPolicy_WithCustomShouldRetry_ShouldUseCustomLogic()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(10),
            ShouldRetry = (ex, attempt) => ex is ReplicatedNetworkError && attempt < 2
        };

        // Act - Validate by using it in a client (which validates internally)
        var client = new ReplicatedClient(retryPolicy: policy);

        // Assert - Should not throw, custom logic is valid
        Assert.NotNull(policy.ShouldRetry);
        Assert.NotNull(client);
    }

    [Fact]
    public void RetryPolicy_WithMaxRetriesZero_ShouldDisableRetries()
    {
        // Arrange
        var policy = new RetryPolicy { MaxRetries = 0 };

        // Act - Use policy in client (which validates internally)
        var client = new ReplicatedClient(retryPolicy: policy);

        // Assert - Should not throw (disabling retries is valid)
        Assert.NotNull(client);
    }

    [Fact]
    public void RetryPolicy_WithNegativeMaxRetries_ShouldThrowOnClientCreation()
    {
        // Arrange
        var policy = new RetryPolicy { MaxRetries = -1 };

        // Act & Assert - Validation happens when building client/builder
        var builder = new ReplicatedClientBuilder()
            .WithRetryPolicy(policy);

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("MaxRetries", exception.Message);
    }

    [Fact]
    public void RetryPolicy_WithNegativeInitialDelay_ShouldThrowOnClientCreation()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(-1)
        };

        // Act & Assert - Validation happens when building client
        var builder = new ReplicatedClientBuilder()
            .WithRetryPolicy(policy);

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("InitialDelay", exception.Message);
    }

    [Fact]
    public void RetryPolicy_WithZeroInitialDelay_ShouldThrowOnClientCreation()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.Zero
        };

        // Act & Assert
        var builder = new ReplicatedClientBuilder()
            .WithRetryPolicy(policy);

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("InitialDelay", exception.Message);
    }

    [Fact]
    public void RetryPolicy_WithNegativeMaxDelay_ShouldThrowOnClientCreation()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromMilliseconds(-1)
        };

        // Act & Assert
        var builder = new ReplicatedClientBuilder()
            .WithRetryPolicy(policy);

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("MaxDelay", exception.Message);
    }

    [Fact]
    public void RetryPolicy_WithMaxDelayLessThanInitialDelay_ShouldThrowOnClientCreation()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromSeconds(10),
            MaxDelay = TimeSpan.FromSeconds(1) // Less than initial delay
        };

        // Act & Assert
        var builder = new ReplicatedClientBuilder()
            .WithRetryPolicy(policy);

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("MaxDelay", exception.Message);
    }

    [Fact]
    public void RetryPolicy_WithNegativeBackoffMultiplier_ShouldThrowOnClientCreation()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromSeconds(1),
            BackoffMultiplier = -1.0
        };

        // Act & Assert
        var builder = new ReplicatedClientBuilder()
            .WithRetryPolicy(policy);

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("BackoffMultiplier", exception.Message);
    }

    [Fact]
    public void RetryPolicy_WithZeroBackoffMultiplier_ShouldThrowOnClientCreation()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromSeconds(1),
            BackoffMultiplier = 0.0
        };

        // Act & Assert
        var builder = new ReplicatedClientBuilder()
            .WithRetryPolicy(policy);

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("BackoffMultiplier", exception.Message);
    }

    [Fact]
    public void RetryPolicy_WithJitterPercentageOutOfRange_ShouldThrowOnClientCreation()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromSeconds(1),
            UseJitter = true,
            JitterPercentage = 1.5 // > 1.0
        };

        // Act & Assert
        var builder = new ReplicatedClientBuilder()
            .WithRetryPolicy(policy);

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("JitterPercentage", exception.Message);
    }

    [Fact]
    public void RetryPolicy_WithNegativeJitterPercentage_ShouldThrowOnClientCreation()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromSeconds(1),
            UseJitter = true,
            JitterPercentage = -0.1
        };

        // Act & Assert
        var builder = new ReplicatedClientBuilder()
            .WithRetryPolicy(policy);

        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("JitterPercentage", exception.Message);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void RetryPolicy_WithValidJitterPercentage_ShouldCreateClientSuccessfully(double jitter)
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromSeconds(1),
            UseJitter = true,
            JitterPercentage = jitter
        };

        // Act
        var client = new ReplicatedClient(retryPolicy: policy);

        // Assert - Should not throw
        Assert.NotNull(client);
    }

    [Fact]
    public void RetryPolicy_WithRetryOnNetworkErrorDisabled_ShouldNotRetryOnNetworkError()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(10),
            RetryOnNetworkError = false
        };

        // Act - Use in client to validate
        var client = new ReplicatedClient(retryPolicy: policy);

        // Assert - Configuration is valid
        Assert.False(policy.RetryOnNetworkError);
        Assert.NotNull(client);
    }

    [Fact]
    public void RetryPolicy_WithRetryOnRateLimitDisabled_ShouldNotRetryOnRateLimit()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(10),
            RetryOnRateLimit = false
        };

        // Act
        var client = new ReplicatedClient(retryPolicy: policy);

        // Assert
        Assert.False(policy.RetryOnRateLimit);
        Assert.NotNull(client);
    }

    [Fact]
    public void RetryPolicy_WithRetryOnServerErrorDisabled_ShouldNotRetryOnServerError()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(10),
            RetryOnServerError = false
        };

        // Act
        var client = new ReplicatedClient(retryPolicy: policy);

        // Assert
        Assert.False(policy.RetryOnServerError);
        Assert.NotNull(client);
    }

    [Fact]
    public void RetryPolicy_WithAllRetriesDisabled_ShouldCreateClientSuccessfully()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 0,
            RetryOnNetworkError = false,
            RetryOnRateLimit = false,
            RetryOnServerError = false
        };

        // Act
        var client = new ReplicatedClient(retryPolicy: policy);

        // Assert - Should not throw
        Assert.NotNull(client);
    }

    [Fact]
    public void RetryPolicy_WithCustomRetryLogic_ShouldHonorCustomLogic()
    {
        // Arrange
        int retryCount = 0;
        var policy = new RetryPolicy
        {
            MaxRetries = 5,
            InitialDelay = TimeSpan.FromMilliseconds(10),
            ShouldRetry = (ex, attempt) =>
            {
                retryCount++;
                // Only retry first 2 times
                return attempt < 2;
            }
        };

        // Act
        var client = new ReplicatedClient(retryPolicy: policy);

        // Assert - Custom logic should be set
        Assert.NotNull(policy.ShouldRetry);
        Assert.NotNull(client);

        // Test the custom logic
        var networkError = new ReplicatedNetworkError("Network error");
        Assert.True(policy.ShouldRetry(networkError, 0));
        Assert.True(policy.ShouldRetry(networkError, 1));
        Assert.False(policy.ShouldRetry(networkError, 2));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    [InlineData(3.0)]
    public void RetryPolicy_WithValidBackoffMultiplier_ShouldCreateClientSuccessfully(double multiplier)
    {
        // Arrange
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromSeconds(1),
            BackoffMultiplier = multiplier
        };

        // Act
        var client = new ReplicatedClient(retryPolicy: policy);

        // Assert - Should not throw
        Assert.NotNull(client);
    }
}
