# Replicated .NET SDK

A community-maintained .NET SDK for the Replicated in-cluster API.

> **Disclaimer:** This is not an official Replicated product and is not affiliated with,
> endorsed by, or supported by Replicated, Inc. It is not covered by any Replicated SLA or
> support agreement. Best-effort support is provided through
> [GitHub Issues](https://github.com/tezizzm/replicated-dotnet/issues).

The Replicated in-cluster service runs alongside your application inside a Kubernetes cluster
managed by Replicated. This SDK connects to it at `http://replicated:3000` (no authentication
required) and exposes app info, license data, custom metrics, and instance tags.

## Installation

```bash
dotnet add package Replicated-SDK
```

```xml
<PackageReference Include="Replicated-SDK" Version="0.1.1" />
```

## Quick Start

```csharp
using Replicated;

await using var client = new ReplicatedClient();

// App info
var app = await client.App.GetInfoAsync();
Console.WriteLine($"{app.AppName} v{app.CurrentRelease?.VersionLabel}");

// License info
var license = await client.License.GetInfoAsync();
Console.WriteLine($"License: {license.LicenseType}, Customer: {license.CustomerName}");

// Custom metrics
await client.App.SendCustomMetricsAsync(new Dictionary<string, double>
{
    ["active_users"] = 42,
    ["memory_mb"] = 512
});

// Instance tags
await client.App.SetInstanceTagsAsync(new Dictionary<string, string>
{
    ["environment"] = "production",
    ["region"] = "us-east-1"
});
```

## Configuration

### Constructor Parameters

```csharp
var client = new ReplicatedClient(
    baseUrl: "http://replicated:3000",   // optional — see env vars below
    timeout: TimeSpan.FromSeconds(30),   // optional — default 30s
    retryPolicy: customRetryPolicy,      // optional — see Retry Configuration
    logger: loggerInstance               // optional — ILogger for request/retry logging
);
```

### Fluent Builder

```csharp
var client = new ReplicatedClientBuilder()
    .WithBaseUrl("http://replicated:3000")
    .WithTimeout(TimeSpan.FromSeconds(60))
    .WithRetryPolicy(new RetryPolicy { MaxRetries = 5 })
    .WithLogger(logger)
    .Build();
```

### Environment Variables

The constructor reads these environment variables when the corresponding parameter is not provided:

| Variable | Description | Default |
|---|---|---|
| `REPLICATED_SDK_ENDPOINT` | Base URL of the in-cluster service | `http://replicated:3000` |
| `REPLICATED_TIMEOUT` | Request timeout in seconds | `30` |
| `REPLICATED_MAX_RETRIES` | Maximum retry attempts | `3` |
| `REPLICATED_RETRY_INITIAL_DELAY` | Initial retry delay in milliseconds | `1000` |
| `REPLICATED_RETRY_MAX_DELAY` | Maximum retry delay in milliseconds | `30000` |
| `REPLICATED_RETRY_BACKOFF_MULTIPLIER` | Exponential backoff multiplier | `2.0` |
| `REPLICATED_RETRY_USE_JITTER` | Enable jitter | `true` |
| `REPLICATED_RETRY_ON_RATE_LIMIT` | Retry on 429 | `true` |
| `REPLICATED_RETRY_ON_SERVER_ERROR` | Retry on 5xx | `true` |
| `REPLICATED_RETRY_ON_NETWORK_ERROR` | Retry on network errors | `true` |

## App Service (`client.App`)

| Method | Description |
|---|---|
| `GetInfoAsync(ct)` | App metadata: name, slug, status, current release |
| `GetStatusAsync(ct)` | Current application resource status |
| `GetUpdatesAsync(ct)` | Available releases for upgrade |
| `GetHistoryAsync(ct)` | Previously installed releases |
| `SendCustomMetricsAsync(metrics, ct)` | Replace all custom metrics |
| `UpsertCustomMetricsAsync(metrics, ct)` | Merge (upsert) custom metrics without replacing others |
| `DeleteCustomMetricAsync(name, ct)` | Delete a single custom metric |
| `SetInstanceTagsAsync(tags, ct)` | Set `Dictionary<string, string>` instance tags |

## License Service (`client.License`)

| Method | Description |
|---|---|
| `GetInfoAsync(ct)` | License metadata: type, customer name, channel, entitlements |
| `GetFieldAsync(name, ct)` | Read a single license field by name |
| `GetEntitlementsAsync(ct)` | List all license entitlements |

## Error Handling

```csharp
try
{
    var app = await client.App.GetInfoAsync();
}
catch (ReplicatedAuthError ex)
{
    // 401 / 403 — check in-cluster service configuration
    Console.WriteLine($"Auth error: {ex.Message} (HTTP {ex.HttpStatus})");
}
catch (ReplicatedRateLimitError ex)
{
    // 429 — automatically retried if retry policy is enabled
    Console.WriteLine($"Rate limited: {ex.Message}");
}
catch (ReplicatedNetworkError ex)
{
    // Transport failure — automatically retried if retry policy is enabled
    Console.WriteLine($"Network error: {ex.Message}");
}
catch (ReplicatedApiError ex)
{
    // Any other 4xx / 5xx
    Console.WriteLine($"API error {ex.HttpStatus}: {ex.Message} (code: {ex.Code})");
}
```

All exceptions expose `.HttpStatus` (int), `.Message` (string), and `.Code` (string, when the
API returns one).

## Retry Configuration

Automatic retry with exponential backoff and jitter is enabled by default (3 retries).

```csharp
var retryPolicy = new RetryPolicy
{
    MaxRetries = 5,
    InitialDelay = TimeSpan.FromSeconds(2),
    MaxDelay = TimeSpan.FromMinutes(2),
    BackoffMultiplier = 1.5,
    UseJitter = true,
    JitterPercentage = 0.15,
    RetryOnRateLimit = true,
    RetryOnServerError = true,
    RetryOnNetworkError = true
};

var client = new ReplicatedClient(retryPolicy: retryPolicy);
```

To disable retries:

```csharp
var client = new ReplicatedClientBuilder()
    .WithoutRetries()
    .Build();
```

Custom retry predicate:

```csharp
var retryPolicy = new RetryPolicy
{
    MaxRetries = 3,
    ShouldRetry = (exception, attemptNumber) =>
        exception is ReplicatedNetworkError && attemptNumber < 2
};
```

## ASP.NET Core / Dependency Injection

```csharp
// Default — reads REPLICATED_SDK_ENDPOINT from environment
builder.Services.AddReplicatedClient();

// With custom base URL
builder.Services.AddReplicatedClient(baseUrl: "http://replicated:3000");

// With retry policy
builder.Services.AddReplicatedClient(retryPolicy: new RetryPolicy { MaxRetries = 5 });

// Builder delegate — full control
builder.Services.AddReplicatedClient(b => b
    .WithTimeout(TimeSpan.FromSeconds(60))
    .WithRetryPolicy(new RetryPolicy { MaxRetries = 5 }));
```

Inject `IReplicatedClient` wherever you need it:

```csharp
public class MyService(IReplicatedClient replicated)
{
    public async Task ReportMetrics(CancellationToken ct)
    {
        await replicated.App.SendCustomMetricsAsync(
            new Dictionary<string, double> { ["users"] = 100 }, ct);
    }
}
```

## Thread Safety

`ReplicatedClient` is safe to use from multiple threads. Register it as a singleton in DI or
share a single instance across your application. Each request uses an independent HTTP message.

## Requirements

- .NET 8.0 (LTS) or .NET 9.0 (STS)
- Application deployed in a Replicated-managed Kubernetes cluster

## Troubleshooting

### Network Errors at Startup

If you see `ReplicatedNetworkError` immediately, check that:
- The pod is running inside a Replicated-managed cluster
- The `replicated` Kubernetes service is reachable (`http://replicated:3000`)
- `REPLICATED_SDK_ENDPOINT` is set correctly if using a non-default endpoint

### Timeouts

Increase the timeout for slow environments:

```csharp
var client = new ReplicatedClient(timeout: TimeSpan.FromSeconds(60));
```

### Rate Limit Errors (429)

The default retry policy handles 429s automatically. To tune the backoff:

```csharp
var client = new ReplicatedClient(retryPolicy: new RetryPolicy
{
    MaxRetries = 5,
    InitialDelay = TimeSpan.FromSeconds(5)
});
```

## License

MIT
