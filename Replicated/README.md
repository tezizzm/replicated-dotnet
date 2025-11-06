# Replicated .NET SDK

Official .NET SDK for Replicated customer, metrics, and instance insights.

## Installation

Add the package to your project:

```bash
dotnet add package Replicated
```

Or via Package Manager:

```xml
<PackageReference Include="Replicated" Version="0.1.0" />
```

## Quick Start

```csharp
using Replicated;

// Create a client
var client = new ReplicatedClient(
    publishableKey: "replicated_pk_your_key_here",
    appSlug: "my-app"
);

// Get or create a customer
var customer = client.Customer.GetOrCreate("user@example.com");

// Get or create an instance
var instance = customer.GetOrCreateInstance();

// Send metrics
instance.SendMetric("cpu_usage", 0.75);
instance.SendMetric("memory_usage", 0.60);
instance.SendMetric("active_users", 150);

// Set instance status and version
instance.SetStatus("running");
instance.SetVersion("1.0.0");
```

## Asynchronous Usage

```csharp
using Replicated;

await using var client = new ReplicatedClient(
    publishableKey: "replicated_pk_your_key_here",
    appSlug: "my-app"
);

var customer = await client.Customer.GetOrCreateAsync("user@example.com");
var instance = await customer.GetOrCreateInstanceAsync();

await instance.SendMetricAsync("cpu_usage", 0.75);
await instance.SetStatusAsync("running");
await instance.SetVersionAsync("1.0.0");
```

## Configuration

### Constructor Parameters

```csharp
var client = new ReplicatedClient(
    publishableKey: "replicated_pk_your_key_here",
    appSlug: "my-app",
    baseUrl: "https://replicated.app",  // Optional, defaults to https://replicated.app
    timeout: TimeSpan.FromSeconds(30),    // Optional, defaults to 30 seconds
    stateDirectory: "/custom/path",      // Optional, uses platform-specific default
    retryPolicy: customRetryPolicy       // Optional, see Retry Configuration below
);
```

### Fluent Builder Pattern

```csharp
var client = new ReplicatedClientBuilder()
    .WithPublishableKey("replicated_pk_your_key_here")
    .WithAppSlug("my-app")
    .WithBaseUrl("https://custom.replicated.app")
    .WithTimeout(TimeSpan.FromSeconds(60))
    .WithStateDirectory("/custom/state/path")
    .FromEnvironment()  // Merge with environment variables
    .Build();
```

## Retry Configuration

The SDK includes automatic retry logic with exponential backoff to handle transient failures. By default, the SDK will retry 3 times with a 1-second initial delay.

### Default Retry Behavior

- **Max Retries**: 3
- **Initial Delay**: 1 second
- **Max Delay**: 30 seconds
- **Backoff Multiplier**: 2.0 (exponential)
- **Jitter**: Enabled (10% variation to prevent synchronized retries)
- **Retries On**: Network errors, rate limits (429), and server errors (5xx)

### Custom Retry Policy

```csharp
// Using RetryPolicy object
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

var client = new ReplicatedClient(
    publishableKey: "replicated_pk_your_key_here",
    appSlug: "my-app",
    retryPolicy: retryPolicy
);
```

### Builder Pattern with Retry Configuration

```csharp
// Simple retry configuration
var client = new ReplicatedClientBuilder()
    .WithPublishableKey("replicated_pk_your_key_here")
    .WithAppSlug("my-app")
    .WithRetryPolicy(
        maxRetries: 5,
        initialDelay: TimeSpan.FromSeconds(2),
        maxDelay: TimeSpan.FromMinutes(2),
        backoffMultiplier: 1.5
    )
    .Build();

// Disable retries
var client = new ReplicatedClientBuilder()
    .WithPublishableKey("replicated_pk_your_key_here")
    .WithAppSlug("my-app")
    .WithoutRetries()
    .Build();

// Full configuration
var retryPolicy = new RetryPolicy
{
    MaxRetries = 10,
    InitialDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromMinutes(2),
    BackoffMultiplier = 1.5,
    RetryOnRateLimit = true,
    RetryOnServerError = true
};

var client = new ReplicatedClientBuilder()
    .WithPublishableKey("replicated_pk_your_key_here")
    .WithAppSlug("my-app")
    .WithRetryPolicy(retryPolicy)
    .Build();
```

