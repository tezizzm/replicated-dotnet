using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Replicated;
using Replicated.Resources;
using Replicated.Services;
using Xunit;

namespace Replicated.Tests;

/// <summary>
/// Tests for Instance class edge cases and error scenarios.
/// </summary>
public class InstanceEdgeCaseTests
{
    [Fact]
    public void SetStatus_WithoutInstanceId_ShouldTriggerInstanceCreation()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123"); // No instance ID initially

        // Act
        instance.SetStatus("running");

        // Assert - Should not throw (mock handles the instance creation)
        Assert.NotNull(instance);
    }

    [Fact]
    public async Task SetStatusAsync_WithoutInstanceId_ShouldTriggerInstanceCreation()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123");

        // Act
        await instance.SetStatusAsync("running");

        // Assert - Should not throw
        Assert.NotNull(instance);
    }

    [Fact]
    public void SetVersion_WithoutInstanceId_ShouldTriggerInstanceCreation()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123");

        // Act
        instance.SetVersion("1.0.0");

        // Assert - Should not throw
        Assert.NotNull(instance);
    }

    [Fact]
    public async Task SetVersionAsync_WithoutInstanceId_ShouldTriggerInstanceCreation()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123");

        // Act
        await instance.SetVersionAsync("1.0.0");

        // Assert - Should not throw
        Assert.NotNull(instance);
    }

    [Fact]
    public void SendMetric_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => instance.SendMetric("test_metric", null!));
    }

    [Fact]
    public async Task SendMetricAsync_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await instance.SendMetricAsync("test_metric", null!));
    }

    [Theory]
    [InlineData("running")]
    [InlineData("RUNNING")]
    [InlineData("Running")]
    [InlineData("degraded")]
    [InlineData("missing")]
    [InlineData("unavailable")]
    [InlineData("ready")]
    [InlineData("updating")]
    public void SetStatus_WithCaseInsensitiveStatus_ShouldAcceptAllCases(string status)
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456");

        // Act & Assert - Should not throw (validation accepts case-insensitive)
        instance.SetStatus(status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(-100)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void SendMetric_WithIntValues_ShouldAcceptAllIntegers(int value)
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456");

        // Act & Assert - Should not throw
        instance.SendMetric("test_metric", value);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(100.99)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    public void SendMetric_WithDoubleValues_ShouldAcceptAllDoubles(double value)
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456");

        // Act & Assert - Should not throw
        instance.SendMetric("test_metric", value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("test_value")]
    [InlineData("Test Value With Spaces")]
    public void SendMetric_WithStringValues_ShouldAcceptAllStrings(string value)
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456");

        // Act & Assert - Should not throw
        instance.SendMetric("test_metric", value);
    }

    [Fact]
    public void SendMetric_WithBooleanValues_ShouldAcceptBooleans()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456");

        // Act & Assert - Should not throw
        instance.SendMetric("test_bool", true);
        instance.SendMetric("test_bool", false);
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
        public StateManager StateManager => new StateManager("test_app");
        public CustomerService Customer => new CustomerService(this);

        public Dictionary<string, string> GetAuthHeaders()
        {
            return new Dictionary<string, string> { ["Authorization"] = "Bearer test_token" };
        }

        public Dictionary<string, object> MakeRequest(string method, string url, Dictionary<string, string>? headers = null, Dictionary<string, object>? jsonData = null, Dictionary<string, object>? parameters = null)
        {
            // Return mock response for instance creation
            if (url.Contains("/v3/instance"))
            {
                return new Dictionary<string, object>
                {
                    ["instance"] = new Dictionary<string, object>
                    {
                        ["id"] = "instance_456"
                    },
                    ["instance_id"] = "instance_456"
                };
            }
            return new Dictionary<string, object> { ["success"] = true };
        }

        public Task<Dictionary<string, object>> MakeRequestAsync(string method, string url, Dictionary<string, string>? headers = null, Dictionary<string, object>? jsonData = null, Dictionary<string, object>? parameters = null)
        {
            return Task.FromResult(MakeRequest(method, url, headers, jsonData, parameters));
        }
    }
}

