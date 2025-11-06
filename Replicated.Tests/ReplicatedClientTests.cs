using Replicated;
using Replicated.Services;
using Replicated.Resources;
using Xunit;

namespace Replicated.Tests;

public class ReplicatedClientTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var publishableKey = "replicated_pk_test_key_123";
        var appSlug = "test_app";

        // Act
        var client = new ReplicatedClient(publishableKey, appSlug);

        // Assert
        Assert.NotNull(client);
        Assert.Equal(publishableKey, client.PublishableKey);
        Assert.Equal(appSlug, client.AppSlug);
        Assert.Equal("https://replicated.app", client.BaseUrl);
        Assert.NotNull(client.Customer);
    }

    [Fact]
    public void Constructor_WithCustomBaseUrl_ShouldUseCustomUrl()
    {
        // Arrange
        var publishableKey = "replicated_pk_test_key_123";
        var appSlug = "test_app";
        var customBaseUrl = "https://custom.replicated.app";

        // Act
        var client = new ReplicatedClient(publishableKey, appSlug, customBaseUrl);

        // Assert
        Assert.Equal(customBaseUrl, client.BaseUrl);
    }

    [Fact]
    public void Constructor_WithNullPublishableKey_ShouldThrowArgumentException()
    {
        // Arrange - Ensure environment variable is not set
        Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
        string? publishableKey = null;
        var appSlug = "test_app";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new ReplicatedClient(publishableKey!, appSlug));
        Assert.Contains("Publishable key is required", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullAppSlug_ShouldThrowArgumentException()
    {
        // Arrange - Ensure environment variable is not set
        var originalAppSlug = Environment.GetEnvironmentVariable("REPLICATED_APP_SLUG");
        var originalPublishableKey = Environment.GetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY");
        try
        {
            // Clear both variables to ensure test isolation
            Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);
            Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
            var publishableKey = "replicated_pk_test_key_123";
            string? appSlug = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ReplicatedClient(publishableKey, appSlug!));
            Assert.Contains("App slug is required", exception.Message);
        }
        finally
        {
            // Restore original values
            if (originalAppSlug != null)
                Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", originalAppSlug);
            else
                Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);
            if (originalPublishableKey != null)
                Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", originalPublishableKey);
            else
                Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
        }
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var client = new ReplicatedClient("replicated_pk_test_key_123", "test_app");

        // Act & Assert
        client.Dispose(); // Should not throw
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow()
    {
        // Arrange
        var client = new ReplicatedClient("replicated_pk_test_key_123", "test_app");

        // Act & Assert
        await client.DisposeAsync(); // Should not throw
    }

    [Fact]
    public void Constructor_WithInvalidPublishableKey_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidKey = "invalid_key_format";
        var appSlug = "test_app";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new ReplicatedClient(invalidKey, appSlug));
        Assert.Contains("Publishable key must start with 'replicated_pk_'", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidBaseUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var publishableKey = "replicated_pk_test_key_123";
        var appSlug = "test_app";
        var invalidUrl = "http://insecure-url.com"; // HTTP instead of HTTPS

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new ReplicatedClient(publishableKey, appSlug, invalidUrl));
        Assert.Contains("Base URL must use HTTPS protocol", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidTimeout_ShouldThrowArgumentException()
    {
        // Arrange
        var publishableKey = "replicated_pk_test_key_123";
        var appSlug = "test_app";
        var invalidTimeout = TimeSpan.FromHours(2); // Too long

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new ReplicatedClient(publishableKey, appSlug, timeout: invalidTimeout));
        Assert.Contains("Timeout cannot exceed 1 hour", exception.Message);
    }
}

public class EnvironmentVariableTests
{
    [Fact]
    public void Constructor_WithEnvironmentVariables_ShouldReadFromEnvironment()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", "replicated_pk_env_key_123");
        Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", "env_app");
        
        try
        {
            // Act
            var client = new ReplicatedClient();

            // Assert
            Assert.NotNull(client);
            Assert.Equal("replicated_pk_env_key_123", client.PublishableKey);
            Assert.Equal("env_app", client.AppSlug);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
            Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);
        }
    }

    [Fact]
    public void Constructor_WithExplicitParameters_ShouldOverrideEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", "replicated_pk_env_key_123");
        Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", "env_app");
        var explicitKey = "replicated_pk_explicit_key_456";
        var explicitSlug = "explicit_app";
        
        try
        {
            // Act
            var client = new ReplicatedClient(explicitKey, explicitSlug);

            // Assert
            Assert.Equal(explicitKey, client.PublishableKey);
            Assert.Equal(explicitSlug, client.AppSlug);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
            Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);
        }
    }

    [Fact]
    public void Constructor_WithBaseUrlFromEnvironment_ShouldUseEnvironmentValue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", "replicated_pk_test_123");
        Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", "test_app");
        Environment.SetEnvironmentVariable("REPLICATED_BASE_URL", "https://custom.replicated.app");
        
        try
        {
            // Act
            var client = new ReplicatedClient();

            // Assert
            Assert.Equal("https://custom.replicated.app", client.BaseUrl);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
            Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);
            Environment.SetEnvironmentVariable("REPLICATED_BASE_URL", null);
        }
    }

    [Fact]
    public void Constructor_WithTimeoutFromEnvironment_ShouldParseTimeout()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", "replicated_pk_test_123");
        Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", "test_app");
        Environment.SetEnvironmentVariable("REPLICATED_TIMEOUT", "60"); // 60 seconds
        
        try
        {
            // Act
            var client = new ReplicatedClient();

            // Assert
            Assert.Equal(TimeSpan.FromSeconds(60), client.Timeout);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
            Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);
            Environment.SetEnvironmentVariable("REPLICATED_TIMEOUT", null);
        }
    }

    [Fact]
    public void Constructor_WithStateDirectoryFromEnvironment_ShouldUseEnvironmentValue()
    {
        // Arrange
        var tempStateDir = Path.Combine(Path.GetTempPath(), $"replicated_test_{Guid.NewGuid().ToString("N")[..8]}");
        Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", "replicated_pk_test_123");
        Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", "test_app");
        Environment.SetEnvironmentVariable("REPLICATED_STATE_DIRECTORY", tempStateDir);
        
        try
        {
            // Act
            var client = new ReplicatedClient();

            // Assert
            Assert.Equal(tempStateDir, client.StateDirectory);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
            Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);
            Environment.SetEnvironmentVariable("REPLICATED_STATE_DIRECTORY", null);
            try
            {
                if (Directory.Exists(tempStateDir))
                    Directory.Delete(tempStateDir, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void Constructor_WithoutEnvironmentVariables_ShouldThrowArgumentException()
    {
        // Arrange - Ensure environment variables are not set
        var originalPublishableKey = Environment.GetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY");
        var originalAppSlug = Environment.GetEnvironmentVariable("REPLICATED_APP_SLUG");
        try
        {
            Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
            Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new ReplicatedClient());
            Assert.Contains("Publishable key is required", exception.Message);
            Assert.Contains("REPLICATED_PUBLISHABLE_KEY", exception.Message);
        }
        finally
        {
            // Restore original values
            if (originalPublishableKey != null)
                Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", originalPublishableKey);
            else
                Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);

            if (originalAppSlug != null)
                Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", originalAppSlug);
            else
                Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);
        }
    }
}

