using System.Collections.Generic;
using Replicated;

namespace Replicated.Example;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Replicated .NET SDK Example");
        Console.WriteLine("==========================");
        Console.WriteLine("Connects to the Replicated in-cluster service (http://replicated:3000)");
        Console.WriteLine();

        // Override base URL from environment or argument (useful for local testing)
        var baseUrl = args.Length > 0 ? args[0] : null;

        await using var client = new ReplicatedClient(baseUrl: baseUrl);

        Console.WriteLine($"Base URL: {client.BaseUrl}");
        Console.WriteLine();

        // Example 1: App info
        Console.WriteLine("1. App Info");
        Console.WriteLine("----------");
        try
        {
            var appInfo = await client.App.GetInfoAsync();
            Console.WriteLine($"Instance ID:  {appInfo.InstanceId}");
            Console.WriteLine($"App Slug:     {appInfo.AppSlug}");
            Console.WriteLine($"App Name:     {appInfo.AppName}");
            Console.WriteLine($"App Status:   {appInfo.AppStatus}");
            if (appInfo.CurrentRelease != null)
                Console.WriteLine($"Version:      {appInfo.CurrentRelease.VersionLabel} ({appInfo.CurrentRelease.ChannelName})");
        }
        catch (ReplicatedApiError ex)
        {
            Console.WriteLine($"  Error: {ex.Message} (HTTP {ex.HttpStatus})");
        }

        Console.WriteLine();

        // Example 2: License info
        Console.WriteLine("2. License Info");
        Console.WriteLine("--------------");
        try
        {
            var license = await client.License.GetInfoAsync();
            Console.WriteLine($"License ID:     {license.LicenseId}");
            Console.WriteLine($"License Type:   {license.LicenseType}");
            Console.WriteLine($"Customer Name:  {license.CustomerName}");
            Console.WriteLine($"Channel:        {license.ChannelName}");
            if (license.Entitlements?.Length > 0)
            {
                Console.WriteLine("Entitlements:");
                foreach (var e in license.Entitlements)
                    Console.WriteLine($"  {e.Name} = {e.Value}");
            }
        }
        catch (ReplicatedApiError ex)
        {
            Console.WriteLine($"  Error: {ex.Message} (HTTP {ex.HttpStatus})");
        }

        Console.WriteLine();

        // Example 3: Custom metrics
        Console.WriteLine("3. Custom Metrics");
        Console.WriteLine("----------------");
        try
        {
            await client.App.SendCustomMetricsAsync(new Dictionary<string, double>
            {
                ["active_users"] = 42,
                ["cpu_usage"] = 0.65,
                ["memory_mb"] = 512
            });
            Console.WriteLine("  Metrics sent successfully.");
        }
        catch (ReplicatedApiError ex)
        {
            Console.WriteLine($"  Error: {ex.Message} (HTTP {ex.HttpStatus})");
        }

        Console.WriteLine();

        // Example 4: Instance tags
        Console.WriteLine("4. Instance Tags");
        Console.WriteLine("---------------");
        try
        {
            await client.App.SetInstanceTagsAsync(new Dictionary<string, string>
            {
                ["environment"] = "production",
                ["region"] = "us-east-1"
            });
            Console.WriteLine("  Tags updated successfully.");
        }
        catch (ReplicatedApiError ex)
        {
            Console.WriteLine($"  Error: {ex.Message} (HTTP {ex.HttpStatus})");
        }

        Console.WriteLine();
        Console.WriteLine("Example completed.");
    }
}
