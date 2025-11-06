using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Replicated;
using Replicated.Resources;
using Xunit;

namespace Replicated.IntegrationTests;

/// <summary>
/// Integration tests using the mock server for comprehensive HTTP error testing.
/// These tests exercise real HTTP client behavior with controlled responses.
/// </summary>
public class MockServerIntegrationTests : IntegrationTestBase, IClassFixture<ServerFixture>
{
    public MockServerIntegrationTests(ServerFixture server) : base(server)
    {
    }

    #region Customer Endpoint Tests

    [Fact]
    public void Customer_GetOrCreate_WithSuccess_ShouldReturnCustomer()
    {
        var client = CreateClient();
        var customer = client.Customer.GetOrCreate("install@example.com", "Test Installation");
        
        Assert.NotNull(customer);
        Assert.NotNull(customer.CustomerId);
        Assert.Equal("install@example.com", customer.EmailAddress);
    }

    [Fact]
    public async Task Customer_GetOrCreateAsync_WithSuccess_ShouldReturnCustomer()
    {
        var client = CreateClient();
        var customer = await client.Customer.GetOrCreateAsync("install@example.com", "Test Installation");
        
        Assert.NotNull(customer);
        Assert.NotNull(customer.CustomerId);
        Assert.Equal("install@example.com", customer.EmailAddress);
    }

    [Fact]
    public void Customer_GetOrCreate_With401_ShouldThrowAuthError()
    {
        var client = CreateClient("401");
        
        var exception = Assert.Throws<ReplicatedAuthError>(() => 
            client.Customer.GetOrCreate("install@example.com", "Test Installation"));
        
        Assert.Equal(401, exception.HttpStatus);
        Assert.Contains("Unauthorized", exception.Message);
        Assert.Equal("AUTH", exception.Code);
    }

    [Fact]
    public async Task Customer_GetOrCreateAsync_With401_ShouldThrowAuthError()
    {
        var client = CreateClient("401");
        
        var exception = await Assert.ThrowsAsync<ReplicatedAuthError>(async () => 
            await client.Customer.GetOrCreateAsync("install@example.com", "Test Installation"));
        
        Assert.Equal(401, exception.HttpStatus);
        Assert.Contains("Unauthorized", exception.Message);
        Assert.Equal("AUTH", exception.Code);
    }

    [Fact]
    public void Customer_GetOrCreate_With403_ShouldThrowApiError()
    {
        var client = CreateClient("403");
        
        // 403 Forbidden is treated as ReplicatedApiError, not ReplicatedAuthError
        // Only 401 Unauthorized throws ReplicatedAuthError
        var exception = Assert.Throws<ReplicatedApiError>(() => 
            client.Customer.GetOrCreate("install@example.com", "Test Installation"));
        
        Assert.Equal(403, exception.HttpStatus);
        Assert.Contains("Forbidden", exception.Message);
        Assert.Equal("FORBIDDEN", exception.Code);
    }

    [Fact]
    public void Customer_GetOrCreate_With429_ShouldThrowRateLimitError()
    {
        var client = CreateClient("429");
        
        var exception = Assert.Throws<ReplicatedRateLimitError>(() => 
            client.Customer.GetOrCreate("install@example.com", "Test Installation"));
        
        Assert.Equal(429, exception.HttpStatus);
        Assert.Contains("Rate limit exceeded", exception.Message);
        Assert.Equal("RATE_LIMIT", exception.Code);
    }

    [Fact]
    public void Customer_GetOrCreate_With400_ShouldThrowApiError()
    {
        var client = CreateClient("400");
        
        var exception = Assert.Throws<ReplicatedApiError>(() => 
            client.Customer.GetOrCreate("install@example.com", "Test Installation"));
        
        Assert.Equal(400, exception.HttpStatus);
        Assert.Contains("Bad Request", exception.Message);
        Assert.Equal("INVALID_REQUEST", exception.Code);
    }

    [Fact]
    public void Customer_GetOrCreate_With500_ShouldThrowApiError()
    {
        var client = CreateClient("500");
        
        var exception = Assert.Throws<ReplicatedApiError>(() => 
            client.Customer.GetOrCreate("install@example.com", "Test Installation"));
        
        Assert.Equal(500, exception.HttpStatus);
        Assert.Contains("Internal Server Error", exception.Message);
        Assert.Equal("SERVER_ERROR", exception.Code);
    }