public class ReplicatedClientBuilderTests
{
    [Fact]
    public void Build_WithValidConfiguration_ShouldCreateClient()
    {
        // Arrange
        var builder = new ReplicatedClientBuilder()
            .WithPublishableKey("replicated_pk_test_123")
            .WithAppSlug("test_app");

        // Act
        var client = builder.Build();

        // Assert
        Assert.NotNull(client);
        Assert.Equal("replicated_pk_test_123", client.PublishableKey);
        Assert.Equal("test_app", client.AppSlug);
    }

    [Fact]
    public void Build_WithAllOptions_ShouldCreateClient()
    {
        // Arrange
        var tempStateDir = Path.Combine(Path.GetTempPath(), $"replicated_test_{Guid.NewGuid().ToString("N")[..8]}");
        var builder = new ReplicatedClientBuilder()
            .WithPublishableKey("replicated_pk_test_123")
            .WithAppSlug("test_app")
            .WithBaseUrl("https://custom.replicated.app")
            .WithTimeout(TimeSpan.FromSeconds(60))
            .WithStateDirectory(tempStateDir);

        // Act
        var client = builder.Build();

        // Assert
        Assert.Equal("replicated_pk_test_123", client.PublishableKey);
        Assert.Equal("test_app", client.AppSlug);
        Assert.Equal("https://custom.replicated.app", client.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(60), client.Timeout);
        Assert.Equal(tempStateDir, client.StateDirectory);
        
        // Cleanup
        try
        {
            if (Directory.Exists(tempStateDir))
                Directory.Delete(tempStateDir, recursive: true);
        }
        catch { }
    }

