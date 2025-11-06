using Replicated;

namespace Replicated.Example;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Replicated .NET SDK Example");
        Console.WriteLine("==========================");

        // Get configuration from command line arguments or environment variables
        var publishableKey = GetArgOrEnv(args, 0, "REPLICATED_PUBLISHABLE_KEY") ?? "replicated_pk_test";
        var appSlug = GetArgOrEnv(args, 1, "REPLICATED_APP_SLUG") ?? "my-app";
        var customerEmail = GetArgOrEnv(args, 2, "REPLICATED_CUSTOMER_EMAIL") ?? "user@example.com";
        var baseUrl = GetArgOrEnv(args, 3, "REPLICATED_BASE_URL") ?? "https://replicated.app";

        Console.WriteLine($"Publishable Key: {publishableKey}");
        Console.WriteLine($"App Slug: {appSlug}");
        Console.WriteLine($"Customer Email: {customerEmail}");
        Console.WriteLine($"Base URL: {baseUrl}");
        Console.WriteLine();

        // Example 1: Synchronous usage
        Console.WriteLine("1. Synchronous Example");
        Console.WriteLine("---------------------");
        await RunSyncExample(publishableKey, appSlug, customerEmail, baseUrl);

        Console.WriteLine();

        // Example 2: Asynchronous usage
        Console.WriteLine("2. Asynchronous Example");
        Console.WriteLine("----------------------");
        await RunAsyncExample(publishableKey, appSlug, customerEmail, baseUrl);

        Console.WriteLine();
        Console.WriteLine("Examples completed successfully!");
    }

    static async Task RunSyncExample(string publishableKey, string appSlug, string customerEmail, string baseUrl)
    {
        using var client = new ReplicatedClient(
            publishableKey: publishableKey,
            appSlug: appSlug,
            baseUrl: baseUrl
        );

        Console.WriteLine("✓ Replicated client initialized successfully");

        // Create or get customer
        Console.WriteLine($"Creating/getting customer with email: {customerEmail}");
        var customer = client.Customer.GetOrCreate(emailAddress: customerEmail);
        Console.WriteLine($"✓ Customer created/retrieved - ID: {customer.CustomerId}");

        // Create or get instance
        Console.WriteLine("Creating/getting instance for customer...");
        var instance = customer.GetOrCreateInstance();
        Console.WriteLine($"✓ Instance created/retrieved - ID: {instance.InstanceId}");

        // Set instance status
        instance.SetStatus("running");
        Console.WriteLine("✓ Instance status set to: running");

        // Set instance version
        instance.SetVersion("1.0.0");
        Console.WriteLine("✓ Instance version set to: 1.0.0");

        // Send some metrics
        instance.SendMetric("cpu_usage", 0.75);
        instance.SendMetric("memory_usage", 0.60);
        instance.SendMetric("active_users", 150);
        Console.WriteLine("✓ Metrics sent successfully");
    }

    static async Task RunAsyncExample(string publishableKey, string appSlug, string customerEmail, string baseUrl)
    {
        await using var client = new ReplicatedClient(
            publishableKey: publishableKey,
            appSlug: appSlug,
            baseUrl: baseUrl
        );

        Console.WriteLine("✓ Replicated client initialized successfully (async)");

        // Create or get customer
        Console.WriteLine($"Creating/getting customer with email: {customerEmail}");
        var customer = await client.Customer.GetOrCreateAsync(emailAddress: customerEmail);
        Console.WriteLine($"✓ Customer created/retrieved - ID: {customer.CustomerId}");

        // Create or get instance
        Console.WriteLine("Creating/getting instance for customer...");
        var instance = await customer.GetOrCreateInstanceAsync();
        Console.WriteLine($"✓ Instance created/retrieved - ID: {instance.InstanceId}");

        // Set instance status
        await instance.SetStatusAsync("running");
        Console.WriteLine("✓ Instance status set to: running");

        // Set instance version
        await instance.SetVersionAsync("1.0.0");
        Console.WriteLine("✓ Instance version set to: 1.0.0");

        // Send some metrics
        await instance.SendMetricAsync("cpu_usage", 0.85);
        await instance.SendMetricAsync("memory_usage", 0.70);
        await instance.SendMetricAsync("active_users", 200);
        Console.WriteLine("✓ Metrics sent successfully");
    }

    static string? GetArgOrEnv(string[] args, int index, string envVar)
    {
        if (args.Length > index && !string.IsNullOrEmpty(args[index]))
        {
            return args[index];
        }
        return Environment.GetEnvironmentVariable(envVar);
    }
}