    [Fact]
    public void Customer_GetOrCreate_With502_ShouldThrowApiError()
    {
        var client = CreateClient("502");
        
        var exception = Assert.Throws<ReplicatedApiError>(() => 
            client.Customer.GetOrCreate("install@example.com", "Test Installation"));
        
        Assert.Equal(502, exception.HttpStatus);
        Assert.Contains("Bad Gateway", exception.Message);
    }

    [Fact]
    public void Customer_GetOrCreate_With503_ShouldThrowApiError()
    {
        var client = CreateClient("503");
        
        var exception = Assert.Throws<ReplicatedApiError>(() => 
            client.Customer.GetOrCreate("install@example.com", "Test Installation"));
        
        Assert.Equal(503, exception.HttpStatus);
        Assert.Contains("Service Unavailable", exception.Message);
    }

    [Fact]
    public void Customer_GetOrCreate_With504_ShouldThrowApiError()
    {
        var client = CreateClient("504");
        
        var exception = Assert.Throws<ReplicatedApiError>(() => 
            client.Customer.GetOrCreate("install@example.com", "Test Installation"));
        
        Assert.Equal(504, exception.HttpStatus);
        Assert.Contains("Gateway Timeout", exception.Message);
    }

    #endregion

    #region Instance Endpoint Tests

    [Fact]
    public void Instance_GetOrCreateInstance_WithSuccess_ShouldReturnInstance()
    {
        var client = CreateClient();
        var customer = client.Customer.GetOrCreate("install@example.com", "Test Installation");
        var instance = customer.GetOrCreateInstance();
        
        Assert.NotNull(instance);
        
        // Instance ID is set lazily when an operation that requires it is performed
        // Trigger instance creation by sending a metric
        instance.SendMetric("test_metric", 123);
        
        Assert.NotNull(instance.InstanceId);
        Assert.NotEmpty(instance.InstanceId);
    }

    [Fact]
    public async Task Instance_GetOrCreateInstanceAsync_WithSuccess_ShouldReturnInstance()
    {
        var client = CreateClient();
        var customer = await client.Customer.GetOrCreateAsync("install@example.com", "Test Installation");
        var instance = await customer.GetOrCreateInstanceAsync();
        
        Assert.NotNull(instance);
        
        // Instance ID is set lazily when an operation that requires it is performed
        // Trigger instance creation by sending a metric
        await instance.SendMetricAsync("test_metric", 123);
        
        Assert.NotNull(instance.InstanceId);
        Assert.NotEmpty(instance.InstanceId);
    }

    [Fact]
    public void Instance_GetOrCreateInstance_With500_ShouldThrowApiError()
    {
        // GetOrCreateInstance() doesn't make HTTP requests - it just creates an Instance object
        // So we need to trigger an HTTP request by calling SendMetric which will call EnsureInstance()
        // This tests that EnsureInstance() fails with 500 when instance creation is attempted
        // First create customer with normal client so it succeeds
        var normalClient = CreateClient();
        var customer = normalClient.Customer.GetOrCreate("install@example.com", "Test Installation");
        var customerId = customer.CustomerId;
        
        // Now create error client and instance - this will fail when EnsureInstance() is called
        var errorClient = CreateClient("500");
        var errorInstance = new Instance(errorClient, customerId, null);
        
        // EnsureInstance() will be called here and should fail with 500
        var exception = Assert.Throws<ReplicatedApiError>(() => 
            errorInstance.SendMetric("test_metric", 123));
        
        Assert.Equal(500, exception.HttpStatus);
        Assert.Contains("Internal Server Error", exception.Message);
    }