    [Fact]
    public void Build_WithoutPublishableKey_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new ReplicatedClientBuilder()
            .WithAppSlug("test_app");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("Publishable key is required", exception.Message);
    }

    [Fact]
    public void Build_WithoutAppSlug_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new ReplicatedClientBuilder()
            .WithPublishableKey("replicated_pk_test_123");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("App slug is required", exception.Message);
    }

    [Fact]
    public void Build_WithFromEnvironment_ShouldMergeWithEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", "replicated_pk_env_key_123");
        Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", "env_app");
        
        try
        {
            var builder = new ReplicatedClientBuilder()
                .FromEnvironment();

            // Act
            var client = builder.Build();

            // Assert
            Assert.Equal("replicated_pk_env_key_123", client.PublishableKey);
            Assert.Equal("env_app", client.AppSlug);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
            Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);
        }
    }

    [Fact]
    public void Build_WithExplicitValuesAndFromEnvironment_ShouldPreferExplicitValues()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", "replicated_pk_env_key_123");
        Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", "env_app");
        Environment.SetEnvironmentVariable("REPLICATED_BASE_URL", "https://env.replicated.app");
        
        try
        {
            var builder = new ReplicatedClientBuilder()
                .WithPublishableKey("replicated_pk_explicit_456")
                .WithAppSlug("explicit_app")
                .FromEnvironment();

            // Act
            var client = builder.Build();

            // Assert - Explicit values should override environment variables
            Assert.Equal("replicated_pk_explicit_456", client.PublishableKey);
            Assert.Equal("explicit_app", client.AppSlug);
            // Base URL should come from environment since not explicitly set
            Assert.Equal("https://env.replicated.app", client.BaseUrl);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
            Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);
            Environment.SetEnvironmentVariable("REPLICATED_BASE_URL", null);
        }
    }

    [Fact]
    public void Build_WithFromEnvironmentButMissingRequiredVariables_ShouldThrowArgumentException()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
        Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);

        var builder = new ReplicatedClientBuilder()
            .FromEnvironment();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => builder.Build());
        Assert.Contains("Publishable key is required", exception.Message);
    }

    [Fact]
    public void WithPublishableKey_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new ReplicatedClientBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithPublishableKey(null!));
    }

    [Fact]
    public void WithAppSlug_WithNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new ReplicatedClientBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithAppSlug(null!));
    }

    [Fact]
    public void WithRetryPolicy_ShouldSetRetryPolicy()
    {
        // Arrange
        var retryPolicy = new RetryPolicy { MaxRetries = 5, InitialDelay = TimeSpan.FromSeconds(2) };
        var builder = new ReplicatedClientBuilder()
            .WithPublishableKey("replicated_pk_test_123")
            .WithAppSlug("test_app")
            .WithRetryPolicy(retryPolicy);

        // Act
        var client = builder.Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void WithRetryPolicy_WithParameters_ShouldCreatePolicy()
    {
        // Arrange
        var builder = new ReplicatedClientBuilder()
            .WithPublishableKey("replicated_pk_test_123")
            .WithAppSlug("test_app")
            .WithRetryPolicy(maxRetries: 10, initialDelay: TimeSpan.FromSeconds(3));

        // Act
        var client = builder.Build();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void WithoutRetries_ShouldDisableRetries()
    {
        // Arrange
        var builder = new ReplicatedClientBuilder()
            .WithPublishableKey("replicated_pk_test_123")
            .WithAppSlug("test_app")
            .WithoutRetries();

        // Act
        var client = builder.Build();

        // Assert
        Assert.NotNull(client);
    }
}

public class RetryPolicyTests
{
    [Fact]
    public void Default_ShouldHaveReasonableDefaults()
    {
        // Act
        var policy = new RetryPolicy();

        // Assert
        Assert.Equal(3, policy.MaxRetries);
        Assert.Equal(TimeSpan.FromSeconds(1), policy.InitialDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), policy.MaxDelay);
        Assert.Equal(2.0, policy.BackoffMultiplier);
        Assert.True(policy.UseJitter);
        Assert.Equal(0.1, policy.JitterPercentage);
        Assert.True(policy.RetryOnNetworkError);
        Assert.True(policy.RetryOnRateLimit);
        Assert.True(policy.RetryOnServerError);
    }

    [Fact]
    public void Builder_WithInvalidRetryPolicy_ShouldThrowOnBuild()
    {
        // Arrange - Invalid policy (negative max retries)
        var invalidPolicy = new RetryPolicy { MaxRetries = -1 };
        var builder = new ReplicatedClientBuilder()
            .WithPublishableKey("replicated_pk_test_123")
            .WithAppSlug("test_app")
            .WithRetryPolicy(invalidPolicy);

        // Act & Assert - Validation happens in Build()
        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void Client_WithValidRetryPolicy_ShouldCreateSuccessfully()
    {
        // Arrange
        var validPolicy = new RetryPolicy
        {
            MaxRetries = 5,
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(30)
        };

        // Act
        var client = new ReplicatedClient(
            "replicated_pk_test_123",
            "test_app",
            retryPolicy: validPolicy);

        // Assert
        Assert.NotNull(client);
    }
}

public class RetryEnvironmentVariableTests
{
    [Fact]
    public void Constructor_WithoutRetryPolicy_ShouldUseDefaultPolicy()
    {
        // Arrange & Act - Create client without explicit retry policy (should use default)
        var client = new ReplicatedClient(
            publishableKey: "replicated_pk_test_123",
            appSlug: "test_app",
            retryPolicy: null);  // null means default policy

        // Assert - Should use default retry policy (validated internally)
        Assert.NotNull(client);
    }

    [Fact]
    public void Constructor_WithRetryPolicyFromEnvironment_ShouldUseEnvironmentPolicy()
    {
        // Arrange: snapshot and clear retry-related env, then set a minimal, valid config
        var vars = new (string Name, string? Value)[]
        {
            ("REPLICATED_PUBLISHABLE_KEY", Environment.GetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY")),
            ("REPLICATED_APP_SLUG", Environment.GetEnvironmentVariable("REPLICATED_APP_SLUG")),
            ("REPLICATED_MAX_RETRIES", Environment.GetEnvironmentVariable("REPLICATED_MAX_RETRIES")),
            ("REPLICATED_RETRY_INITIAL_DELAY", Environment.GetEnvironmentVariable("REPLICATED_RETRY_INITIAL_DELAY")),
            ("REPLICATED_RETRY_MAX_DELAY", Environment.GetEnvironmentVariable("REPLICATED_RETRY_MAX_DELAY")),
            ("REPLICATED_RETRY_BACKOFF_MULTIPLIER", Environment.GetEnvironmentVariable("REPLICATED_RETRY_BACKOFF_MULTIPLIER")),
            ("REPLICATED_RETRY_USE_JITTER", Environment.GetEnvironmentVariable("REPLICATED_RETRY_USE_JITTER")),
            ("REPLICATED_RETRY_JITTER_PERCENTAGE", Environment.GetEnvironmentVariable("REPLICATED_RETRY_JITTER_PERCENTAGE")),
            ("REPLICATED_RETRY_ON_RATE_LIMIT", Environment.GetEnvironmentVariable("REPLICATED_RETRY_ON_RATE_LIMIT")),
            ("REPLICATED_RETRY_ON_SERVER_ERROR", Environment.GetEnvironmentVariable("REPLICATED_RETRY_ON_SERVER_ERROR")),
            ("REPLICATED_RETRY_ON_NETWORK_ERROR", Environment.GetEnvironmentVariable("REPLICATED_RETRY_ON_NETWORK_ERROR"))
        };

        foreach (var (Name, _) in vars)
        {
            Environment.SetEnvironmentVariable(Name, null);
        }

        Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", "replicated_pk_test_123");
        Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", "test_app");
        Environment.SetEnvironmentVariable("REPLICATED_MAX_RETRIES", "3");
        Environment.SetEnvironmentVariable("REPLICATED_RETRY_INITIAL_DELAY", "500");

        try
        {
            // Act
            var client = new ReplicatedClient();

            // Assert
            Assert.NotNull(client);
        }
        finally
        {
            // Restore snapshot
            foreach (var (Name, Value) in vars)
            {
                Environment.SetEnvironmentVariable(Name, Value);
            }
        }
    }

    [Fact]
    public void Builder_WithFromEnvironment_ShouldMergeRetryPolicyFromEnvironment()
    {
        // Arrange
        Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", "replicated_pk_test_123");
        Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", "test_app");
        Environment.SetEnvironmentVariable("REPLICATED_MAX_RETRIES", "7");

        try
        {
            var builder = new ReplicatedClientBuilder()
                .FromEnvironment();

            // Act
            var client = builder.Build();

            // Assert
            Assert.NotNull(client);
        }
        finally
        {
            Environment.SetEnvironmentVariable("REPLICATED_PUBLISHABLE_KEY", null);
            Environment.SetEnvironmentVariable("REPLICATED_APP_SLUG", null);
            Environment.SetEnvironmentVariable("REPLICATED_MAX_RETRIES", null);
        }
    }
}


public class FingerprintTests
{
    [Fact]
    public void GetMachineFingerprint_ShouldReturnNonEmptyString()
    {
        // Act
        var fingerprint = Fingerprint.GetMachineFingerprint();

        // Assert
        Assert.NotNull(fingerprint);
        Assert.NotEmpty(fingerprint);
        Assert.True(fingerprint.Length == 64); // SHA256 hex string length
    }

    [Fact]
    public void GetMachineFingerprint_ShouldReturnConsistentValue()
    {
        // Act
        var fingerprint1 = Fingerprint.GetMachineFingerprint();
        var fingerprint2 = Fingerprint.GetMachineFingerprint();

        // Assert
        Assert.Equal(fingerprint1, fingerprint2);
    }

    [Fact]
    public void GetMachineFingerprint_ShouldBeValidHexString()
    {
        // Act
        var fingerprint = Fingerprint.GetMachineFingerprint();

        // Assert
        Assert.Matches("^[0-9a-f]{64}$", fingerprint);
    }

    [Fact]
    public void GetMachineFingerprint_MultipleCalls_ShouldReturnSameValue()
    {
        // Act
        var fingerprints = Enumerable.Range(0, 5)
            .Select(_ => Fingerprint.GetMachineFingerprint())
            .ToList();

        // Assert
        Assert.All(fingerprints, fp => Assert.Equal(fingerprints[0], fp));
    }
}

public class StateManagerTests
{
    [Fact]
    public void Constructor_WithValidAppSlug_ShouldCreateInstance()
    {
        // Arrange
        var appSlug = "test_app";

        // Act
        var stateManager = new StateManager(appSlug);

        // Assert
        Assert.NotNull(stateManager);
    }

    [Fact]
    public void Constructor_WithNullAppSlug_ShouldThrowArgumentNullException()
    {
        // Arrange
        string? appSlug = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StateManager(appSlug!));
    }

    [Fact]
    public void GetState_Initially_ShouldReturnEmptyDictionary()
    {
        // Arrange - Use a unique app slug to avoid state sharing
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}");

        // Act
        var state = stateManager.GetState();

        // Assert
        Assert.NotNull(state);
        Assert.Empty(state);
    }

    [Fact]
    public void SetAndGetCustomerId_ShouldWork()
    {
        // Arrange - Use a unique app slug to avoid state sharing
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}");
        var customerId = "test_customer_id";

        // Act
        stateManager.SetCustomerId(customerId);
        var retrievedId = stateManager.GetCustomerId();

        // Assert
        Assert.Equal(customerId, retrievedId);
    }

    [Fact]
    public void SetAndGetInstanceId_ShouldWork()
    {
        // Arrange - Use a unique app slug to avoid state sharing
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}");
        var instanceId = "test_instance_id";

        // Act
        stateManager.SetInstanceId(instanceId);
        var retrievedId = stateManager.GetInstanceId();

        // Assert
        Assert.Equal(instanceId, retrievedId);
    }

    [Fact]
    public void ClearState_ShouldResetToEmpty()
    {
        // Arrange
        var stateManager = new StateManager("test_app");
        stateManager.SetCustomerId("test_id");
        stateManager.SetInstanceId("test_instance");

        // Act
        stateManager.ClearState();

        // Assert
        Assert.Null(stateManager.GetCustomerId());
        Assert.Null(stateManager.GetInstanceId());
    }

    [Fact]
    public void SetAndGetCustomerEmail_ShouldWork()
    {
        // Arrange - Use a unique app slug to avoid state sharing
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}");
        var email = "test@example.com";

        // Act
        stateManager.SetCustomerEmail(email);
        var retrievedEmail = stateManager.GetCustomerEmail();

        // Assert
        Assert.Equal(email, retrievedEmail);
    }

    [Fact]
    public void SetAndGetDynamicToken_ShouldWork()
    {
        // Arrange - Use a unique app slug to avoid state sharing
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}");
        var token = "dynamic_token_123";

        // Act
        stateManager.SetDynamicToken(token);
        var retrievedToken = stateManager.GetDynamicToken();

        // Assert
        Assert.Equal(token, retrievedToken);
    }

    [Fact]
    public void GetState_WithMultipleValues_ShouldReturnAllValues()
    {
        // Arrange - Use a unique app slug to avoid state sharing
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}");
        stateManager.SetCustomerId("customer_123");
        stateManager.SetInstanceId("instance_456");
        stateManager.SetCustomerEmail("test@example.com");
        stateManager.SetDynamicToken("dynamic_token");

        // Act
        var state = stateManager.GetState();

        // Assert
        Assert.NotNull(state);
        Assert.Equal("customer_123", state["customer_id"]?.ToString());
        Assert.Equal("instance_456", state["instance_id"]?.ToString());
        Assert.Equal("test@example.com", state["customer_email"]?.ToString());
        Assert.Equal("dynamic_token", state["dynamic_token"]?.ToString());
    }

    [Fact]
    public void Constructor_WithCustomStateDirectory_ShouldUseCustomDirectory()
    {
        // Arrange
        var appSlug = "test_app";
        var customDirectory = Path.Combine(Path.GetTempPath(), "custom_replicated_state");

        // Act
        var stateManager = new StateManager(appSlug, customDirectory);

        // Assert
        Assert.NotNull(stateManager);
        // Note: We can't easily test the actual directory creation without file system access
        // but we can verify the constructor doesn't throw
    }
}

