# Replicated .NET SDK - Integration Tests

This project contains integration tests for the Replicated .NET SDK. These tests make actual API calls to the Replicated API and require valid credentials to run.

## Prerequisites

- Valid Replicated API credentials (publishable key and app slug)
- Network access to `https://replicated.app` (or your configured base URL)

## Configuration

Integration tests can be configured in several ways:

### Option 1: Environment Variables (Recommended)

```bash
export REPLICATED_PUBLISHABLE_KEY="replicated_pk_your_key_here"
export REPLICATED_APP_SLUG="your-app-slug"
export REPLICATED_BASE_URL="https://replicated.app"  # Optional
```

### Option 2: appsettings.test.json

Create or modify `appsettings.test.json`:

```json
{
  "Replicated": {
    "PublishableKey": "replicated_pk_your_key_here",
    "AppSlug": "your-app-slug",
    "BaseUrl": "https://replicated.app"
  }
}
```

### Option 3: User Secrets (for local development)

```bash
dotnet user-secrets init
dotnet user-secrets set "Replicated:PublishableKey" "replicated_pk_your_key_here"
dotnet user-secrets set "Replicated:AppSlug" "your-app-slug"
```

## Running Integration Tests

### Run All Integration Tests

```bash
dotnet test --filter "Category=Integration"
```

### Run All Tests (Including Integration)

```bash
dotnet test
```

### Skip Integration Tests (when credentials not available)

Integration tests will automatically skip if credentials are not provided. You can also explicitly skip them:

```bash
dotnet test --filter "Category!=Integration"
```

## Test Categories

Tests are marked with traits for easy filtering:

- `Category=Integration` - All integration tests
- `RequiresCredentials=true` - Tests that need API credentials

## Running in CI/CD

In CI/CD pipelines, integration tests should:

1. Use secure environment variables or secrets for credentials
2. Be run in a separate job/step from unit tests
3. Be optional (shouldn't fail the build if skipped)

Example GitHub Actions:

```yaml
- name: Run Integration Tests
  if: env.REPLICATED_PUBLISHABLE_KEY != ''
  env:
    REPLICATED_PUBLISHABLE_KEY: ${{ secrets.REPLICATED_PUBLISHABLE_KEY }}
    REPLICATED_APP_SLUG: ${{ secrets.REPLICATED_APP_SLUG }}
  run: dotnet test --filter "Category=Integration"
```

## Test Isolation

Integration tests use unique email addresses (with GUIDs) to ensure test isolation:

```csharp
var email = $"integration-test-{Guid.NewGuid()}@example.com";
```

This prevents tests from interfering with each other when using the same API account.

## Notes

- Integration tests make real API calls and may incur costs
- Tests create actual customers and instances in your Replicated account
- Use a test/development app slug for integration testing
- Tests are designed to clean up after themselves where possible

## Troubleshooting

### Tests are Skipped

If tests are being skipped, check:
1. Are environment variables set correctly?
2. Is `appsettings.test.json` configured?
3. Are user secrets configured (for local development)?

### Tests Fail with Authentication Errors

- Verify your publishable key is valid
- Check that the app slug matches your Replicated account
- Ensure the base URL is correct (if using a custom endpoint)

### Tests Fail with Network Errors

- Verify network connectivity to the API endpoint
- Check firewall/proxy settings
- Ensure the API endpoint is accessible