### Custom Retry Logic

```csharp
var retryPolicy = new RetryPolicy
{
    MaxRetries = 3,
    ShouldRetry = (exception, attemptNumber) =>
    {
        // Custom logic: Only retry network errors, not API errors
        if (exception is ReplicatedNetworkError)
            return true;
        
        // Don't retry after 2 attempts
        if (attemptNumber >= 2)
            return false;
        
        return false;
    }
};

var client = new ReplicatedClient(
    publishableKey: "replicated_pk_your_key_here",
    appSlug: "my-app",
    retryPolicy: retryPolicy
);
```

## Configuration via Environment Variables

The SDK supports configuration via environment variables, with constructor parameters taking precedence:

### Basic Configuration

```bash
export REPLICATED_PUBLISHABLE_KEY="replicated_pk_your_key_here"
export REPLICATED_APP_SLUG="my-app"
export REPLICATED_BASE_URL="https://replicated.app"  # Optional
export REPLICATED_TIMEOUT="30"  # Optional, in seconds
export REPLICATED_STATE_DIRECTORY="/custom/path"  # Optional
```

### Retry Configuration via Environment Variables

```bash
export REPLICATED_MAX_RETRIES="5"
export REPLICATED_RETRY_INITIAL_DELAY="2000"  # milliseconds
export REPLICATED_RETRY_MAX_DELAY="120000"     # milliseconds
export REPLICATED_RETRY_BACKOFF_MULTIPLIER="1.5"
export REPLICATED_RETRY_USE_JITTER="true"
export REPLICATED_RETRY_JITTER_PERCENTAGE="0.15"
export REPLICATED_RETRY_ON_RATE_LIMIT="true"
export REPLICATED_RETRY_ON_SERVER_ERROR="true"
export REPLICATED_RETRY_ON_NETWORK_ERROR="true"
```

### Using Environment Variables

```csharp
// With FromEnvironment(), reads all configuration from environment
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .Build();

// Environment variables are used as fallback when parameters are not provided
var client = new ReplicatedClient();  // Reads from environment

// Explicit parameters override environment variables
var client = new ReplicatedClient(
    publishableKey: "override_key",  // Takes precedence over REPLICATED_PUBLISHABLE_KEY
    appSlug: "override-app"          // Takes precedence over REPLICATED_APP_SLUG
);
```

## Configuration Precedence

The SDK uses the following precedence order (highest to lowest):

1. **Explicit constructor parameters**
2. **Environment variables**
3. **Default values**

For example, if both a constructor parameter and an environment variable are set, the constructor parameter is used.

## Error Handling

```csharp
using Replicated;

try
{
    var customer = client.Customer.GetOrCreate("user@example.com");
}
catch (ReplicatedAuthError ex)
{
    // Authentication failed - check your publishable key
    Console.WriteLine($"Auth error: {ex.Message} (HTTP {ex.HttpStatus})");
}
catch (ReplicatedRateLimitError ex)
{
    // Rate limit exceeded - will retry automatically if configured
    Console.WriteLine($"Rate limit: {ex.Message}");
}
catch (ReplicatedNetworkError ex)
{
    // Network error - will retry automatically if configured
    Console.WriteLine($"Network error: {ex.Message}");
}
catch (ReplicatedApiError ex)
{
    // API error (4xx or 5xx)
    Console.WriteLine($"API error: {ex.Message} (HTTP {ex.HttpStatus})");
}
catch (ReplicatedException ex)
{
    // Any other Replicated SDK error
    Console.WriteLine($"Error: {ex.Message}");
}
```

## State Management

The SDK automatically manages state (customer ID, instance ID, dynamic tokens) in platform-specific directories:

- **macOS**: `~/Library/Application Support/Replicated/<app_slug>`
- **Linux**: `${XDG_STATE_HOME:-~/.local/state}/replicated/<app_slug>`
- **Windows**: `%APPDATA%\Replicated\<app_slug>`