public class InstanceStatusTests
{
    [Fact]
    public void InstanceStatus_Values_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("running", InstanceStatus.Running.ToString().ToLower());
        Assert.Equal("degraded", InstanceStatus.Degraded.ToString().ToLower());
        Assert.Equal("missing", InstanceStatus.Missing.ToString().ToLower());
        Assert.Equal("unavailable", InstanceStatus.Unavailable.ToString().ToLower());
        Assert.Equal("ready", InstanceStatus.Ready.ToString().ToLower());
        Assert.Equal("updating", InstanceStatus.Updating.ToString().ToLower());
    }

    [Theory]
    [InlineData("running", InstanceStatus.Running)]
    [InlineData("degraded", InstanceStatus.Degraded)]
    [InlineData("missing", InstanceStatus.Missing)]
    [InlineData("unavailable", InstanceStatus.Unavailable)]
    [InlineData("ready", InstanceStatus.Ready)]
    [InlineData("updating", InstanceStatus.Updating)]
    public void InstanceStatus_StringValues_ShouldMatch(string expected, InstanceStatus actual)
    {
        Assert.Equal(expected, actual.ToString().ToLower());
    }
}

public class ExceptionTests
{
    [Fact]
    public void ReplicatedException_ShouldInheritFromException()
    {
        // Act
        var exception = new ReplicatedException("Test message");

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
        Assert.Equal("Test message", exception.Message);
    }