    [Fact]
    public void Instance_GetOrCreateInstance_With429_ShouldThrowRateLimitError()
    {
        // GetOrCreateInstance() doesn't make HTTP requests - it just creates an Instance object
        // So we need to trigger an HTTP request by calling SendMetric which will call EnsureInstance()
        // This tests that EnsureInstance() fails with 429 when instance creation is attempted
        // First create customer with normal client so it succeeds
        var normalClient = CreateClient();
        var customer = normalClient.Customer.GetOrCreate("install@example.com", "Test Installation");
        var customerId = customer.CustomerId;
        
        // Now create error client and instance - this will fail when EnsureInstance() is called
        var errorClient = CreateClient("429");
        var errorInstance = new Instance(errorClient, customerId, null);
        
        // EnsureInstance() will be called here and should fail with 429
        var exception = Assert.Throws<ReplicatedRateLimitError>(() => 
            errorInstance.SendMetric("test_metric", 123));
        
        Assert.Equal(429, exception.HttpStatus);
        Assert.Contains("Rate limit exceeded", exception.Message);
    }

    #endregion

    #region Metrics Endpoint Tests

    [Fact]
    public void Instance_SendMetric_WithSuccess_ShouldNotThrow()
    {
        var client = CreateClient();
        var customer = client.Customer.GetOrCreate("install@example.com", "Test Installation");
        var instance = customer.GetOrCreateInstance();
        
        // Should not throw
        instance.SendMetric("test_metric", 123);
    }

    [Fact]
    public async Task Instance_SendMetricAsync_WithSuccess_ShouldNotThrow()
    {
        var client = CreateClient();
        var customer = await client.Customer.GetOrCreateAsync("install@example.com", "Test Installation");
        var instance = await customer.GetOrCreateInstanceAsync();
        
        // Should not throw
        await instance.SendMetricAsync("test_metric", 123);
    }

    [Fact]
    public void Instance_SendMetric_With500_ShouldThrowApiError()
    {
        // First, create instance with normal client so we get the instance ID
        var normalClient = CreateClient();
        var customer = normalClient.Customer.GetOrCreate("install@example.com", "Test Installation");
        var normalInstance = customer.GetOrCreateInstance();
        // Trigger instance creation by sending a metric (this will succeed)
        normalInstance.SendMetric("setup_metric", 1);
        var instanceId = normalInstance.InstanceId;
        var customerId = customer.CustomerId;
        Assert.NotNull(instanceId);
        Assert.NotNull(customerId);
        
        // Now create a client with error status and use the pre-created instance
        // This will skip EnsureInstance() and go directly to the metrics endpoint
        var errorClient = CreateClient("500");
        // Create instance with pre-set customer ID and instance ID so EnsureInstance() won't be called
        var errorInstance = new Instance(errorClient, customerId, instanceId);
        
        var exception = Assert.Throws<ReplicatedApiError>(() => 
            errorInstance.SendMetric("test_metric", 123));
        
        Assert.Equal(500, exception.HttpStatus);
        Assert.Contains("Internal Server Error", exception.Message);
    }

    [Fact]
    public void Instance_SendMetric_With429_ShouldThrowRateLimitError()
    {
        // First, create instance with normal client so we get the instance ID
        var normalClient = CreateClient();
        var customer = normalClient.Customer.GetOrCreate("install@example.com", "Test Installation");
        var normalInstance = customer.GetOrCreateInstance();
        // Trigger instance creation by sending a metric (this will succeed)
        normalInstance.SendMetric("setup_metric", 1);
        var instanceId = normalInstance.InstanceId;
        var customerId = customer.CustomerId;
        Assert.NotNull(instanceId);
        Assert.NotNull(customerId);
        
        // Now create a client with error status and use the pre-created instance
        // This will skip EnsureInstance() and go directly to the metrics endpoint
        var errorClient = CreateClient("429");
        // Create instance with pre-set customer ID and instance ID so EnsureInstance() won't be called
        var errorInstance = new Instance(errorClient, customerId, instanceId);
        
        var exception = Assert.Throws<ReplicatedRateLimitError>(() => 
            errorInstance.SendMetric("test_metric", 123));
        
        Assert.Equal(429, exception.HttpStatus);
        Assert.Contains("Rate limit exceeded", exception.Message);
    }

    #endregion

    #region Instance Info Endpoint Tests

    [Fact]
    public void Instance_GetData_WithSuccess_ShouldReturnData()
    {
        var client = CreateClient();
        var customer = client.Customer.GetOrCreate("install@example.com", "Test Installation");
        
        // GetData returns data from the internal dictionary, which is empty by default
        // So we test with data passed in constructor
        var instanceData = new Dictionary<string, object> { ["test_key"] = "test_value" };
        var instance = new Instance(client, customer.CustomerId, null, instanceData);
        
        var data = instance.GetData("test_key");
        
        Assert.NotNull(data);
        Assert.Equal("test_value", data);
    }

