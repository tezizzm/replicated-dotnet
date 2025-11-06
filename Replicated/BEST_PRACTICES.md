# Replicated .NET SDK - Best Practices

This guide provides best practices for using the Replicated .NET SDK effectively and avoiding common pitfalls.

## Table of Contents

- [Client Lifecycle Management](#client-lifecycle-management)
- [Async vs Sync Usage](#async-vs-sync-usage)
- [Error Handling](#error-handling)
- [Performance Recommendations](#performance-recommendations)
- [Security Best Practices](#security-best-practices)
- [State Management](#state-management)
- [Resource Cleanup](#resource-cleanup)
- [Thread Safety](#thread-safety)
- [Testing Strategies](#testing-strategies)
- [Anti-Patterns to Avoid](#anti-patterns-to-avoid)

---

## Client Lifecycle Management

### ✅ DO: Use `await using` for Automatic Disposal

```csharp
// Preferred: Automatic disposal
await using var client = new ReplicatedClient("key", "app");
var customer = await client.Customer.GetOrCreateAsync("user@example.com");
// Client automatically disposed when exiting scope
```

### ✅ DO: Explicitly Dispose in Long-Lived Services

```csharp
public class MetricsService : IDisposable
{
    private readonly ReplicatedClient _client;

    public MetricsService()
    {
        _client = new ReplicatedClient("key", "app");
    frames
    
    public void Dispose()
    {
        _client?.Dispose();
    }
}
```

### ❌ DON'T: Create Clients Without Disposing

```csharp
// Bad: Client never disposed, resources leak
var client = new ReplicatedClient("key", "app");
// ... use client
// Client should be disposed!
```

### ❌ DON'T: Dispose Multiple Times

```csharp
// Bad: Disposing multiple times is safe but unnecessary
client.Dispose();
client.Dispose(); // No need - already disposed
```

---

## Async vs Sync Usage

### ✅ DO: Prefer Async Methods in Async Contexts

```csharp
// Good: Use async methods in async code
public async Task ProcessMetricsAsync()
{
    await using var client = new ReplicatedClient("key", "app");
    var customer = await client.Customer.GetOrCreateAsync("user@example.com");
    await customer.GetOrCreateInstanceAsync().SendMetricAsync("metric", 100);
}
```

### ✅ DO: Use Sync Methods in Synchronous Contexts

```csharp
// Good: Use sync methods when you can't use async
public void ProcessMetrics()
{
    using var client = new ReplicatedClient("key", "app");
    var customer = client.Customer.GetOrCreate("user@example.com霜");
    customer.GetOrCreateInstance().SendMetric("metric", 100);
}
```

### ❌ DON'T: Block Async Methods with `.Wait()` or `.Result`

```csharp
// Bad: Blocks the thread, can cause deadlocks
var customer = client.Customer.GetOrCreateAsync("user@example.com").Result;
```

### ✅ DO: Use `ConfigureAwait(false)` in Library Code

```csharp
// Good for library code: Prevents context capture
var customer = await client.Customer.GetOrCreateAsync("user@example.com")
    .ConfigureAwait(false);
```

---

## Error Handling

### ✅ DO: Handle Specific Exception Types

```csharp
try
{
    var customer = await client.Customer.GetOrCreateAsync("user@example.com");
}
catch (ReplicatedAuthError ex)
{
    // Handle authentication errors specifically
    logger.LogError("Authentication failed: {Message}", ex.Message);
}
catch (ReplicatedRateLimitError ex)
{
    // Handle rate limiting with backoff
    await Task.Delay(TimeSpan.FromMinutes(1));
    // Retry logic
}
catch (ReplicatedNetworkError ex)
{
    // Handle network issues
    logger.LogWarning("Network error: {Message}", ex.Message);
}
catch (ArgumentException ex)
{
    // Handle invalid input
    logger.LogError("Invalid argument: {Message}", ex.Message);
}
```

### ✅ DO: Log Exception Details for Debugging

```csharp
catch (ReplicatedApiError ex)
{
    logger.LogError(ex, 
        "API Error: {Status} {Code} - {Message}. Body: {Body}",
        ex.HttpStatus,
        ex.Code,
        ex.Message,
        ex.HttpBody ?? ex.JsonBody?.ToString() ?? "N/A");
}
```

### ❌ DON'T: Swallow All Exceptions

```csharp
// Bad: Hides errors, makes debugging impossible
try
{
    instance.SendMetric("metric", 100);
}
catch
{
    // What went wrong? Who knows!
}
```

### ✅ DO: Implement Retry Logic for Transient Failures

```csharp
public async Task<bool> SendMetricWithRetryAsync(Instance instance, string name, object value)
{
    const int maxRetries = 3;
    
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            await instance.SendMetricAsync(name, value);
            return true;
        }
        catch (ReplicatedNetworkError) when (attempt < maxRetries - 1)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
        catch (ReplicatedApiError ex) when (ex.HttpStatus == 429 && attempt < maxRetries - 1)
        {
            await Task.Delay(TimeSpan.FromSeconds(60));
        }
    }
    
    return false;
}
```

---

## Performance Recommendations

### ✅ DO: Reuse Client Instances

```csharp
// Good: Reuse client across multiple operations
await using var client = new ReplicatedClient("key", "app");
var customer = await client.Customer.GetOrCreateAsync("user@example.com");
var instance = await customer.GetOrCreateInstanceAsync();

// Send multiple metrics
await instance.SendMetricAsync("metric1", 100);
await instance.SendMetricAsync("metric2", 200);
await instance.SendMetricAsync("metric3", 300);
```

### ❌ DON'T: Create New Clients for Every Operation

```csharp
// Bad: Creates unnecessary overhead
await instance.SendMetricAsync("metric1", 100);

// Bad: Creates a new client for each metric
var client2 = new ReplicatedClient("key", "app");
// ...
client2.Dispose();
```

### ✅ DO: Batch Metrics When Possible

```csharp
// Good: Send multiple metrics in sequence (they're sent together)
var instance = customer.GetOrCreateInstance();
instance.SendMetric("cpu_usage", 0.75);
instance.SendMetric("memory_usage", 0.60);
instance.SendMetric("disk_usage", 0.50);
// All metrics sent in the same request
```

### ✅ DO: Configure Appropriate Timeouts

```csharp
// Good: Set timeouts based on your network conditions
var client = new ReplicatedClient(
    "key",
    "app",
    timeout: TimeSpan.FromSeconds(60) // Longer timeout for slower networks
);
```

---

## Security Best Practices

### ✅ DO: Use Environment Variables for Secrets

```csharp
// Good: Don't hardcode credentials
var client = new ReplicatedClient(
    publishableKey: Environment.GetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY"),
    appSlug: Environment.GetEnvironmentVariable("REPLICATED_APP_SLUG")
);
```

### ❌ DON'T: Hardcode Publishable Keys

```csharp
// Bad: Security risk - keys in source code
var client = new ReplicatedClient(
    publishableKey: "replicated_pk_abc123...", // Never do this!
    appSlug: "my-app"
);
```

### ✅ DO: Use Secure Secret Management

```csharp
// Good: Use your platform's secret management
var client = new ReplicatedClient(
    publishableKey: await secretManager.GetSecretAsync("REPLICATED_PUBLISHABLE_KEY"),
    appSlug: configuration["Replicated:AppSlug"]
);
```

### ✅ DO: Validate Inputs Client-Side

```csharp
// Good: SDK validates inputs, but you can add additional validation
public void SendValidatedMetric(Instance instance, string name, double value)
{
    if (value < 0 || value > 1)
        throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 1");
    
    instance.SendMetric(name, value);
}
```

---

## State Management

### ✅ DO: Understand State Caching Behavior

```csharp
// Good: State is automatically cached
var customer = client.Customer.GetOrCreate("user@example.com");
// Customer ID is cached automatically

// Later, same email uses cached ID
var customer2 = client.Customer.GetOrCreate("user@example.com");
// Uses cached customer ID (no API call if same email)
```

### ✅ DO: Clear State When Switching Customers

```csharp
// Good: Clear state when switching to a different customer
client.StateManager.ClearState();
var newCustomer = client.Customer.GetOrCreate("different@example.com");
```

### ❌ DON'T: Assume State Persists Across Processes

```csharp
// Note: State is persisted to disk, but different processes may have different state directories
// Don't assume state from one process is available to another
```

---

## Resource Cleanup

### ✅ DO: Dispose Clients in Finally Blocks

```csharp
try
{
    var client = new ReplicatedClient("key", "app");
    // ... use client
}
finally
{
    client?.Dispose(); // Always dispose
}
```

### ✅ DO: Use Try-Finally Pattern

```csharp
ReplicatedClient? client = null;
try
{
    client = new ReplicatedClient("key", "app");
    // ... use client
}
finally
{
    client?.Dispose();
}
```

---

## Thread Safety

### ✅ DO: Use One Client Per Thread or Synchronize Access

```csharp
// Good: Each thread has its own client
public class ThreadSafeMetricsCollector
{
    [ThreadStatic]
    private static ReplicatedClient? _client;
    
    private static ReplicatedClient GetClient()
    {
        if (_client == null)
        {
            Dep_client = new ReplicatedClient("key", "app");
        }
        return _client;
    }
}
```

### ✅ DO: Use Lock When Sharing Clients

```csharp
// Good: Synchronize access if sharing a client
private readonly ReplicatedClient _client;
private readonly object _lock = new object();

public void SendMetric(string name, object value)
{
    lock (_lock)
    {
        // Use client safely
        var customer = _client.Customer.GetOrCreate("user@example.com");
        customer.GetOrCreateInstance().SendMetric(name, value);
    }
}
```

### ❌ DON'T: Share synchronized Instances

```csharp
// Note: Instance objects are not thread-safe
// Each thread should create its own instance objects
```

---

## Testing Strategies

### ✅ DO: Mock the Client for Unit Tests

```csharp
// Good: Use a mock or stub for testing
var mockClient = new Mock<IReplicatedClient>();
// Configure mock behavior
var service = new MyService(mockClient.Object);
```

### ✅ DO: Use Dependency Injection for Testability

```csharp
// Good: Inject client for easy testing
public class MetricsService
{
    private readonly IReplicatedClient _client;
    
    public MetricsService(IReplicatedClient client)
    {
        _client = client;
    }
}
```

### ✅ DO: Test Error Scenarios

```csharp
[Fact]
public async Task HandlesNetworkErrors()
{
    var mockClient = new Mock<IReplicatedClient>();
    mockClient.Setup(x => x.Customer.GetOrCreateAsync(It.IsAny<string>()))
        .ThrowsAsync(new ReplicatedNetworkError("Network error"));
    
    // Test error handling
}
```

---

## Anti-Patterns to Avoid

### ❌ DON'T: Create Clients in Loops

```csharp
// Bad: Creates many clients unnecessarily
foreach (var metric in metrics)
{
    var client = new ReplicatedClient("key", "app"); // BAD!
    // ... use client
    client.Dispose();
}
```

### ✅ DO: Create Client Once and Reuse

```csharp
// Good: Create once, reuse many times
await using var client = new ReplicatedClient("key", "app");
foreach (var metric in metrics)
{
    // Reuse client
    var customer = client.Customer.GetOrCreate("user@example.com");
}
```

### ❌ DON'T: Ignore Validation Errors

```csharp
// Bad: Ignores validation, may cause issues later
try
{
    instance.SendMetric("invalid-metric-name!", 100);
}
catch (ArgumentException)
{
    // Swallows validation error
}
```

### ✅ DO: Validate and Handle Errors Appropriately

```csharp
// Good: Handles validation errors
try
{
    instance.SendMetric("metric_name", 100);
}
catch (ArgumentException ex)
{
    logger.LogWarning("Invalid metric name: {Message}", ex.Message);
    // Use a fallback or skip this metric
}
```

### ❌ DON'T: Mix Sync and Async Code Incorrectly

```csharp
// Bad: Sync over async anti-pattern
public void BadMethod()
{
    var customer = client.Customer.GetOrCreateAsync("user@example.com").Result;
}
```

### ✅ DO: Use Async All the Way or Sync All the Way

```csharp
// Good: All async
public async Task GoodMethodAsync()
{
    var customer = await client.Customer.GetOrCreateAsync("user@example.com");
}
```

---

## Summary

- **Always dispose clients** - Use `await using` or explicit `Dispose()`
- **Prefer async methods** - Use async/await instead of blocking calls
- **Handle errors specifically** - Catch specific exception types
- **Reuse clients** - Don't create new clients for each operation
- **Use environment variables** - Never hardcode secrets
- **Understand state management** - Know when state is cached and cleared
- **Test error scenarios** - Don't just test happy paths
- **Follow thread-safety guidelines** - One client per thread or synchronize access

For more examples, see [EXAMPLES.md](EXAMPLES.md).