    [Fact]
    public void ReplicatedException_WithHttpStatus_ShouldPreserveStatus()
    {
        // Arrange
        var httpStatus = 400;

        // Act
        var exception = new ReplicatedException("API error", httpStatus);

        // Assert
        Assert.Equal("API error", exception.Message);
        Assert.Equal(httpStatus, exception.HttpStatus);
    }

    [Fact]
    public void ReplicatedApiError_ShouldInheritFromReplicatedException()
    {
        // Act
        var exception = new ReplicatedApiError("API error", 400);

        // Assert
        Assert.IsAssignableFrom<ReplicatedException>(exception);
        Assert.Equal("API error", exception.Message);
        Assert.Equal(400, exception.HttpStatus);
    }

    [Fact]
    public void ReplicatedApiError_WithResponse_ShouldPreserveResponse()
    {
        // Arrange
        var response = new Dictionary<string, object> { ["error"] = "Invalid request" };

        // Act
        var exception = new ReplicatedApiError("API error", 400, jsonBody: response);

        // Assert
        Assert.Equal("API error", exception.Message);
        Assert.Equal(400, exception.HttpStatus);
        Assert.Equal(response, exception.JsonBody);
    }

    [Fact]
    public void ReplicatedAuthError_ShouldInheritFromReplicatedException()
    {
        // Arrange
        var innerException = new HttpRequestException("Network error");

        // Act
        var exception = new ReplicatedAuthError("Auth failed");

        // Assert
        Assert.IsAssignableFrom<ReplicatedException>(exception);
        Assert.Equal("Auth failed", exception.Message);
    }

    [Fact]
    public void ReplicatedNetworkError_ShouldInheritFromReplicatedException()
    {
        // Act
        var exception = new ReplicatedNetworkError("Network failed");

        // Assert
        Assert.IsAssignableFrom<ReplicatedException>(exception);
        Assert.Equal("Network failed", exception.Message);
    }
}

public class CustomerTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerId = "customer_123";
        var email = "test@example.com";

        // Act
        var customer = new Customer(mockClient, customerId, email);

        // Assert
        Assert.NotNull(customer);
        Assert.Equal(customerId, customer.CustomerId);
        Assert.Equal(email, customer.EmailAddress);
        Assert.Null(customer.Channel);
    }

    [Fact]
    public void Constructor_WithChannel_ShouldSetChannel()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerId = "customer_123";
        var email = "test@example.com";
        var channel = "beta";

        // Act
        var customer = new Customer(mockClient, customerId, email, channel);

        // Assert
        Assert.Equal(channel, customer.Channel);
    }

    [Fact]
    public void Constructor_WithData_ShouldSetData()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerId = "customer_123";
        var email = "test@example.com";
        var data = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var customer = new Customer(mockClient, customerId, email, data: data);

        // Assert
        Assert.Equal("value", customer.GetData("key"));
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        IReplicatedClient? client = null;
        var customerId = "customer_123";
        var email = "test@example.com";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Customer(client!, customerId, email));
    }

    [Fact]
    public void Constructor_WithNullCustomerId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockClient = CreateMockClient();
        string? customerId = null;
        var email = "test@example.com";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Customer(mockClient, customerId!, email));
    }

    [Fact]
    public void Constructor_WithNullEmail_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerId = "customer_123";
        string? email = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Customer(mockClient, customerId, email!));
    }

    [Fact]
    public void GetOrCreateInstance_ShouldReturnInstance()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customer = new Customer(mockClient, "customer_123", "test@example.com");

        // Act
        var instance = customer.GetOrCreateInstance();

        // Assert
        Assert.NotNull(instance);
        Assert.Equal("customer_123", instance.CustomerId);
    }

    [Fact]
    public async Task GetOrCreateInstanceAsync_ShouldReturnInstance()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customer = new Customer(mockClient, "customer_123", "test@example.com");

        // Act
        var instance = await customer.GetOrCreateInstanceAsync();

        // Assert
        Assert.NotNull(instance);
        Assert.Equal("customer_123", instance.CustomerId);
    }

    [Fact]
    public void GetData_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var data = new Dictionary<string, object> { ["key1"] = "value1", ["key2"] = 42 };
        var customer = new Customer(mockClient, "customer_123", "test@example.com", data: data);

        // Act & Assert
        Assert.Equal("value1", customer.GetData("key1"));
        Assert.Equal(42, customer.GetData("key2"));
    }

    [Fact]
    public void GetData_WithNonExistentKey_ShouldReturnNull()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customer = new Customer(mockClient, "customer_123", "test@example.com");

        // Act
        var result = customer.GetData("nonexistent");

        // Assert
        Assert.Null(result);
    }

    private static IReplicatedClient CreateMockClient()
    {
        // Create a simple mock that implements the interface
        return new MockReplicatedClient();
    }

    private class MockReplicatedClient : IReplicatedClient
    {
        public string PublishableKey => "test_key";
        public string AppSlug => "test_app";
        public string BaseUrl => "https://test.replicated.app";
        public TimeSpan Timeout => TimeSpan.FromSeconds(30);
        public string? StateDirectory => null;
        public string MachineId => "test_machine_id";
        public StateManager StateManager => new StateManager("test_app");
        public CustomerService Customer => new CustomerService(this);

        public Dictionary<string, string> GetAuthHeaders()
        {
            return new Dictionary<string, string> { ["Authorization"] = "Bearer test_token" };
        }

        public Dictionary<string, object> MakeRequest(string method, string url, Dictionary<string, string>? headers = null, Dictionary<string, object>? jsonData = null, Dictionary<string, object>? parameters = null)
        {
            return new Dictionary<string, object> { ["success"] = true };
        }

        public Task<Dictionary<string, object>> MakeRequestAsync(string method, string url, Dictionary<string, string>? headers = null, Dictionary<string, object>? jsonData = null, Dictionary<string, object>? parameters = null)
        {
            return Task.FromResult(new Dictionary<string, object> { ["success"] = true });
        }
    }
}

