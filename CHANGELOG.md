# Changelog

All notable changes to the Replicated .NET SDK will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2025-11-06

### Added

#### Core Features
- **Customer Management**: Create and retrieve customer installations with email, channel, and name support
- **Instance Management**: Create and manage application instances with automatic ID caching
- **Metrics Tracking**: Send custom application metrics (numeric values) to Replicated
- **Instance Status & Version**: Set and track instance status (running, stopped, etc.) and version information
- **State Persistence**: Automatic caching of customer IDs and instance IDs to disk for improved performance

#### Configuration & Setup
- **Fluent Builder Pattern**: Easy-to-use `ReplicatedClientBuilder` for configuring clients
- **Environment Variable Support**: Full configuration via environment variables (`REPLICATED_PUBLISHABLE_KEY`, `REPLICATED_APP_SLUG`, etc.)
- **Constructor-based Configuration**: Direct constructor initialization with optional parameters
- **Platform-specific State Directories**: Automatic state directory selection based on operating system
- **Custom State Directory**: Override default state directory location

#### Retry & Resilience
- **Automatic Retry Logic**: Configurable retry policies with exponential backoff
- **Jitter Support**: Randomized delay to prevent synchronized retries
- **Configurable Retry Conditions**: Control retries for network errors, rate limits, and server errors
- **Polly Integration**: Built on Polly for robust retry handling

#### Error Handling
- **Specific Exception Types**: 
  - `ReplicatedApiError` for API errors (4xx, 5xx)
  - `ReplicatedAuthError` for authentication failures (401, 403)
  - `ReplicatedRateLimitError` for rate limit violations (429)
  - `ReplicatedNetworkError` for network connectivity issues
- **Rich Error Information**: HTTP status codes, error messages, JSON response bodies, and headers included in exceptions
- **Comprehensive Input Validation**: Validation for all API parameters with clear error messages

#### Asynchronous Support
- **Full Async/Await Support**: All operations available in both synchronous and asynchronous forms
- **IAsyncDisposable Support**: Proper async disposal for resource cleanup
- **Task-based Async Pattern**: Standard .NET async patterns throughout

#### Cross-Platform Support
- **Multi-Targeting**: Supports .NET 8.0 (LTS) and .NET 9.0 (STS)
- **Cross-Platform**: Works on Windows, macOS, and Linux
- **Platform-Specific Features**: Automatic OS detection for state directory management

#### Developer Experience
- **XML Documentation**: Comprehensive XML documentation for all public APIs
- **SourceLink Support**: Source code debugging support in NuGet packages
- **Symbol Packages**: Symbol packages (snupkg) for enhanced debugging experience
- **Usage Examples**: Comprehensive examples in `Replicated.Example` project
- **Best Practices Guide**: Development guidelines and recommendations
- **Troubleshooting Guide**: Common issues and solutions documentation

#### Testing & Quality
- **Comprehensive Unit Tests**: 380+ unit tests covering all major functionality
- **Integration Test Suite**: 58+ integration tests with mock server
- **Mock Server**: Standalone mock server for integration testing
- **Code Coverage**: 83.8% line coverage, 78.6% branch coverage
- **Test Isolation**: Proper test isolation with unique state directories

#### Documentation
- **SDK README**: Complete installation and usage guide
- **Examples Documentation**: Code examples for common use cases
- **Best Practices**: Development guidelines and recommendations
- **Production Readiness Checklist**: Deployment checklist for production use
- **Production Recommendations**: Production deployment guidelines
- **Integration Test Documentation**: Setup and usage guide for integration tests
- **Mock Server Documentation**: Mock server setup and API documentation

### Technical Details

#### Supported Platforms
- .NET 8.0 (LTS)
- .NET 9.0 (STS)

#### Dependencies
- Polly 8.3.1 (retry and resilience)
- System.Text.Json 6.0.10 (JSON serialization)
- System.Management 7.0.2 (platform-specific features)

#### Package Features
- MIT License
- SourceLink enabled
- Symbol packages (snupkg)
- XML documentation included
- Multi-targeting support

### Breaking Changes

This is the initial release, so there are no breaking changes.

### Migration Guide

This is the initial release, so no migration is needed. See the [README](./Replicated/README.md) for getting started.

---

## [Unreleased]

Future improvements and features will be documented here.
