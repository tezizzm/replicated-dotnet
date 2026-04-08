# Replicated .NET SDK — Examples

Code examples for the Replicated .NET SDK. The `Program.cs` in this project demonstrates
common usage patterns against the Replicated in-cluster API.

## Running the example

```bash
# Connects to http://replicated:3000 (requires an in-cluster environment)
dotnet run --project Replicated.Example

# Override the endpoint for local testing
dotnet run --project Replicated.Example -- http://localhost:3000
```

---

## Basic usage

```csharp
using Replicated;

// Connects to the in-cluster Replicated service (http://replicated:3000)
// or REPLICATED_SDK_ENDPOINT if set
await using var client = new ReplicatedClient();

var app = await client.App.GetInfoAsync();
Console.WriteLine($"App: {app.AppName}, Version: {app.CurrentRelease?.VersionLabel}");

var license = await client.License.GetInfoAsync();
Console.WriteLine($"License: {license.LicenseType}, Customer: {license.CustomerName}");
```

---

## App service

```csharp
// App and release info
var app = await client.App.GetInfoAsync();
Console.WriteLine($"Instance: {app.InstanceId}");
Console.WriteLine($"Status:   {app.AppStatus}");
if (app.CurrentRelease != null)
    Console.WriteLine($"Version:  {app.CurrentRelease.VersionLabel} ({app.CurrentRelease.ChannelName})");

// Available updates
var updates = await client.App.GetUpdatesAsync();
Console.WriteLine($"Updates available: {updates.Length}");

// Update history
var history = await client.App.GetHistoryAsync();
foreach (var release in history)
    Console.WriteLine($"  {release.VersionLabel} ({release.CreatedAt})");

// Resource status
var status = await client.App.GetStatusAsync();
Console.WriteLine($"Sequence: {status.Sequence}");

// Custom metrics (replace all)
await client.App.SendCustomMetricsAsync(new Dictionary<string, double>
{
    ["active_users"] = 42,
    ["cpu_usage"] = 0.65,
    ["memory_mb"] = 512
});

// Custom metrics (merge/upsert)
await client.App.UpsertCustomMetricsAsync(new Dictionary<string, double>
{
    ["active_users"] = 43  // updates only this metric
});

// Instance tags
await client.App.SetInstanceTagsAsync(new Dictionary<string, string>
{
    ["environment"] = "production",
    ["region"] = "us-east-1"
});
```

---

## License service

```csharp
// Full license info
var license = await client.License.GetInfoAsync();
Console.WriteLine($"License ID:    {license.LicenseId}");
Console.WriteLine($"License Type:  {license.LicenseType}");
Console.WriteLine($"Customer:      {license.CustomerName}");
Console.WriteLine($"Channel:       {license.ChannelName}");
if (license.Entitlements?.Length > 0)
{
    Console.WriteLine("Entitlements:");
    foreach (var e in license.Entitlements)
        Console.WriteLine($"  {e.Name} = {e.Value}");
}

// All license fields
var fields = await client.License.GetFieldsAsync();
foreach (var f in fields)
    Console.WriteLine($"  {f.Name} = {f.Value}");

// Specific field
var seats = await client.License.GetFieldAsync("num-seats");
Console.WriteLine($"Seats: {seats.Value}");
```

---

## Error handling

```csharp
try
{
    var app = await client.App.GetInfoAsync();
}
catch (ReplicatedAuthError ex)
{
    Console.WriteLine($"Auth error: {ex.Message} (HTTP {ex.HttpStatus})");
}
catch (ReplicatedRateLimitError ex)
{
    Console.WriteLine($"Rate limited: {ex.Message}");
}
catch (ReplicatedNetworkError ex)
{
    // Service not reachable — check that the pod is running in a Replicated-managed cluster
    Console.WriteLine($"Network error: {ex.Message}");
}
catch (ReplicatedApiError ex)
{
    Console.WriteLine($"API error {ex.HttpStatus}: {ex.Message} (code: {ex.Code})");
}
```

---

## ASP.NET Core with DI

```csharp
// Program.cs
builder.Services.AddReplicatedClient();

// In a service or controller
public class MetricsReporter(IReplicatedClient replicated)
{
    public async Task ReportAsync(CancellationToken ct)
    {
        await replicated.App.SendCustomMetricsAsync(
            new Dictionary<string, double> { ["users"] = 100 }, ct);
    }
}
```

---

## Builder pattern

```csharp
var client = new ReplicatedClientBuilder()
    .WithBaseUrl("http://replicated:3000")
    .WithTimeout(TimeSpan.FromSeconds(60))
    .WithRetryPolicy(new RetryPolicy { MaxRetries = 5 })
    .Build();
```

---

## CancellationToken

All async methods accept an optional `CancellationToken`:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var app = await client.App.GetInfoAsync(cts.Token);
```

---

For full API documentation see [Replicated/README.md](../Replicated/README.md).
