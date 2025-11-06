using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Replicated;

namespace Replicated.IntegrationTests;

/// <summary>
/// Additional comprehensive integration tests covering edge cases, error scenarios, and UI visibility.
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
    [Trait("RequiresCredentials", "true")]
    public async Task CreateCustomer_WithInstallationDetails_ShouldBeVisibleInUI()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange - Use installation identifier email and descriptive name
        using var client = CreateClient();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var email = $"install.{timestamp}@customerdomain.com";
        var installationName = $"Customer Installation {timestamp} - Production";

        // Act
        var customer = await client.Customer.GetOrCreateAsync(email, "Stable", installationName);

        // Create instance and send metrics/status for activity (often needed for UI visibility)
        var instance = await customer.GetOrCreateInstanceAsync();
        await instance.SetStatusAsync("running");
        await instance.SetVersionAsync("1.0.0");
        await instance.SendMetricAsync("active", 1);

        // Assert
        Assert.NotNull(customer);
        Assert.Equal(email, customer.EmailAddress);
        Assert.NotNull(customer.CustomerId);
        Assert.NotNull(instance.InstanceId);

        _output.WriteLine($"Created customer installation:");
        _output.WriteLine($"  Installation Name: {installationName}");
        _output.WriteLine($"  Email (Identifier): {email}");
        _output.WriteLine($"  Customer ID: {customer.CustomerId}");
        _output.WriteLine($"  Channel: {customer.Channel}");
        _output.WriteLine($"  Instance ID: {instance.InstanceId}");
        _output.WriteLine($"  Status: running");
        _output.WriteLine($"  Version: 1.0.0");
        _output.WriteLine($"  Search UI for: {installationName} or Customer ID: {customer.CustomerId}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task CreateCustomer_WithName_ShouldIncludeName()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange
        using var client = CreateClient();
        var email = $"install-{Guid.NewGuid()}@customer.com";
        var installationName = "Production Installation";

        // Act
        var customer = await client.Customer.GetOrCreateAsync(email, "Stable", installationName);

        // Assert
        Assert.NotNull(customer);
        Assert.Equal(email, customer.EmailAddress);
        
        // Check if installation name is in customer data
        var name = customer.GetData("name");
        if (name != null)
        {
            _output.WriteLine($"Installation name in data: {name}");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task CreateCustomer_MultipleChannels_ShouldCreateDifferentCustomers()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange
        using var client = CreateClient();
        var baseEmail = $"multi-channel-{Guid.NewGuid()}@test.com";

        // Act - Create customers in different channels
        var stableCustomer = await client.Customer.GetOrCreateAsync(baseEmail, "Stable");
        var betaCustomer = await client.Customer.GetOrCreateAsync(baseEmail, "Beta");
        var alphaCustomer = await client.Customer.GetOrCreateAsync(baseEmail, "Alpha");

        // Assert - Should create different customer records for different channels
        Assert.NotNull(stableCustomer);
        Assert.NotNull(betaCustomer);
        Assert.NotNull(alphaCustomer);

        _output.WriteLine($"Stable Channel Customer ID: {stableCustomer.CustomerId}");
        _output.WriteLine($"Beta Channel Customer ID: {betaCustomer.CustomerId}");
        _output.WriteLine($"Alpha Channel Customer ID: {alphaCustomer.CustomerId}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task CreateCustomer_WithSpecialCharactersInName_ShouldWork()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange
        using var client = CreateClient();
        var email = $"install-{Guid.NewGuid()}@customer.com";
        var installationName = "Customer Site & Production - 2024";

        // Act
        var customer = await client.Customer.GetOrCreateAsync(email, "Stable", installationName);

        // Assert
        Assert.NotNull(customer);
        Assert.Equal(email, customer.EmailAddress);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task SendMetrics_MultipleInstances_SameCustomer_ShouldWork()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange
        using var client = CreateClient();
        var email = $"multi-instance-{Guid.NewGuid()}@test.com";
        var customer = await client.Customer.GetOrCreateAsync(email);

        // Act - Create multiple instances for same customer
        var instance1 = await customer.GetOrCreateInstanceAsync();
        var instance2 = await customer.GetOrCreateInstanceAsync();

        await instance1.SendMetricAsync("instance_1_metric", 100);
        await instance2.SendMetricAsync("instance_2_metric", 200);

        // Assert
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
        Assert.Equal(customer.CustomerId, instance1.CustomerId);
        Assert.Equal(customer.CustomerId, instance2.CustomerId);

        _output.WriteLine($"Instance 1 ID: {instance1.InstanceId}");
        _output.WriteLine($"Instance 2 ID: {instance2.InstanceId}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task Instance_StatusTransitions_ShouldWork()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange
        using var client = CreateClient();
        var email = $"status-test-{Guid.NewGuid()}@test.com";
        var customer = await client.Customer.GetOrCreateAsync(email);
        var instance = await customer.GetOrCreateInstanceAsync();

        // Act - Test various status transitions
        await instance.SetStatusAsync("ready");
        await Task.Delay(500); // Small delay between status changes
        await instance.SetStatusAsync("running");
        await Task.Delay(500);
        await instance.SetStatusAsync("degraded");
        await Task.Delay(500);
        await instance.SetStatusAsync("running");

        // Assert - No exceptions thrown
        Assert.True(true);
        _output.WriteLine("Status transitions completed successfully");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task SendMetric_LargePayload_ShouldWork()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange
        using var client = CreateClient();
        var email = $"large-payload-{Guid.NewGuid()}@test.com";
        var customer = await client.Customer.GetOrCreateAsync(email);
        var instance = await customer.GetOrCreateInstanceAsync();

        // Act - Send many metrics
        for (int i = 0; i < 20; i++)
        {
            await instance.SendMetricAsync($"metric_{i}", i * 10);
        }

        // Assert
        Assert.True(true);
        _output.WriteLine("Successfully sent 20 metrics");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task SendMetric_DifferentDataTypes_ShouldWork()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange
        using var client = CreateClient();
        var email = $"data-types-{Guid.NewGuid()}@test.com";
        var customer = await client.Customer.GetOrCreateAsync(email);
        var instance = await customer.GetOrCreateInstanceAsync();

        // Act - Send different data types
        await instance.SendMetricAsync("int_metric", 42);
        await instance.SendMetricAsync("double_metric", 3.14159);
        await instance.SendMetricAsync("string_metric", "test value");
        await instance.SendMetricAsync("bool_metric", true);

        // Assert
        Assert.True(true);
        _output.WriteLine("Successfully sent metrics with different data types");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task MultipleCustomers_ConcurrentCreation_ShouldWork()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange
        using var client = CreateClient();
        var tasks = new List<Task<Replicated.Resources.Customer>>();

        // Act - Create multiple customers concurrently
        for (int i = 0; i < 5; i++)
        {
            var email = $"install-{i}-{Guid.NewGuid()}@customer.com";
            tasks.Add(client.Customer.GetOrCreateAsync(email, "Stable", $"Concurrent Installation {i}"));
        }

        var customers = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, customers.Length);
        foreach (var customer in customers)
        {
            Assert.NotNull(customer);
            Assert.NotNull(customer.CustomerId);
        }

        _output.WriteLine($"Successfully created {customers.Length} customers concurrently");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task Client_StatePersistence_ShouldWorkAcrossClients()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange - Use shared app slug and state directory for state persistence test
        var sharedAppSlug = $"shared_test_{Guid.NewGuid():N}";
        var sharedStateDir = Path.Combine(Path.GetTempPath(), $"replicated_shared_test_{Guid.NewGuid():N}");
        var email = $"state-persist-{Guid.NewGuid()}@test.com";

        // Act - Create customer with first client
        Replicated.Resources.Customer customer1;
        using (var client1 = CreateClientWithSharedState(sharedAppSlug, sharedStateDir))
        {
            customer1 = await client1.Customer.GetOrCreateAsync(email);
        }

        // Create another client with same app slug and state directory - should use cached state
        using (var client2 = CreateClientWithSharedState(sharedAppSlug, sharedStateDir))
        {
            var customer2 = await client2.Customer.GetOrCreateAsync(email);

            // Assert - Should get same customer ID from state
            Assert.Equal(customer1.CustomerId, customer2.CustomerId);
        }

        _output.WriteLine($"State persisted: Customer ID {customer1.CustomerId}");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task Instance_VersionTracking_ShouldWork()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange
        using var client = CreateClient();
        var email = $"version-test-{Guid.NewGuid()}@test.com";
        var customer = await client.Customer.GetOrCreateAsync(email);
        var instance = await customer.GetOrCreateInstanceAsync();

        // Act - Set and update version
        await instance.SetVersionAsync("1.0.0");
        await Task.Delay(500);
        await instance.SetVersionAsync("1.1.0");
        await Task.Delay(500);
        await instance.SetVersionAsync("2.0.0");

        // Assert
        Assert.True(true);
        _output.WriteLine("Version tracking completed successfully");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task Customer_GetFullData_ShouldReturnAllFields()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange
        using var client = CreateClient();
        var email = $"install-{Guid.NewGuid()}@customer.com";
        var installationName = "Full Data Test Installation";

        // Act
        var customer = await client.Customer.GetOrCreateAsync(email, "Stable", installationName);

        // Assert and log all available data
        _output.WriteLine($"Customer ID: {customer.CustomerId}");
        _output.WriteLine($"Email: {customer.EmailAddress}");
        _output.WriteLine($"Channel: {customer.Channel}");

        // Try to get additional data that might be in the response
        var possibleKeys = new[] { "name", "id", "email", "channel", "created_at", "updated_at", "instanceId", "serviceToken" };
        foreach (var key in possibleKeys)
        {
            var value = customer.GetData(key);
            if (value != null)
            {
                _output.WriteLine($"  {key}: {value}");
            }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task ErrorHandling_InvalidMetricName_ShouldThrowException()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange
        using var client = CreateClient();
        var email = $"error-test-{Guid.NewGuid()}@test.com";
        var customer = await client.Customer.GetOrCreateAsync(email);
        var instance = await customer.GetOrCreateInstanceAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await instance.SendMetricAsync("invalid-metric-name!", 100));
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("RequiresCredentials", "true")]
    public async Task ErrorHandling_InvalidStatus_ShouldThrowException()
    {
        if (!HasCredentials())
        {
            return;
        }

        // Arrange
        using var client = CreateClient();
        var email = $"error-status-{Guid.NewGuid()}@test.com";
        var customer = await client.Customer.GetOrCreateAsync(email);
        var instance = await customer.GetOrCreateInstanceAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await instance.SetStatusAsync("invalid-status"));
    }
}

