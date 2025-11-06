# Replicated API Mock Server

A minimal ASP.NET Core mock server for integration testing the Replicated .NET SDK. Simulates all Replicated API endpoints with configurable responses for testing error handling, retry logic, and client behavior.

## Quick Start

### 1. Start the Mock Server

```bash
cd Replicated.IntegrationTests.MockServer
dotnet run
```

The server will start on `https://localhost:5001` with HTTPS enabled.

### 2. Trust Development Certificate (First Time Only)

```bash
dotnet dev-certs https --trust
```

### 3. Verify Server is Running

```bash
curl https://localhost:5001/health
# Should return: {"status":"healthy","timestamp":"..."}
```

## API Endpoints

The mock server implements all Replicated API endpoints:

### Customer Management
- **POST** `/v3/customer` - Create or get customer

### Instance Management  
- **POST** `/v3/instance` - Create instance
- **GET** `/kots_metrics/license_instance/info` - Get instance info

### Metrics
- **POST** `/application/custom-metrics` - Send custom metrics

### Utility
- **GET** `/health` - Health check
- **GET** `/` - API documentation

## Behavior Control

Control server responses using query parameters:

### Status Codes
Add `?status={code}` to any endpoint:
- `200` - Success (default)
- `400` - Bad Request
- `401` - Unauthorized  
- `403` - Forbidden
- `404` - Not Found
- `429` - Rate Limited
- `500` - Internal Server Error
- `502` - Bad Gateway
- `503` - Service Unavailable
- `504` - Gateway Timeout

### Response Delays
Add `?delay={milliseconds}` to simulate slow responses:
- `?delay=1000` - 1 second delay
- `?delay=5000` - 5 second delay

### Rate Limiting
Add `?retryAfter={seconds}` for rate limit responses:
- `?status=429&retryAfter=60` - Rate limited with 60 second retry

## Usage Examples

### Test Authentication Errors
```bash
curl -X POST https://localhost:5001/v3/customer?status=401
# Returns: {"message":"Unauthorized","code":"AUTH"}
```

### Test Rate Limiting
```bash
curl -X POST https://localhost:5001/v3/customer?status=429&retryAfter=60
# Returns: 429 with Retry-After: 60 header
```

### Test Slow Responses
```bash
curl -X POST https://localhost:5001/v3/customer?delay=2000
# Returns: 200 after 2 second delay
```

### Test Server Errors
```bash
curl -X POST https://localhost:5001/v3/customer?status=500
# Returns: {"message":"Internal Server Error","code":"SERVER_ERROR"}
```

## Integration with Tests

### Set Environment Variable
```bash
export TEST_BASE_URL=https://localhost:5001
```

### Run Integration Tests
```bash
cd Replicated.IntegrationTests
dotnet test
```

### Test Specific Scenarios
```csharp
[Fact]
public async Task Unauthorized_ShouldThrowAuthError()
{
    var client = new ReplicatedClient(
        publishableKey: "replicated_pk_test",
        appSlug: "test_app", 
        baseUrl: "https://localhost:5001");
    
    // This will hit: POST /v3/customer?status=401
    Assert.Throws<ReplicatedAuthError>(() => 
        client.Customer.GetOrCreate("install@example.com"));
}
```

## Configuration

### Custom Port
Set the port via environment variable:
```bash
export ASPNETCORE_URLS=https://localhost:5002
dotnet run
```

### Custom Base URL
Update your integration tests to use a different base URL:
```csharp
var client = new ReplicatedClient(
    publishableKey: "replicated_pk_test",
    appSlug: "test_app",
    baseUrl: "https://localhost:5002");
```

## Response Format

### Success Responses
```json
{
  "customer": {
    "id": "cust_abc12345",
    "email": "install@example.com", 
    "name": "Test Installation"
  }
}
```

### Error Responses
```json
{
  "message": "Unauthorized",
  "code": "AUTH"
}
```

### Rate Limit Responses
- Status: `429`
- Headers: `Retry-After: 60`
- Body: `{"message":"Rate limit exceeded","code":"RATE_LIMIT"}`

## Troubleshooting

### Certificate Issues
If you get certificate errors:
```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Port Already in Use
If port 5001 is busy:
```bash
export ASPNETCORE_URLS=https://localhost:5002
dotnet run
```

### CORS Issues
The server is configured to allow all origins for testing. If you need to restrict CORS, modify `Program.cs`.

## Development

### Adding New Endpoints
1. Add the endpoint mapping in `Program.cs`
2. Implement the status code logic
3. Add delay support if needed
4. Update this README

### Adding New Status Codes
1. Add the case to each endpoint's switch statement
2. Define the response body and headers
3. Update the documentation

## Architecture

- **Minimal API**: Uses ASP.NET Core minimal APIs for simplicity
- **Query Parameters**: Behavior controlled via URL parameters
- **Async/Await**: All endpoints are async for realistic behavior
- **CORS Enabled**: Allows cross-origin requests for testing
- **HTTPS Only**: Enforces secure connections

## License

Part of the Replicated .NET SDK project.
