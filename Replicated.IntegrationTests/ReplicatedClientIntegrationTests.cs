using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Replicated;

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
    [Trait("RequiresCredentials", "true")]
    public async Task CreateClient_WithValidCredentials_ShouldSucceed()
    {
        // Skip if credentials not available
        if (!HasCredentials())
        {
            return; // Skip test silently - credentials not available
        }

        // Arrange & Act
        using var client = CreateClient();

        // Assert
        Assert.NotNull(client);
        Assert.Equal("replicated_pk_test_key", client.PublishableKey);
        Assert.StartsWith("test_app_", client.AppSlug); // App slug is now unique per test
        Assert.Equal(Server.BaseUrl, client.BaseUrl);
        Assert.NotNull(client.MachineId);
        Assert.NotEmpty(client.MachineId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task GetOrCreateCustomer_WithValidEmail_ShouldReturnCustomer()
    {
        if (!HasCredentials())
        {
            return; // Skip test - credentials not available
        }

        // Arrange
        using var client = CreateClient();
        var email = $"integration-test-{Guid.NewGuid()}@example.com";

        // Act
        var customer = await client.Customer.GetOrCreateAsync(email, "Stable", "Integration Test Customer");

        // Assert
        Assert.NotNull(customer);
        Assert.Equal(email, customer.EmailAddress);
        Assert.Equal("Stable", customer.Channel);
        Assert.NotNull(customer.CustomerId);
        Assert.NotEmpty(customer.CustomerId);

        _output.WriteLine($"Created customer with ID: {customer.CustomerId}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task GetOrCreateCustomer_WithSameEmail_ShouldReturnSameCustomer()
    {
        if (!HasCredentials())
        {
            return; // Skip test - credentials not available
        }

        // Arrange
        using var client = CreateClient();
        var email = $"integration-test-{Guid.NewGuid()}@example.com";

        // Act
        var customer1 = await client.Customer.GetOrCreateAsync(email);
        var customer2 = await client.Customer.GetOrCreateAsync(email);

        // Assert
        Assert.NotNull(customer1);
        Assert.NotNull(customer2);
        Assert.Equal(customer1.CustomerId, customer2.CustomerId);
        Assert.Equal(customer1.EmailAddress, customer2.EmailAddress);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task GetOrCreateInstance_ShouldCreateInstance()
    {
        if (!HasCredentials())
        {
            return; // Skip test - credentials not available
        }

        // Arrange
        using var client = CreateClient();
        var email = $"integration-test-{Guid.NewGuid()}@example.com";
        var customer = await client.Customer.GetOrCreateAsync(email);

        // Act
        var instance = await customer.GetOrCreateInstanceAsync();

        // Assert
        Assert.NotNull(instance);
        Assert.Equal(customer.CustomerId, instance.CustomerId);
        
        // Instance ID is only set when an operation that requires it is performed
        // Trigger instance creation by sending a metric
        await instance.SendMetricAsync("test_metric", 123);
        
        Assert.NotNull(instance.InstanceId);
        Assert.NotEmpty(instance.InstanceId);

        _output.WriteLine($"Created instance with ID: {instance.InstanceId}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task SendMetric_ShouldSucceed()
    {
        if (!HasCredentials())
        {
            return; // Skip test - credentials not available
        }

        // Arrange
        using var client = CreateClient();
        var email = $"integration-test-{Guid.NewGuid()}@example.com";
        var customer = await client.Customer.GetOrCreateAsync(email);
        var instance = await customer.GetOrCreateInstanceAsync();

        // Act
        await instance.SendMetricAsync("integration_test_metric", 42.5);

        // Assert - If no exception is thrown, the operation succeeded
        Assert.True(true);
        
        _output.WriteLine("Successfully sent metric");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task SendMultipleMetrics_ShouldSucceed()
    {
        if (!HasCredentials())
        {
            return; // Skip test - credentials not available
        }

        // Arrange
        using var client = CreateClient();
        var email = $"integration-test-{Guid.NewGuid()}@example.com";
        var customer = await client.Customer.GetOrCreateAsync(email);
        var instance = await customer.GetOrCreateInstanceAsync();

        // Act
        await instance.SendMetricAsync("cpu_usage", 0.75);
        await instance.SendMetricAsync("memory_usage", 0.60);
        await instance.SendMetricAsync("active_users", 150);

        // Assert - If no exception is thrown, the operation succeeded
        Assert.True(true);
        
        _output.WriteLine("Successfully sent multiple metrics");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task SetStatus_ShouldSucceed()
    {
        if (!HasCredentials())
        {
            return; // Skip test - credentials not available
        }

        // Arrange
        using var client = CreateClient();
        var email = $"integration-test-{Guid.NewGuid()}@example.com";
        var customer = await client.Customer.GetOrCreateAsync(email);
        var instance = await customer.GetOrCreateInstanceAsync();

        // Act
        await instance.SetStatusAsync("running");

        // Assert - If no exception is thrown, the operation succeeded
        Assert.True(true);
        
        _output.WriteLine("Successfully set instance status");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task SetVersion_ShouldSucceed()
    {
        if (!HasCredentials())
        {
            return; // Skip test - credentials not available
        }

        // Arrange
        using var client = CreateClient();
        var email = $"integration-test-{Guid.NewGuid()}@example.com";
        var customer = await client.Customer.GetOrCreateAsync(email);
        var instance = await customer.GetOrCreateInstanceAsync();

        // Act
        await instance.SetVersionAsync("1.0.0-integration-test");

        // Assert - If no exception is thrown, the operation succeeded
        Assert.True(true);
        
        _output.WriteLine("Successfully set instance version");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task CompleteWorkflow_EndToEnd_ShouldSucceed()
    {
        if (!HasCredentials())
        {
            return; // Skip test - credentials not available
        }

        // Arrange
        using var client = CreateClient();
        var email = $"integration-test-{Guid.NewGuid()}@example.com";

        // Act - Complete workflow
        var customer = await client.Customer.GetOrCreateAsync(email, "Stable", "E2E Test Customer");
        var instance = await customer.GetOrCreateInstanceAsync();
        await instance.SetStatusAsync("running");
        await instance.SetVersionAsync("1.0.0");
        await instance.SendMetricAsync("cpu_usage", 0.75);
        await instance.SendMetricAsync("memory_usage", 0.60);
        await instance.SendMetricAsync("active_users", 150);

        // Assert
        Assert.NotNull(customer);
        Assert.NotNull(instance);
        Assert.Equal(email, customer.EmailAddress);
        Assert.NotNull(instance.InstanceId);

        _output.WriteLine($"E2E Test completed successfully for customer: {customer.CustomerId}, instance: {instance.InstanceId}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task RetryPolicy_OnTransientError_ShouldRetry()
    {
        if (!HasCredentials())
        {
            return; // Skip test - credentials not available
        }

        // Arrange
        var retryPolicy = new RetryPolicy
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            RetryOnNetworkError = true,
            RetryOnServerError = true
        };

        using var client = new ReplicatedClientBuilder()
            .WithPublishableKey("replicated_pk_test_key")
            .WithAppSlug("test_app")
            .WithBaseUrl(Server.BaseUrl!)
            .WithRetryPolicy(retryPolicy)
            .Build();

        var email = $"integration-test-{Guid.NewGuid()}@example.com";

        // Act - Should handle transient errors with retry
        var customer = await client.Customer.GetOrCreateAsync(email);

        // Assert
        Assert.NotNull(customer);
        _output.WriteLine($"Retry policy test completed. Customer ID: {customer.CustomerId}");
    }
}

/// <summary>
/// Test fixture for integration tests.
/// </summary>
public class IntegrationTestFixture : IDisposable
{
    public IntegrationTestFixture()
    {
        // One-time setup if needed
    }

    public void Dispose()
    {
        // One-time cleanup if needed
    }
}

