# Replicated .NET SDK - Usage Examples

This document provides comprehensive examples for using the Replicated .NET SDK.

## Table of Contents

- [Basic Usage](#basic-usage)
- [Asynchronous Usage](#asynchronous-usage)
- [Fluent Builder Pattern](#fluent-builder-pattern)
- [Configuration Options](#configuration-options)
- [Error Handling](#error-handling)
- [Retry Policy Configuration](#retry-policy-configuration)
- [Environment Variables](#environment-variables)
- [Advanced Scenarios](#advanced-scenarios)

---

## Basic Usage

### Simple Client Creation and Customer Management

```csharp
using Replicated;

// Create a client using environment variables
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .Build();

// Get or create a customer (email should come from user context or configuration)
var customerEmail = Environment.GetEnvironmentVariable("CUSTOMER_EMAIL") ?? "user@example.com";
var customer = client.Customer.GetOrCreate(customerEmail);

// Get or create an instance for the customer
var instance = customer.GetOrCreateInstance();

// Send metrics
instance.SendMetric("cpu_usage", 0.75);
instance.SendMetric("memory_usage", 0.60);
instance.SendMetric("active_users", 150);

// Set instance status and version
instance.SetStatus("running");
instance.SetVersion("1.0.0");

// Dispose the client when done
client.Dispose();
```

### Using Named Arguments

```csharp
// Get customer email from user context or configuration
var customerEmail = Environment.GetEnvironmentVariable("CUSTOMER_EMAIL") ?? "user@example.com";
var customerName = Environment.GetEnvironmentVariable("CUSTOMER_NAME") ?? "John Doe";

var customer = client.Customer.GetOrCreate(
    emailAddress: customerEmail,
    channel: "Stable",
    name: customerName
);
```

---

## Asynchronous Usage

### Async/Await Pattern

```csharp
using Replicated;

// Create client with 'await using' for automatic disposal
// Uses environment variables for configuration
await using var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .Build();

// Use async methods
var customerEmail = Environment.GetEnvironmentVariable("CUSTOMER_EMAIL") ?? "user@example.com";
var customer = await client.Customer.GetOrCreateAsync(customerEmail);
var instance = await customer.GetOrCreateInstanceAsync();

await instance.SendMetricAsync("cpu_usage", 0.75);
await instance.SetStatusAsync("running");
await instance.SetVersionAsync("1.0.0");
```

### Async in a Background Service

```csharp
using Replicated;

public class MetricsService : BackgroundService
{
    private ReplicatedClient _client;
    private Instance _instance;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create client from environment variables
        _client = new ReplicatedClientBuilder()
            .FromEnvironment()
            .Build();

        // Get service email from configuration
        var serviceEmail = Environment.GetEnvironmentVariable("SERVICE_EMAIL") ?? "service@example.com";
        var customer = await _client.Customer.GetOrCreateAsync(serviceEmail);
        _instance = await customer.GetOrCreateInstanceAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            // Collect and send metrics periodically
            await CollectMetricsAsync();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CollectMetricsAsync()
    {
        var cpuUsage = GetCpuUsage();
        var memoryUsage = GetMemoryUsage();

        await _instance.SendMetricAsync("cpu_usage", cpuUsage);
        await _instance.SendMetricAsync("memory_usage", memoryUsage);
        await _instance.SetStatusAsync("running");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
```

---

## Fluent Builder Pattern

### Complete Configuration

```csharp
// Start with environment variables, then override specific settings
var client = new ReplicatedClientBuilder()
    .FromEnvironment() // Reads base configuration from environment
    .WithBaseUrl(Environment.GetEnvironmentVariable("REPLICATED_BASE_URL") ?? "https://replicated.app")
    .WithTimeout(TimeSpan.FromSeconds(
        int.TryParse(Environment.GetEnvironmentVariable("REPLICATED_TIMEOUT"), out var timeout) 
            ? timeout 
            : 60))
    .WithStateDirectory(Environment.GetEnvironmentVariable("REPLICATED_STATE_DIRECTORY") ?? "/custom/path/to/state")
    .WithRetryPolicy(
        maxRetries: 5,
        initialDelay: TimeSpan.FromSeconds(2),
        maxDelay: TimeSpan.FromMinutes(2),
        backoffMultiplier: 2.0,
        useJitter: true
    )
    .Build();
```

### Using Environment Variables

```csharp
var client = new ReplicatedClientBuilder()
    .FromEnvironment() // Reads from environment variables
    .WithTimeout(TimeSpan.FromSeconds(60)) // Override timeout
    .Build();
```

### Minimal Configuration

```csharp
// Minimal configuration using environment variables
// Reads REPLICATED_PUBLISHABLE_KEY and REPLICATED_APP_SLUG from environment
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .Build();
```

---

## Configuration Options

### Custom Timeout

```csharp
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .WithTimeout(TimeSpan.FromSeconds(
        int.TryParse(Environment.GetEnvironmentVariable("REPLICATED_TIMEOUT"), out var timeout) 
            ? timeout 
            : 60))
    .Build();
```

### Custom Base URL

```csharp
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .WithBaseUrl(Environment.GetEnvironmentVariable("REPLICATED_BASE_URL") ?? "https://replicated.app")
    .Build();
```

### Custom State Directory

```csharp
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .WithStateDirectory(Environment.GetEnvironmentVariable("REPLICATED_STATE_DIRECTORY") ?? "/var/lib/myapp/replicated-state")
    .Build();
```

---

## Error Handling

### Basic Error Handling

```csharp
using Replicated;

try
{
    var client = new ReplicatedClientBuilder()
        .FromEnvironment()
        .Build();
    
    var customerEmail = Environment.GetEnvironmentVariable("CUSTOMER_EMAIL") ?? "user@example.com";
    var customer = client.Customer.GetOrCreate(customerEmail);
}
catch (ReplicatedAuthError ex)
{
    Console.WriteLine($"Authentication failed: {ex.Message}");
    Console.WriteLine($"HTTP Status: {ex.HttpStatus}");
}
catch (ReplicatedApiError ex)
{
    Console.WriteLine($"API Error: {ex.HttpStatus} - {ex.Message}");
    if (ex.JsonBody != null)
    {
        Console.WriteLine($"Error Details: {System.Text.Json.JsonSerializer.Serialize(ex.JsonBody)}");
    }
}
catch (ReplicatedNetworkError ex)
{
    Console.WriteLine($"Network error: {ex.Message}");
}
catch (ReplicatedException ex)
{
    Console.WriteLine($"Replicated error: {ex.Message}");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
```

### Comprehensive Error Handling with Logging

```csharp
using Replicated;
using Microsoft.Extensions.Logging;

public class ReplicatedService
{
    private readonly ILogger<ReplicatedService> _logger;
    private readonly ReplicatedClient _client;

    public ReplicatedService(ILogger<ReplicatedService> logger, ReplicatedClient client)
    {
        _logger = logger;
        _client = client;
    }

    public async Task<bool> SendMetricsAsync()
    {
        try
        {
            // Get customer email from configuration or user context
            var customerEmail = Environment.GetEnvironmentVariable("CUSTOMER_EMAIL") ?? "user@example.com";
            var customer = await _client.Customer.GetOrCreateAsync(customerEmail);
            var instance = await customer.GetOrCreateInstanceAsync();
            
            await instance.SendMetricAsync("status", "healthy");
            return true;
        }
        catch (ReplicatedAuthError ex)
        {
            _logger.LogError(ex, "Authentication failed: {Message} (Status: {Status})", 
                ex.Message, ex.HttpStatus);
            return false;
        }
        catch (ReplicatedRateLimitError ex)
        {
            _logger.LogWarning(ex, "Rate limit exceeded. Retrying after delay...");
            await Task.Delay(TimeSpan.FromSeconds(60));
            return await SendMetricsAsync(); // Retry
        }
        catch (ReplicatedNetworkError ex)
        {
            _logger.LogWarning(ex, "Network error occurred. Will retry on next cycle.");
            return false;
        }
        catch (ReplicatedApiError ex)
        {
            _logger.LogError(ex, "API error: {Status} - {Message}", 
                ex.HttpStatus, ex.Message);
            return false;
        }
    }
}
```

---

## Retry Policy Configuration

### Default Retry Policy

```csharp
// Uses default: 3 retries, 1s initial delay, exponential backoff
// Configuration comes from environment variables
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .Build();
```

### Custom Retry Policy

```csharp
var retryPolicy = new RetryPolicy
{
    MaxRetries = 5,
    InitialDelay = TimeSpan.FromSeconds(2),
    MaxDelay = TimeSpan.FromMinutes(2),
    BackoffMultiplier = 2.0,
    UseJitter = true,
    JitterPercentage = 0.1,
    RetryOnRateLimit = true,
    RetryOnServerError = true,
    RetryOnNetworkError = true
};

var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .WithRetryPolicy(retryPolicy)
    .Build();
```

### Disable Retries

```csharp
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .WithoutRetries()
    .Build();
```

### Retry Policy via Builder

```csharp
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .WithRetryPolicy(
        maxRetries: 5,
        initialDelay: TimeSpan.FromSeconds(2),
        maxDelay: TimeSpan.FromMinutes(2),
        backoffMultiplier: 2.5,
        useJitter: true
    )
    .Build();
```

---

## Environment Variables

### Using Environment Variables

```bash
export REPLICATED_PUBLISHABLE_KEY="replicated_pk_your_key_here"
export REPLICATED_APP_SLUG="my-app"
export REPLICATED_BASE_URL="https://replicated.app"
export REPLICATED_TIMEOUT="60"  # seconds
export REPLICATED_STATE_DIRECTORY="/custom/path"
```

Then create the client:

```csharp
// Reads configuration from environment variables
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .Build();
```

### Partial Environment Configuration

```csharp
// Use environment for key and slug, override timeout
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .WithTimeout(TimeSpan.FromSeconds(120))
    .Build();
```

### Environment Variables Reference

| Variable | Description | Default |
|----------|-------------|---------|
| `REPLICATED_PUBLISHABLE_KEY` | Your Replicated publishable key | Required |
| `REPLICATED_APP_SLUG` | Your application slug | Required |
| `REPLICATED_BASE_URL` | Base URL for API | `https://replicated.app` |
| `REPLICATED_TIMEOUT` | Request timeout in seconds | `30` |
| `REPLICATED_STATE_DIRECTORY` | Custom state directory path | Platform-specific default |
| `REPLICATED_RETRY_MAX_RETRIES` | Maximum retry attempts | `3` |
| `REPLICATED_RETRY_INITIAL_DELAY` | Initial retry delay in milliseconds | `1000` |
| `REPLICATED_RETRY_MAX_DELAY` | Maximum retry delay in milliseconds | `30000` |
| `REPLICATED_RETRY_BACKOFF_MULTIPLIER` | Exponential backoff multiplier | `2.0` |
| `REPLICATED_RETRY_USE_JITTER` | Enable jitter (`true`/`false`) | `true` |
| `REPLICATED_RETRY_JITTER_PERCENTAGE` | Jitter percentage (0.0-1.0) | `0.1` |
| `REPLICATED_RETRY_ON_RATE_LIMIT` | Retry on rate limit (`true`/`false`) | `true` |
| `REPLICATED_RETRY_ON_SERVER_ERROR` | Retry on server errors (`true`/`false`) | `true` |
| `REPLICATED_RETRY_ON_NETWORK_ERROR` | Retry on network errors (`true`/`false`) | `true` |

---

## Advanced Scenarios

### Multiple Customers

```csharp
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .Build();

// Manage multiple customers (emails should come from your application context)
var customer1Email = Environment.GetEnvironmentVariable("CUSTOMER_1_EMAIL") ?? "customer1@example.com";
var customer1 = client.Customer.GetOrCreate(customer1Email);
var instance1 = customer1.GetOrCreateInstance();
instance1.SendMetric("metric1", 100);

var customer2Email = Environment.GetEnvironmentVariable("CUSTOMER_2_EMAIL") ?? "customer2@example.com";
var customer2 = client.Customer.GetOrCreate(customer2Email);
var instance2 = customer2.GetOrCreateInstance();
instance2.SendMetric("metric2", 200);
```

### Dependency Injection (ASP.NET Core)

```csharp
// In Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<ReplicatedClient>(provider =>
    {
        var config = provider.GetRequiredService<IConfiguration>();
        
        // Use builder pattern with configuration
        var builder = new ReplicatedClientBuilder()
            .WithPublishableKey(config["Replicated:PublishableKey"] ?? 
                              Environment.GetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY") ?? 
                              throw new InvalidOperationException("Replicated:PublishableKey is required"))
            .WithAppSlug(config["Replicated:AppSlug"] ?? 
                        Environment.GetEnvironmentVariable("REPLICATED_APP_SLUG") ?? 
                        throw new InvalidOperationException("Replicated:AppSlug is required"));
        
        // Optional: override timeout from configuration
        if (int.TryParse(config["Replicated:Timeout"], out var timeoutSeconds))
        {
            builder.WithTimeout(TimeSpan.FromSeconds(timeoutSeconds));
        }
        
        return builder.Build();
    });
    
    services.AddHostedService<MetricsBackgroundService>();
}
```

### Dependency Injection with Options Pattern

```csharp
// Configure options with fallback to environment variables
services.Configure<ReplicatedOptions>(options =>
{
    var config = configuration.GetSection("Replicated");
    options.PublishableKey = config["PublishableKey"] ?? 
                             Environment.GetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY") ?? 
                             throw new InvalidOperationException("Replicated:PublishableKey is required");
    options.AppSlug = config["AppSlug"] ?? 
                      Environment.GetEnvironmentVariable("REPLICATED_APP_SLUG") ?? 
                      throw new InvalidOperationException("Replicated:AppSlug is required");
});

// Register client
services.AddSingleton<ReplicatedClient>(provider =>
{
    var options = provider.GetRequiredService<IOptions<ReplicatedOptions>>().Value;
    return new ReplicatedClientBuilder()
        .WithPublishableKey(options.PublishableKey)
        .WithAppSlug(options.AppSlug)
        .Build();
});
```

### Metrics Collection Service

```csharp
public class SystemMetricsCollector
{
    private readonly Instance _instance;
    private readonly Timer _timer;

    public SystemMetricsCollector(Instance instance)
    {
        _instance = instance;
        
        // Collect metrics every 5 minutes
        _timer = new Timer(CollectMetrics, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }

    private void CollectMetrics(object? state)
    {
        try
        {
            var cpuUsage = GetCpuUsage();
            var memoryUsage = GetMemoryUsage();
            var diskUsage = GetDiskUsage();

            _instance.SendMetric("cpu_usage", cpuUsage);
            _instance.SendMetric("memory_usage", memoryUsage);
            _instance.SendMetric("disk_usage", diskUsage);
            _instance.SetStatus("running");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error collecting metrics: {ex.Message}");
            _instance.SetStatus("degraded");
        }
    }

    private double GetCpuUsage() { /* Implementation */ return 0.0; }
    private double GetMemoryUsage() { /* Implementation */ return 0.0; }
    private double GetDiskUsage() { /* Implementation */ return 0.0; }
}
```

---

For more information, see the [README.md](../Replicated/README.md) and [Best Practices](BEST_PRACTICES.md) guide.