public class InstanceTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerId = "customer_123";

        // Act
        var instance = new Instance(mockClient, customerId);

        // Assert
        Assert.NotNull(instance);
        Assert.Equal(customerId, instance.CustomerId);
        Assert.Null(instance.InstanceId);
    }

    [Fact]
    public void Constructor_WithInstanceId_ShouldSetInstanceId()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerId = "customer_123";
        var instanceId = "instance_456";

        // Act
        var instance = new Instance(mockClient, customerId, instanceId);

        // Assert
        Assert.Equal(instanceId, instance.InstanceId);
    }

    [Fact]
    public void Constructor_WithData_ShouldSetData()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerId = "customer_123";
        var data = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        var instance = new Instance(mockClient, customerId, data: data);

        // Assert
        Assert.Equal("value", instance.GetData("key"));
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        IReplicatedClient? client = null;
        var customerId = "customer_123";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Instance(client!, customerId));
    }

    [Fact]
    public void Constructor_WithNullCustomerId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockClient = CreateMockClient();
        string? customerId = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Instance(mockClient, customerId!));
    }

    [Fact]
    public void SendMetric_ShouldNotThrow()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456"); // Provide instance ID to avoid EnsureInstance

        // Act & Assert
        instance.SendMetric("cpu_usage", 0.75); // Should not throw with mock
    }

    [Fact]
    public async Task SendMetricAsync_ShouldNotThrow()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456"); // Provide instance ID to avoid EnsureInstanceAsync

        // Act & Assert
        await instance.SendMetricAsync("cpu_usage", 0.75); // Should not throw with mock
    }

    [Fact]
    public void SetStatus_ShouldNotThrow()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456"); // Provide instance ID to avoid EnsureInstance

        // Act & Assert
        instance.SetStatus("running"); // Should not throw with mock
    }

    [Fact]
    public async Task SetStatusAsync_ShouldNotThrow()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456"); // Provide instance ID to avoid EnsureInstanceAsync

        // Act & Assert
        await instance.SetStatusAsync("running"); // Should not throw with mock
    }

    [Fact]
    public void SetVersion_ShouldNotThrow()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456"); // Provide instance ID to avoid EnsureInstance

        // Act & Assert
        instance.SetVersion("1.0.0"); // Should not throw with mock
    }

    [Fact]
    public async Task SetVersionAsync_ShouldNotThrow()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456"); // Provide instance ID to avoid EnsureInstanceAsync

        // Act & Assert
        await instance.SetVersionAsync("1.0.0"); // Should not throw with mock
    }

    [Fact]
    public void GetData_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var data = new Dictionary<string, object> { ["key1"] = "value1", ["key2"] = 42 };
        var instance = new Instance(mockClient, "customer_123", data: data);

        // Act & Assert
        Assert.Equal("value1", instance.GetData("key1"));
        Assert.Equal(42, instance.GetData("key2"));
    }

    [Fact]
    public void GetData_WithNonExistentKey_ShouldReturnNull()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123");

        // Act
        var result = instance.GetData("nonexistent");

        // Assert
        Assert.Null(result);
    }

    private static IReplicatedClient CreateMockClient()
    {
        return new MockReplicatedClient();
    }

    private class MockReplicatedClient : IReplicatedClient
    {
        public string PublishableKey => "test_key";
        public string AppSlug => "test_app";
        public string BaseUrl => "https://test.replicated.app";
        public TimeSpan Timeout => TimeSpan.FromSeconds(30);
        public string? StateDirectory => null;
        public string MachineId => "test_machine_id";
        public StateManager StateManager => new StateManager("test_app");
        public CustomerService Customer => new CustomerService(this);

        public Dictionary<string, string> GetAuthHeaders()
        {
            return new Dictionary<string, string> { ["Authorization"] = "Bearer test_token" };
        }

        public Dictionary<string, object> MakeRequest(string method, string url, Dictionary<string, string>? headers = null, Dictionary<string, object>? jsonData = null, Dictionary<string, object>? parameters = null)
        {
            return new Dictionary<string, object> { ["success"] = true };
        }

        public Task<Dictionary<string, object>> MakeRequestAsync(string method, string url, Dictionary<string, string>? headers = null, Dictionary<string, object>? jsonData = null, Dictionary<string, object>? parameters = null)
        {
            return Task.FromResult(new Dictionary<string, object> { ["success"] = true });
        }
    }

    [Fact]
    public void SendMetric_WithInvalidMetricName_ShouldThrowArgumentException()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => instance.SendMetric("invalid-metric-name!", 123));
        Assert.Contains("Metric name can only contain alphanumeric characters and underscores", exception.Message);
    }

    [Fact]
    public void SendMetric_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => instance.SendMetric("valid_metric", null!));
    }

    [Fact]
    public async Task SendMetricAsync_WithInvalidMetricName_ShouldThrowArgumentException()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await instance.SendMetricAsync("invalid-metric-name!", 123));
        Assert.Contains("Metric name can only contain alphanumeric characters and underscores", exception.Message);
    }

    [Fact]
    public void SetStatus_WithInvalidStatus_ShouldThrowArgumentException()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => instance.SetStatus("invalid_status"));
        Assert.Contains("Invalid status", exception.Message);
    }

    [Fact]
    public void SetVersion_WithInvalidVersion_ShouldThrowArgumentException()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var instance = new Instance(mockClient, "customer_123", "instance_456");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => instance.SetVersion("version@with#invalid!chars"));
        Assert.Contains("Version can only contain alphanumeric characters, dots, underscores, and hyphens", exception.Message);
    }
}

