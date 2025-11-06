using System;
using Replicated;
using Replicated.Services;
using Xunit;

namespace Replicated.Tests;

/// <summary>
/// Tests for CustomerService edge cases and state management scenarios.
/// </summary>
public class CustomerServiceEdgeCaseTests
{
    [Fact]
    public void GetOrCreate_WithCachedCustomerIdAndMatchingEmail_ShouldReturnCachedCustomer()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerService = new CustomerService(mockClient);
        var email = "test@example.com";
        
        // Set cached customer ID and email in state manager
        mockClient.StateManager.SetCustomerId("cached_customer_123");
        mockClient.StateManager.SetCustomerEmail(email);

        // Act
        var customer = customerService.GetOrCreate(email);

        // Assert - Should return customer with cached ID without making request
        Assert.NotNull(customer);
        Assert.Equal("cached_customer_123", customer.CustomerId);
        Assert.Equal(email, customer.EmailAddress);
    }

    [Fact]
    public void GetOrCreate_WithCachedCustomerIdButDifferentEmail_ShouldClearStateAndCreateNew()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerService = new CustomerService(mockClient);
        var originalEmail = "original@example.com";
        var newEmail = "new@example.com";
        
        // Set cached state
        mockClient.StateManager.SetCustomerId("original_customer_123");
        mockClient.StateManager.SetCustomerEmail(originalEmail);

        // Act
        var customer = customerService.GetOrCreate(newEmail, "Stable");

        // Assert - Should clear old state and create new customer
        Assert.NotNull(customer);
        Assert.NotEqual("original_customer_123", customer.CustomerId);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithCachedCustomerIdAndMatchingEmail_ShouldReturnCachedCustomer()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerService = new CustomerService(mockClient);
        var email = "test@example.com";
        
        mockClient.StateManager.SetCustomerId("cached_customer_123");
        mockClient.StateManager.SetCustomerEmail(email);

        // Act
        var customer = await customerService.GetOrCreateAsync(email);

        // Assert
        Assert.NotNull(customer);
        Assert.Equal("cached_customer_123", customer.CustomerId);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithCachedCustomerIdButDifferentEmail_ShouldClearStateAndCreateNew()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerService = new CustomerService(mockClient);
        var originalEmail = "original@example.com";
        var newEmail = "new@example.com";
        
        mockClient.StateManager.SetCustomerId("original_customer_123");
        mockClient.StateManager.SetCustomerEmail(originalEmail);

        // Act
        var customer = await customerService.GetOrCreateAsync(newEmail);

        // Assert
        Assert.NotNull(customer);
        Assert.NotEqual("original_customer_123", customer.CustomerId);
    }

    private static IReplicatedClient CreateMockClient()
    {
        return new MockReplicatedClient();
    }

    private class MockReplicatedClient : IReplicatedClient
    {
        public string PublishableKey => "test_key";
        public string AppSlug => "test_app";
        public string BaseUrl => "https://test.replicated.app";
        public TimeSpan Timeout => TimeSpan.FromSeconds(30);
        public string? StateDirectory => null;
        public string MachineId => "test_machine_id";
        public StateManager StateManager { get; }
        public CustomerService Customer => new CustomerService(this);

        public Dictionary<string, string> GetAuthHeaders()
        {
            return new Dictionary<string, string> { ["Authorization"] = "Bearer test_token" };
        }

        public Dictionary<string, object> MakeRequest(string method, string url, Dictionary<string, string>? headers = null, Dictionary<string, object>? jsonData = null, Dictionary<string, object>? parameters = null)
        {
            // Generate new customer ID for each request
            var customerId = $"customer_{Guid.NewGuid().ToString("N")[..8]}";
            return new Dictionary<string, object>
            {
                ["customer"] = new Dictionary<string, object>
                {
                    ["id"] = customerId,
                    ["email"] = "test@example.com",
                    ["instanceId"] = "instance_456"
                },
                ["instance_id"] = "instance_456"
            };
        }

        public Task<Dictionary<string, object>> MakeRequestAsync(string method, string url, Dictionary<string, string>? headers = null, Dictionary<string, object>? jsonData = null, Dictionary<string, object>? parameters = null)
        {
            return Task.FromResult(MakeRequest(method, url, headers, jsonData, parameters));
        }

        public MockReplicatedClient()
        {
            // Isolate state per test with unique app slug and temp directory
            var uniqueSlug = $"test_app_{Guid.NewGuid().ToString("N")[..8]}";
            var tempDir = Path.Combine(Path.GetTempPath(), $"replicated_state_{Guid.NewGuid().ToString("N")[..8]}");
            Directory.CreateDirectory(tempDir);
            StateManager = new StateManager(uniqueSlug, tempDir);
            // Ensure clean
            StateManager.ClearState();
        }
    }
}

