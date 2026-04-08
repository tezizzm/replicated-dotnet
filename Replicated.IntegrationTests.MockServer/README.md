# Replicated In-Cluster API Mock Server

Minimal ASP.NET Core server that simulates the Replicated in-cluster SDK API
(`http://replicated:3000/api/v1`) for local integration testing. Behavior is controlled via
request headers so tests can inject error scenarios without separate server configuration.

## Quick start

```bash
# Trust the dev certificate (first time only)
dotnet dev-certs https --trust

cd Replicated.IntegrationTests.MockServer
dotnet run
# Server starts at https://localhost:5001
```

Run integration tests against it:

```bash
dotnet test Replicated.IntegrationTests/
```

## Endpoints

| Method | Path | Description |
|---|---|---|
| GET | `/api/v1/app/info` | App name, slug, status, current release |
| GET | `/api/v1/app/status` | Resource state and sequence |
| GET | `/api/v1/app/updates` | Available update releases |
| GET | `/api/v1/app/history` | Previously installed releases |
| POST | `/api/v1/app/custom-metrics` | Send (replace) custom metrics |
| PATCH | `/api/v1/app/custom-metrics` | Merge (upsert) custom metrics |
| DELETE | `/api/v1/app/custom-metrics/{name}` | Delete a single metric |
| POST | `/api/v1/app/instance-tags` | Set instance tags |
| GET | `/api/v1/license/info` | License type, customer, channel, entitlements |
| GET | `/api/v1/license/fields` | All license fields |
| GET | `/api/v1/license/fields/{fieldName}` | Single license field |
| GET | `/health` | Health check |
| GET | `/` | Endpoint listing |

## Controlling responses

Set request headers to inject error scenarios without changing the URL:

| Header | Values | Effect |
|---|---|---|
| `X-Test-Status` | `401`, `403`, `404`, `429`, `400`, `500`, `502`, `503`, `504` | Returns that HTTP status |
| `X-Test-Delay` | milliseconds | Adds a delay before responding |
| `X-Test-Retry-After` | seconds | Sets `Retry-After` header on 429 responses |

The SDK's `ReplicatedClientIntegrationTests` injects `X-Test-Status` via
`HttpClient.DefaultRequestHeaders` to test error handling without needing multiple server
instances.

## Examples

```bash
# Health check
curl https://localhost:5001/health

# App info (success)
curl https://localhost:5001/api/v1/app/info

# Simulate unauthorized
curl -H "X-Test-Status: 401" https://localhost:5001/api/v1/app/info

# Simulate rate limit with retry-after
curl -H "X-Test-Status: 429" -H "X-Test-Retry-After: 60" https://localhost:5001/api/v1/app/info

# Simulate slow response
curl -H "X-Test-Delay: 2000" https://localhost:5001/api/v1/app/info
```

## Custom port

```bash
ASPNETCORE_URLS=https://localhost:5002 dotnet run
TEST_BASE_URL=https://localhost:5002 dotnet test Replicated.IntegrationTests/
```

## Certificate issues

```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```
