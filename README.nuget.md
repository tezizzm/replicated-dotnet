# Replicated .NET SDK

A community-maintained .NET SDK for the [Replicated](https://www.replicated.com) in-cluster API.

Use this SDK inside a Kubernetes application managed by Replicated to read app and license
information, report custom metrics, and manage instance tags. Connects to the Replicated
in-cluster service at `http://replicated:3000` — no authentication required.

> **Disclaimer:** This is not an official Replicated product and is not affiliated with,
> endorsed by, or supported by Replicated, Inc. It is not covered by any Replicated SLA or
> support agreement. Best-effort support is provided through GitHub Issues.

Targets **net8.0** and **net9.0**.

---

## Installation

```bash
dotnet add package Replicated
```

---

## Quick Start

```csharp
using Replicated;

// Connects to the Replicated in-cluster service (http://replicated:3000)
await using var client = new ReplicatedClient();

// Read app and license info
var app = await client.App.GetInfoAsync();
var license = await client.License.GetInfoAsync();
Console.WriteLine($"App: {app.AppName}, License: {license.LicenseType}");

// Report custom metrics
await client.App.SendCustomMetricsAsync(new Dictionary<string, double>
{
    ["active_users"] = 42,
    ["memory_mb"] = 512
});

// Set instance tags
await client.App.SetInstanceTagsAsync(new Dictionary<string, string>
{
    ["environment"] = "production"
});
```

---

## ASP.NET Core DI

```csharp
// Program.cs — registers IReplicatedClient as a singleton
builder.Services.AddReplicatedClient();

// Inject IReplicatedClient wherever you need it
```

---

## Error Handling

```csharp
try
{
    var app = await client.App.GetInfoAsync();
}
catch (ReplicatedAuthError ex)      { /* 401 / 403 */ }
catch (ReplicatedRateLimitError ex)  { /* 429 — retried automatically */ }
catch (ReplicatedNetworkError ex)    { /* connectivity issue — retried automatically */ }
catch (ReplicatedApiError ex)        { /* other 4xx / 5xx */ }
```

All exceptions expose `.HttpStatus` (int) and `.Code` (string, when the API returns one).

---

## Configuration

Automatic retry with exponential backoff and jitter is enabled by default (3 retries).

| Environment variable | Description | Default |
|---|---|---|
| `REPLICATED_SDK_ENDPOINT` | Base URL of the in-cluster service | `http://replicated:3000` |
| `REPLICATED_TIMEOUT` | Request timeout in seconds | `30` |
| `REPLICATED_MAX_RETRIES` | Maximum retry attempts | `3` |
| `REPLICATED_RETRY_INITIAL_DELAY` | Initial retry delay in milliseconds | `1000` |
| `REPLICATED_RETRY_MAX_DELAY` | Maximum retry delay in milliseconds | `30000` |
| `REPLICATED_RETRY_BACKOFF_MULTIPLIER` | Exponential backoff multiplier | `2.0` |

---

## Links

- [GitHub repository](https://github.com/tezizzm/replicated-dotnet)
- [Full documentation](https://github.com/tezizzm/replicated-dotnet/blob/main/Replicated/README.md)
- [Changelog](https://github.com/tezizzm/replicated-dotnet/blob/main/CHANGELOG.md)
- [License: MIT](https://github.com/tezizzm/replicated-dotnet/blob/main/LICENSE)
