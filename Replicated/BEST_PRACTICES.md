# Replicated .NET SDK — Best Practices

Guidelines for using the Replicated .NET SDK effectively in production.

---

## Client lifecycle

### DO: Register as a singleton

`ReplicatedClient` owns a `SocketsHttpHandler` connection pool. Creating a new client per
request defeats connection reuse and exhausts sockets.

```csharp
// In ASP.NET Core — singleton via DI (recommended)
builder.Services.AddReplicatedClient();

// Or for non-DI contexts — create once, reuse for the lifetime of the process
await using var client = new ReplicatedClient();
```

### DO: Use `await using` for scoped lifetimes

```csharp
await using var client = new ReplicatedClient();
var app = await client.App.GetInfoAsync();
// Client disposed automatically on exit
```

### DON'T: Create a client per request or per operation

```csharp
// Bad — creates a new connection pool on every call
foreach (var _ in events)
{
    await using var client = new ReplicatedClient();
    await client.App.SendCustomMetricsAsync(metrics);
}

// Good — create once outside the loop
await using var client = new ReplicatedClient();
foreach (var _ in events)
{
    await client.App.SendCustomMetricsAsync(metrics);
}
```

---

## Async usage

### DO: Use async/await throughout

All service methods are async. Prefer `async/await` over blocking with `.Result` or `.Wait()`.

```csharp
// Good
var app = await client.App.GetInfoAsync();

// Bad — can deadlock in ASP.NET / GUI contexts
var app = client.App.GetInfoAsync().Result;
```

### DO: Pass CancellationToken from your caller

All methods accept an optional `CancellationToken`. Pass it through for proper request
cancellation on timeout or shutdown.

```csharp
public async Task ReportAsync(CancellationToken ct)
{
    var app = await client.App.GetInfoAsync(ct);
    await client.App.SendCustomMetricsAsync(metrics, ct);
}
```

---

## Error handling

### DO: Catch specific exception types

```csharp
try
{
    await client.App.SendCustomMetricsAsync(metrics, ct);
}
catch (ReplicatedAuthError ex)
{
    logger.LogError("Auth failed ({Status}) — check in-cluster service config", ex.HttpStatus);
}
catch (ReplicatedRateLimitError)
{
    // SDK already retries on 429 — if this surfaces, load is unusually high
    logger.LogWarning("Replicated rate limit hit");
}
catch (ReplicatedNetworkError ex)
{
    // In-cluster service unreachable — retried automatically by retry policy
    logger.LogWarning("Replicated network error: {Message}", ex.Message);
}
catch (ReplicatedApiError ex)
{
    logger.LogError("API error {Status} ({Code}): {Message}", ex.HttpStatus, ex.Code, ex.Message);
}
```

### DO: Log `.HttpStatus` and `.Code` for actionable alerts

```csharp
catch (ReplicatedApiError ex)
{
    logger.LogError(ex,
        "Replicated API error {Status} {Code}: {Message}",
        ex.HttpStatus, ex.Code ?? "—", ex.Message);
}
```

### DON'T: Swallow exceptions silently

```csharp
// Bad — hides problems, makes debugging impossible
try { await client.App.SendCustomMetricsAsync(metrics); }
catch { }
```

### DON'T: Implement manual retry — the SDK already does it

The default retry policy handles transient network errors, 429, and 5xx with exponential
backoff. Wrapping calls in your own retry loop doubles the retries.

```csharp
// Bad — unnecessary wrapper
for (var i = 0; i < 3; i++)
{
    try { await client.App.SendCustomMetricsAsync(metrics); break; }
    catch (ReplicatedNetworkError) { await Task.Delay(1000); }
}

// Good — SDK handles this
await client.App.SendCustomMetricsAsync(metrics);
```

---

## Retry configuration

Tune the retry policy when you have strict latency budgets or unusual connectivity:

```csharp
builder.Services.AddReplicatedClient(retryPolicy: new RetryPolicy
{
    MaxRetries = 5,
    InitialDelay = TimeSpan.FromSeconds(2),
    MaxDelay = TimeSpan.FromMinutes(1)
});
```

Disable retries in unit tests so they fail fast:

```csharp
services.AddReplicatedClient(b => b.WithoutRetries());
```

---

## Metrics

### DO: Use `SendCustomMetricsAsync` to replace all metrics, `UpsertCustomMetricsAsync` to update selectively

```csharp
// Replaces ALL metrics for this instance
await client.App.SendCustomMetricsAsync(new Dictionary<string, double>
{
    ["active_users"] = 42,
    ["cpu_usage"] = 0.65
});

// Updates only active_users, leaves others unchanged
await client.App.UpsertCustomMetricsAsync(new Dictionary<string, double>
{
    ["active_users"] = 43
});
```

---

## Testing

### DO: Inject `IReplicatedClient` for easy mocking

```csharp
public class MetricsReporter(IReplicatedClient replicated)
{
    public async Task ReportAsync(CancellationToken ct)
        => await replicated.App.SendCustomMetricsAsync(
               new Dictionary<string, double> { ["users"] = 100 }, ct);
}

// In tests
var mock = new Mock<IReplicatedClient>();
var sut = new MetricsReporter(mock.Object);
```

### DO: Disable retries in tests

```csharp
services.AddReplicatedClient(b => b.WithoutRetries());
```

---

## Summary

| Rule | Why |
|---|---|
| Register as a singleton | Shared connection pool prevents socket exhaustion |
| Pass `CancellationToken` | Enables request cancellation on timeout/shutdown |
| Use `async/await` | Avoids deadlocks; all methods are async-only |
| Catch specific exceptions | `ReplicatedAuthError`, `ReplicatedRateLimitError`, `ReplicatedNetworkError`, `ReplicatedApiError` |
| Don't wrap in manual retry | SDK retry policy (Polly) already handles transient failures |
| Inject `IReplicatedClient` | Enables mocking in unit tests |