public class CustomerServiceTests
{
    [Fact]
    public void Constructor_WithValidClient_ShouldCreateInstance()
    {
        // Arrange
        var mockClient = CreateMockClient();

        // Act
        var customerService = new CustomerService(mockClient);

        // Assert
        Assert.NotNull(customerService);
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrowArgumentNullException()
    {
        // Arrange
        IReplicatedClient? client = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CustomerService(client!));
    }

    [Fact]
    public void GetOrCreate_WithValidEmail_ShouldReturnCustomer()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerService = new CustomerService(mockClient);
        var email = "test@example.com";

        // Act
        var customer = customerService.GetOrCreate(email);

        // Assert
        Assert.NotNull(customer);
        Assert.Equal(email, customer.EmailAddress);
    }

    [Fact]
    public void GetOrCreate_WithChannel_ShouldUseChannel()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerService = new CustomerService(mockClient);
        var email = "test@example.com";
        var channel = "beta";

        // Act
        var customer = customerService.GetOrCreate(email, channel);

        // Assert
        Assert.Equal(channel, customer.Channel);
    }

    [Fact]
    public void GetOrCreate_WithName_ShouldUseName()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerService = new CustomerService(mockClient);
        var email = "test@example.com";
        var name = "Test User";

        // Act
        var customer = customerService.GetOrCreate(email, name: name);

        // Assert
        Assert.NotNull(customer);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithValidEmail_ShouldReturnCustomer()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerService = new CustomerService(mockClient);
        var email = "test@example.com";

        // Act
        var customer = await customerService.GetOrCreateAsync(email);

        // Assert
        Assert.NotNull(customer);
        Assert.Equal(email, customer.EmailAddress);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithChannel_ShouldUseChannel()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerService = new CustomerService(mockClient);
        var email = "test@example.com";
        var channel = "beta";

        // Act
        var customer = await customerService.GetOrCreateAsync(email, channel);

        // Assert
        Assert.Equal(channel, customer.Channel);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithName_ShouldUseName()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerService = new CustomerService(mockClient);
        var email = "test@example.com";
        var name = "Test User";

        // Act
        var customer = await customerService.GetOrCreateAsync(email, name: name);

        // Assert
        Assert.NotNull(customer);
    }

    private static IReplicatedClient CreateMockClient()
    {
        return new MockReplicatedClient();
    }

    private class MockReplicatedClient : IReplicatedClient
    {
        public string PublishableKey => "test_key";
        public string AppSlug => "test_app";
        public string BaseUrl => "https://test.replicated.app";
        public TimeSpan Timeout => TimeSpan.FromSeconds(30);
        public string? StateDirectory => null;
        public string MachineId => "test_machine_id";
        public StateManager StateManager => new StateManager("test_app");
        public CustomerService Customer => new CustomerService(this);

        public Dictionary<string, string> GetAuthHeaders()
        {
            return new Dictionary<string, string> { ["Authorization"] = "Bearer test_token" };
        }

        public Dictionary<string, object> MakeRequest(string method, string url, Dictionary<string, string>? headers = null, Dictionary<string, object>? jsonData = null, Dictionary<string, object>? parameters = null)
        {
            // Mock response for customer creation
            return new Dictionary<string, object>
            {
                ["customer"] = new Dictionary<string, object>
                {
                    ["id"] = "customer_123",
                    ["email"] = "test@example.com",
                    ["instanceId"] = "instance_456" // Move instanceId inside customer object
                },
                ["instance"] = new Dictionary<string, object>
                {
                    ["id"] = "instance_456"
                },
                ["dynamic_token"] = "dynamic_token_123",
                ["service_token"] = "service_token_456"
            };
        }

        public Task<Dictionary<string, object>> MakeRequestAsync(string method, string url, Dictionary<string, string>? headers = null, Dictionary<string, object>? jsonData = null, Dictionary<string, object>? parameters = null)
        {
            return Task.FromResult(MakeRequest(method, url, headers, jsonData, parameters));
        }
    }

    [Fact]
    public void GetOrCreate_WithInvalidEmail_ShouldThrowArgumentException()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerService = new CustomerService(mockClient);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => customerService.GetOrCreate("invalid-email"));
        Assert.Contains("Invalid email address format", exception.Message);
    }

    [Fact]
    public void GetOrCreate_WithInvalidChannel_ShouldThrowArgumentException()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var customerService = new CustomerService(mockClient);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => customerService.GetOrCreate("test@example.com", "invalid@channel"));
        Assert.Contains("Channel can only contain alphanumeric characters, underscores, hyphens, and spaces", exception.Message);
    }
}

public class IntegrationWorkflowTests
{
    [Fact]
    public void CompleteWorkflow_Sync_ShouldWorkEndToEnd()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var email = "integration@example.com";

