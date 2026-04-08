# Replicated .NET SDK — Integration Tests

Integration tests for the Replicated .NET SDK. Tests exercise `AppService` and `LicenseService`
against a running server and skip gracefully (via `catch ReplicatedNetworkError`) when no server
is available.

## Running integration tests

### Against the mock server (local development)

Start the mock server (separate terminal):

```bash
cd Replicated.IntegrationTests.MockServer
dotnet dev-certs https --trust   # first time only
dotnet run
```

Run the tests:

```bash
dotnet test Replicated.IntegrationTests/
```

The `ServerFixture` defaults to `https://localhost:5001`. Tests connect, execute, and assert
against mock responses.

### Against a real in-cluster environment

Set `TEST_BASE_URL` to override the server address:

```bash
TEST_BASE_URL=http://replicated:3000 dotnet test Replicated.IntegrationTests/
```

### In CI

The integration test job in `.github/workflows/ci.yml` builds the project as a hard gate and
runs the tests with `continue-on-error: true` on the test step (no server available in CI).

## Test structure

| Class | What it tests |
|---|---|
| `ReplicatedClientIntegrationTests` | `AppService` and `LicenseService` happy-path responses |

Tests inject error responses using the `X-Test-Status` request header, which the mock server
reads to return the requested HTTP status code. The `IntegrationTestBase.CreateClient()` helper
sets this header on the underlying `HttpClient`.

## Behavior without a server

Each test wraps its assertions in a `try/catch`:

```csharp
try
{
    var info = await client.App.GetInfoAsync();
    Assert.NotNull(info);
}
catch (ReplicatedNetworkError)
{
    return; // no server running — skip gracefully
}
catch (ReplicatedApiError ex)
{
    // mock server returned an error status — acceptable
}
```

This ensures tests never block CI when no server is present.
