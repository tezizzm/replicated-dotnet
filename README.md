# Replicated .NET SDK

Official .NET SDK for Replicated customer, metrics, and instance insights.

## 📚 Documentation

Documentation is organized by project:

- **[Replicated SDK Documentation](./Replicated/README.md)** - Main SDK documentation, installation, and usage
- **[Examples](./Replicated.Example/README.md)** - Code examples and usage patterns
- **[Best Practices](./Replicated/BEST_PRACTICES.md)** - Development guidelines and recommendations
- **[Production Readiness](./Replicated/PRODUCTION_READINESS_CHECKLIST.md)** - Production deployment checklist
- **[Production Recommendations](./Replicated/PRODUCTION_RECOMMENDATIONS.md)** - Production deployment guidelines
- **[Integration Tests](./Replicated.IntegrationTests/README.md)** - Integration testing setup and usage
- **[Mock Server](./Replicated.IntegrationTests.MockServer/README.md)** - Mock server for testing
- **[Test Coverage Report](./Replicated.Tests/TEST_COVERAGE_REPORT.md)** - Test coverage analysis

## 🚀 Quick Start

### Installation

```bash
dotnet add package Replicated
```

Or via Package Manager:

```xml
<PackageReference Include="Replicated" Version="0.1.0" />
```

### Basic Usage

```csharp
using Replicated;

// Configure via environment variables (recommended)
// Set REPLICATED_PUBLISHABLE_KEY and REPLICATED_APP_SLUG
var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .Build();

// Get or create a customer
var customer = client.Customer.GetOrCreate("user@example.com");

// Get or create an instance
var instance = customer.GetOrCreateInstance();

// Send metrics
instance.SendMetric("cpu_usage", 0.75);
instance.SendMetric("memory_usage", 0.60);

// Set instance status and version
instance.SetStatus("running");
instance.SetVersion("1.0.0");
```

### Asynchronous Usage

```csharp
using Replicated;

await using var client = new ReplicatedClientBuilder()
    .FromEnvironment()
    .Build();

var customer = await client.Customer.GetOrCreateAsync("user@example.com");
var instance = await customer.GetOrCreateInstanceAsync();

await instance.SendMetricAsync("cpu_usage", 0.75);
await instance.SetStatusAsync("running");
await instance.SetVersionAsync("1.0.0");
```

### Environment Variables

```bash
export REPLICATED_PUBLISHABLE_KEY="replicated_pk_your_key_here"
export REPLICATED_APP_SLUG="my-app"
export REPLICATED_BASE_URL="https://replicated.app"  # Optional
export REPLICATED_TIMEOUT="30"  # Optional, in seconds
```

## 🧪 Testing

### Unit Tests

```bash
dotnet test Replicated.Tests/
```

### Integration Tests

```bash
# Start mock server
cd Replicated.IntegrationTests.MockServer
dotnet run

# Run integration tests (in another terminal)
cd Replicated.IntegrationTests
dotnet test
```

## 📦 Projects

- **Replicated** - Main SDK package
- **Replicated.Example** - Example application demonstrating SDK usage
- **Replicated.Tests** - Unit tests
- **Replicated.IntegrationTests** - Integration tests
- **Replicated.IntegrationTests.MockServer** - Mock server for testing

## 🏗️ Project Structure

```
├── Replicated/                           # Main SDK
│   ├── README.md                         # SDK documentation
│   ├── BEST_PRACTICES.md                 # Development guidelines
│   ├── PRODUCTION_READINESS_CHECKLIST.md # Production checklist
│   └── ...
├── Replicated.Example/                   # Example application
│   ├── README.md                         # Usage examples
│   └── ...
├── Replicated.Tests/                     # Unit tests
│   ├── TEST_COVERAGE_REPORT.md           # Coverage report
│   └── ...
├── Replicated.IntegrationTests/          # Integration tests
│   ├── README.md                         # Integration test docs
│   └── ...
├── Replicated.IntegrationTests.MockServer/ # Mock server
│   ├── README.md                         # Mock server docs
│   └── ...
└── Replicated.sln                        # Solution file
```

## 🔧 Requirements

- .NET 6.0 or later (.NET 8.0+ recommended)
- Windows, macOS, or Linux
- Valid Replicated publishable key

## 📄 License

This project is part of the Replicated platform ecosystem.

## 🤝 Contributing

See the [Best Practices](./Replicated/BEST_PRACTICES.md) documentation for development guidelines.