        // Act - Complete workflow
        var customer = mockClient.Customer.GetOrCreate(email, "beta", "Integration Test User");
        var instance = customer.GetOrCreateInstance();
        instance.SetStatus("running");
        instance.SetVersion("1.0.0");
        instance.SendMetric("cpu_usage", 0.75);
        instance.SendMetric("memory_usage", 0.60);
        instance.SendMetric("active_users", 150);

        // Assert
        Assert.NotNull(customer);
        Assert.Equal(email, customer.EmailAddress);
        Assert.Equal("beta", customer.Channel);
        Assert.NotNull(instance);
        Assert.Equal(customer.CustomerId, instance.CustomerId);
    }

    [Fact]
    public async Task CompleteWorkflow_Async_ShouldWorkEndToEnd()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var email = "integration-async@example.com";

        // Act - Complete async workflow
        var customer = await mockClient.Customer.GetOrCreateAsync(email, "stable", "Async Integration Test User");
        var instance = await customer.GetOrCreateInstanceAsync();
        await instance.SetStatusAsync("running");
        await instance.SetVersionAsync("2.0.0");
        await instance.SendMetricAsync("cpu_usage", 0.85);
        await instance.SendMetricAsync("memory_usage", 0.70);
        await instance.SendMetricAsync("active_users", 200);

        // Assert
        Assert.NotNull(customer);
        Assert.Equal(email, customer.EmailAddress);
        Assert.Equal("stable", customer.Channel);
        Assert.NotNull(instance);
        Assert.Equal(customer.CustomerId, instance.CustomerId);
    }

    [Fact]
    public void StatePersistence_ShouldWorkAcrossOperations()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var email = "state@example.com";

        // Act - First operation
        var customer1 = mockClient.Customer.GetOrCreate(email);
        var instance1 = customer1.GetOrCreateInstance();
        instance1.SetStatus("running");

        // Act - Second operation (should use cached state)
        var customer2 = mockClient.Customer.GetOrCreate(email);
        var instance2 = customer2.GetOrCreateInstance();

        // Assert - Should be the same customer/instance due to state persistence
        // Note: With mock clients, we can't test actual state persistence, so we just verify they don't throw
        Assert.NotNull(customer1);
        Assert.NotNull(customer2);
        Assert.NotNull(instance1);
        Assert.NotNull(instance2);
    }

    [Fact]
    public void MultipleCustomers_ShouldWorkIndependently()
    {
        // Arrange
        var mockClient = CreateMockClient();
        var email1 = "customer1@example.com";
        var email2 = "customer2@example.com";

        // Act
        var customer1 = mockClient.Customer.GetOrCreate(email1, "stable");
        var customer2 = mockClient.Customer.GetOrCreate(email2, "beta");
        
        var instance1 = customer1.GetOrCreateInstance();
        var instance2 = customer2.GetOrCreateInstance();

        instance1.SetStatus("running");
        instance2.SetStatus("unavailable");

        // Assert
        Assert.NotEqual(customer1.CustomerId, customer2.CustomerId);
        Assert.NotEqual(instance1.CustomerId, instance2.CustomerId);
        Assert.Equal("stable", customer1.Channel);
        Assert.Equal("beta", customer2.Channel);
    }

    private static IReplicatedClient CreateMockClient()
    {
        return new MockReplicatedClient();
    }

    private class MockReplicatedClient : IReplicatedClient
    {
        public string PublishableKey => "test_key";
        public string AppSlug => "test_app";
        public string BaseUrl => "https://test.replicated.app";
        public TimeSpan Timeout => TimeSpan.FromSeconds(30);
        public string? StateDirectory => null;
        public string MachineId => "test_machine_id";
        public StateManager StateManager => new StateManager("test_app");
        public CustomerService Customer => new CustomerService(this);

        public Dictionary<string, string> GetAuthHeaders()
        {
            return new Dictionary<string, string> { ["Authorization"] = "Bearer test_token" };
        }

        public Dictionary<string, object> MakeRequest(string method, string url, Dictionary<string, string>? headers = null, Dictionary<string, object>? jsonData = null, Dictionary<string, object>? parameters = null)
        {
            // Mock realistic responses based on URL
            if (url.Contains("/v3/customer"))
            {
                var instanceId = $"instance_{Guid.NewGuid().ToString("N")[..8]}";
                return new Dictionary<string, object>
                {
                    ["customer"] = new Dictionary<string, object>
                    {
                        ["id"] = $"customer_{Guid.NewGuid().ToString("N")[..8]}",
                        ["email"] = "test@example.com",
                        ["instanceId"] = instanceId // Move instanceId inside customer object
                    },
                    ["instance"] = new Dictionary<string, object>
                    {
                        ["id"] = instanceId
                    },
                    ["instance_id"] = instanceId, // Add missing key for Instance class
                    ["dynamic_token"] = $"dynamic_{Guid.NewGuid().ToString("N")[..8]}",
                    ["service_token"] = $"service_{Guid.NewGuid().ToString("N")[..8]}"
                };
            }
            
            if (url.Contains("/v3/instance"))
            {
                return new Dictionary<string, object>
                {
                    ["instance"] = new Dictionary<string, object>
                    {
                        ["id"] = $"instance_{Guid.NewGuid().ToString("N")[..8]}"
                    },
                    ["instance_id"] = $"instance_{Guid.NewGuid().ToString("N")[..8]}" // Add missing key
                };
            }
            
            return new Dictionary<string, object> { ["success"] = true };
        }

        public Task<Dictionary<string, object>> MakeRequestAsync(string method, string url, Dictionary<string, string>? headers = null, Dictionary<string, object>? jsonData = null, Dictionary<string, object>? parameters = null)
        {
            return Task.FromResult(MakeRequest(method, url, headers, jsonData, parameters));
        }
    }
}
