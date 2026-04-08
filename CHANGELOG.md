# Changelog

All notable changes to the Replicated .NET SDK are documented here.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)

## Versioning policy

This project follows [Semantic Versioning 2.0.0](https://semver.org/spec/v2.0.0.html).

| Change type | Version bump | Examples |
|---|---|---|
| Bug fixes, doc updates, dependency patches | **patch** `0.1.x` | Fix a null-ref, bump Polly |
| New public APIs, new services, additive features | **minor** `0.x.0` | New builder method, new service |
| Removed or renamed public APIs, changed method signatures | **major** `x.0.0` | Rename `GetOrCreate`, remove a public class |

Internal (`internal`/`private`) changes are never breaking regardless of scope.

When a **minor** release adds a new interface member to `IReplicatedClient`, that is considered a **minor** bump (not major) because the interface is designed to be consumed, not implemented, by callers.

---

## [Unreleased]

---

## [0.1.0] - 2026-04-07

Initial release.

### Added

**Client**
- `ReplicatedClient` — typed client for the Replicated in-cluster API (`http://replicated:3000/api/v1`)
- `AppService` — app info (`GetInfoAsync`), custom metrics (`SendCustomMetricsAsync`), instance tags (`SetInstanceTagsAsync`, `GetInstanceTagsAsync`), instance status (`SetInstanceStatusAsync`, `GetInstanceStatusAsync`), app updates (`CheckForUpdatesAsync`)
- `LicenseService` — license info (`GetInfoAsync`), field lookup (`GetFieldAsync`), entitlement listing (`GetEntitlementsAsync`)
- `ReplicatedClientBuilder` — fluent builder with `WithBaseUrl()`, `WithTimeout()`, `WithRetryPolicy()`, `WithLogger()`
- `IReplicatedClient` interface for dependency injection / test mocking
- `AddReplicatedClient()` extension for `IServiceCollection` (optional `baseUrl`, `timeout`, `retryPolicy` parameters and builder delegate overload)
- Base URL defaults to `REPLICATED_SDK_ENDPOINT` environment variable, then `http://replicated:3000`

**HTTP layer**
- Static `SocketsHttpHandler` connection pool — prevents socket exhaustion
- Polly retry with exponential backoff and jitter: 3 retries by default, configurable via `RetryPolicy`
- Retries on network errors, `429 Too Many Requests`, and `5xx` server errors
- `CancellationToken` propagation on all async methods
- AOT/trim-safe JSON serialization via source-generated `ReplicatedJsonContext`

**Error handling**
- `ReplicatedApiError` — 4xx / 5xx responses; exposes `.HttpStatus` and `.Code`
- `ReplicatedAuthError` — 401 / 403 responses
- `ReplicatedRateLimitError` — 429 responses
- `ReplicatedNetworkError` — transport-level failures

**Quality**
- `EnablePackageValidation` active — CI catches breaking API surface changes
- SourceLink and `.snupkg` symbol packages included
- XML documentation on all public APIs
- Multi-targeting: `net8.0` (LTS) and `net9.0` (STS)

### Dependencies

| Package | Version |
|---|---|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | 8.0.0 |
| `Microsoft.Extensions.Logging.Abstractions` | 8.0.0 |
| `Polly` | 8.3.1 |
| `System.Text.Json` | 6.0.10 |
| `Microsoft.SourceLink.GitHub` | 8.0.0 *(build-only)* |