    [Fact]
    public void Instance_GetData_WithMissingKey_ShouldReturnNull()
    {
        var client = CreateClient();
        var customer = client.Customer.GetOrCreate("install@example.com", "Test Installation");
        var instance = customer.GetOrCreateInstance();
        
        // GetData doesn't make HTTP requests, it just reads from internal dictionary
        var data = instance.GetData("nonexistent_key");
        
        Assert.Null(data);
    }

    [Fact]
    public void Instance_GetOrCreate_With404_ShouldThrowApiError()
    {
        // Test that when status code 404 is injected, the first HTTP call (customer creation) throws on 404
        // Note: Since header injection applies to all requests, customer creation will fail with 404
        var client = CreateClient("404");
        
        // Customer creation should fail with 404 since the header applies to all requests
        var exception = Assert.Throws<ReplicatedApiError>(() => 
            client.Customer.GetOrCreate("install@example.com", "Test Installation"));
        
        Assert.Equal(404, exception.HttpStatus);
    }

    #endregion

    #region Retry Behavior Tests

    [Fact]
    public void Customer_GetOrCreate_WithRetryPolicy_ShouldRetryOn5xx()
    {
        // Use header injection for status code
        var client = CreateClient("500", new RetryPolicy
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromMilliseconds(500),
            RetryOnServerError = true
        });
        
        // This should retry 2 times then fail
        var exception = Assert.Throws<ReplicatedApiError>(() => 
            client.Customer.GetOrCreate("install@example.com", "Test Installation"));
        
        Assert.Equal(500, exception.HttpStatus);
    }

    [Fact]
    public void Customer_GetOrCreate_WithRetryPolicy_ShouldNotRetryOn4xx()
    {
        // Use header injection for status code
        var client = CreateClient("400", new RetryPolicy
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromMilliseconds(500),
            RetryOnServerError = false
        });
        
        // This should not retry on 4xx errors
        var exception = Assert.Throws<ReplicatedApiError>(() => 
            client.Customer.GetOrCreate("install@example.com", "Test Installation"));
        
        Assert.Equal(400, exception.HttpStatus);
    }

    #endregion

    #region End-to-End Workflow Tests

    [Fact]
    public void EndToEnd_CompleteWorkflow_WithSuccess_ShouldWork()
    {
        var client = CreateClient();
        
        // Create customer
        var customer = client.Customer.GetOrCreate("install@example.com", "Test Installation");
        Assert.NotNull(customer);
        
        // Create instance
        var instance = customer.GetOrCreateInstance();
        Assert.NotNull(instance);
        
        // Send metrics
        instance.SendMetric("startup_time", 1500);
        instance.SendMetric("memory_usage", 256.5);
        
        // Set status
        instance.SetStatus("running");
        
        // Set version
        instance.SetVersion("1.0.0");
        
        // Verify instance was created (instance ID should be set after operations that require it)
        Assert.NotNull(instance.InstanceId);
        Assert.NotEmpty(instance.InstanceId);
        
        // All operations should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task EndToEnd_CompleteWorkflowAsync_WithSuccess_ShouldWork()
    {
        var client = CreateClient();
        
        // Create customer
        var customer = await client.Customer.GetOrCreateAsync("install@example.com", "Test Installation");
        Assert.NotNull(customer);
        
        // Create instance
        var instance = await customer.GetOrCreateInstanceAsync();
        Assert.NotNull(instance);
        
        // Send metrics
        await instance.SendMetricAsync("startup_time", 1500);
        await instance.SendMetricAsync("memory_usage", 256.5);
        
        // Set status
        await instance.SetStatusAsync("running");
        
        // Set version
        await instance.SetVersionAsync("1.0.0");
        
        // Verify instance was created (instance ID should be set after operations that require it)
        Assert.NotNull(instance.InstanceId);
        Assert.NotEmpty(instance.InstanceId);
        
        // All operations should complete without throwing
        Assert.True(true);
    }

    #endregion
}