You can override the state directory:

```csharp
var client = new ReplicatedClient(
    publishableKey: "replicated_pk_your_key_here",
    appSlug: "my-app",
    stateDirectory: "/custom/path/to/state"
);
```

## Features

- ✅ **Automatic Retry Logic**: Configurable retry with exponential backoff and jitter
- ✅ **Input Validation**: Comprehensive validation for all API parameters
- ✅ **Environment Variable Support**: Flexible configuration via environment variables
- ✅ **Fluent Builder Pattern**: Easy-to-use builder for client configuration
- ✅ **Sync & Async Support**: Both synchronous and asynchronous methods available
- ✅ **Cross-Platform**: Works on .NET 8.0+ and .NET 9.0+ (Windows, macOS, Linux)
- ✅ **State Management**: Automatic caching of customer and instance IDs
- ✅ **Error Handling**: Rich exception types for different error scenarios

## Requirements

- .NET 8.0 (LTS) or .NET 9.0 (STS)

## Troubleshooting

### Common Issues

#### "Publishable key is required" Error
This error occurs when no publishable key is provided. Solutions:
- Set the `REPLICATED_PUBLISHABLE_KEY` environment variable
- Provide the key via constructor: `new ReplicatedClient("your_key", "app")`
- Use the builder pattern: `builder.WithPublishableKey("your_key")`

#### "App slug is required" Error
Similar to above, provide the app slug via:
- Environment variable: `REPLICATED_APP_SLUG`
- Constructor parameter
- Builder pattern

#### Authentication Errors (401/403)
Check that:
- Your publishable key is valid and not expired
- The key has necessary permissions
- You're using the correct API endpoint (base URL)

#### Network Errors
- Verify network connectivity
- Check firewall/proxy settings
- Ensure the API endpoint is accessible
- Consider increasing timeout: `WithTimeout(TimeSpan.FromSeconds(60))`

#### Rate Limit Errors (429)
- Implement exponential backoff
- Reduce request frequency
- Enable retry policy: `WithRetryPolicy(retryOnRateLimit: true)`

## FAQ

### Can I use this SDK in multiple threads?
Yes, but each thread should use its own `ReplicatedClient` instance, or you should synchronize access if sharing a client. Instance objects are not thread-safe.

### How do I handle errors?
The SDK throws specific exception types (`ReplicatedApiError`, `ReplicatedAuthError`, etc.). Catch these and handle appropriately. See [Best Practices](docs/BEST_PRACTICES.md#error-handling) for examples.

### Where is state stored?
State is stored in platform-specific directories:
- **Windows**: `%APPDATA%\Replicated\<app_slug>`
- **macOS**: `~/Library/Application Support/Replicated/<app_slug>`
- **Linux**: `${XDG_STATE_HOME:-~/.local/state}/replicated/<app_slug>`

You can customize this with the `stateDirectory` parameter.

### Can I disable retries?
Yes, use the builder: `builder.WithoutRetries()` or set `RetryPolicy.MaxRetries = 0`.

### Is the SDK async?
Yes! The SDK supports both synchronous and asynchronous operations. Use `*Async` methods with `async/await` for best performance.

### How do I configure retry behavior?
Use the `RetryPolicy` class or builder methods:
```csharp
var client = new ReplicatedClientBuilder()
    .WithPublishableKey("key")
    .WithAppSlug("app")
    .WithRetryPolicy(
        maxRetries: 5,
        initialDelay: TimeSpan.FromSeconds(2),
        maxDelay: TimeSpan.FromMinutes(2)
    )
    .Build();
```

### Can I use this in ASP.NET Core?
Yes! Register the client as a singleton in your dependency injection container:
```csharp
services.AddSingleton<ReplicatedClient>(provider =>
    new ReplicatedClient(
        Environment.GetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY"),
        Environment.GetEnvironmentVariable("REPLICATED_APP_SLUG")
    ));
```

## Documentation

- [Usage Examples](docs/EXAMPLES.md) - Comprehensive code examples
- [Best Practices](docs/BEST_PRACTICES.md) - Recommended patterns and anti-patterns

## License

MIT

