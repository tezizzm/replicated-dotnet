# Replicated .NET SDK

A community-maintained .NET SDK for the [Replicated](https://www.replicated.com) in-cluster API.
Use this SDK inside a Kubernetes application managed by Replicated to read app and license
information, report custom metrics, and manage instance tags.

> **Disclaimer:** This is not an official Replicated product and is not affiliated with,
> endorsed by, or supported by Replicated, Inc. It is not covered by any Replicated SLA or
> support agreement. Best-effort support is provided through
> [GitHub Issues](https://github.com/tezizzm/replicated-dotnet/issues).

## Documentation

- **[SDK Documentation](./Replicated/README.md)** — Installation, configuration, and full API reference
- **[Examples](./Replicated.Example/README.md)** — Code examples and usage patterns
- **[Best Practices](./Replicated/BEST_PRACTICES.md)** — Production guidance and anti-patterns
- **[Contributing](./CONTRIBUTING.md)** — How to contribute, run tests, and open issues
- **[Changelog](./CHANGELOG.md)** — Version history and upgrade notes

## Quick Start

### Installation

```bash
dotnet add package Replicated-SDK
```

Or via Package Manager:

```xml
<PackageReference Include="Replicated-SDK" Version="0.1.1" />
```

### Basic Usage

```csharp
using Replicated;

// Connects to http://replicated:3000 (the in-cluster Replicated service)
await using var client = new ReplicatedClient();

// Read app and license info
var app = await client.App.GetInfoAsync();
var license = await client.License.GetInfoAsync();

Console.WriteLine($"App: {app.AppName}, License: {license.LicenseType}");

// Report custom metrics
await client.App.SendCustomMetricsAsync(new Dictionary<string, double>
{
    ["active_users"] = 42,
    ["cpu_usage"] = 0.65
});

// Set instance tags
await client.App.SetInstanceTagsAsync(new Dictionary<string, string>
{
    ["environment"] = "production"
});
```

### ASP.NET Core

```csharp
// Program.cs
builder.Services.AddReplicatedClient();

// Inject IReplicatedClient wherever you need it
```

### Environment Variables

```bash
# Override the in-cluster endpoint (optional — defaults to http://replicated:3000)
export REPLICATED_SDK_ENDPOINT="http://replicated:3000"
export REPLICATED_TIMEOUT="30"  # seconds, optional
```

## Testing

### Unit Tests

```bash
dotnet test Replicated.Tests/
```

### Integration Tests

Integration tests require a running Replicated in-cluster service (or compatible mock) at
`http://localhost:3000` (configurable via `TEST_BASE_URL`):

```bash
TEST_BASE_URL=http://localhost:3000 dotnet test Replicated.IntegrationTests/
```

## Projects

| Project | Description |
|---|---|
| `Replicated` | Main SDK NuGet package |
| `Replicated.Tests` | Unit tests |
| `Replicated.IntegrationTests` | Integration tests |
| `Replicated.Example` | Example application |

## Requirements

- .NET 8.0 (LTS) or .NET 9.0 (STS)
- Application deployed in a Replicated-managed Kubernetes cluster

## Contributing

Bug reports, feature requests, and pull requests are welcome. See [CONTRIBUTING.md](./CONTRIBUTING.md).

## License

MIT
