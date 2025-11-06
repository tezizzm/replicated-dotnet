using Replicated.Validation;
using Xunit;

namespace Replicated.Tests;

public class InputValidatorTests
{
    [Theory]
    [InlineData("replicated_pk_abc123")]
    [InlineData("replicated_pk_test-key_123")]
    [InlineData("replicated_pk_a")]
    [InlineData("replicated_pk_" + "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void ValidatePublishableKey_WithValidKeys_ShouldNotThrow(string key)
    {
        // Act & Assert - Should not throw
        InputValidator.ValidatePublishableKey(key);
    }

    [Fact]
    public void ValidatePublishableKey_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidatePublishableKey(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidatePublishableKey_WithEmpty_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidatePublishableKey(""));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidatePublishableKey_WithWhitespace_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidatePublishableKey("   "));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("invalid_key")]
    [InlineData("replicated_sk_")]
    [InlineData("pk_test")]
    [InlineData("replicated_pk_ test")]
    [InlineData("replicated_pk_test@invalid")]
    public void ValidatePublishableKey_WithInvalidFormat_ShouldThrowArgumentException(string key)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidatePublishableKey(key));
        Assert.Contains("must start with 'replicated_pk_'", exception.Message);
    }

    [Theory]
    [InlineData("test_app")]
    [InlineData("test-app")]
    [InlineData("test_app_123")]
    [InlineData("a")]
    [InlineData("123")]
    [InlineData("TestApp")]
    public void ValidateAppSlug_WithValidSlugs_ShouldNotThrow(string slug)
    {
        // Act & Assert - Should not throw
        InputValidator.ValidateAppSlug(slug);
    }

    [Fact]
    public void ValidateAppSlug_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateAppSlug(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("test app")]
    [InlineData("test@app")]
    [InlineData("test.app")]
    public void ValidateAppSlug_WithInvalidFormat_ShouldThrowArgumentException(string slug)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateAppSlug(slug));
        Assert.Contains("must contain only alphanumeric characters", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateAppSlug_WithEmptyOrWhitespace_ShouldThrowArgumentException(string slug)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateAppSlug(slug));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("https://replicated.app")]
    [InlineData("https://custom.replicated.app")]
    [InlineData("https://example.com/v1")]
    [InlineData("https://example.com:443")]
    public void ValidateBaseUrl_WithValidUrls_ShouldNotThrow(string url)
    {
        // Act & Assert - Should not throw
        InputValidator.ValidateBaseUrl(url);
    }

    [Fact]
    public void ValidateBaseUrl_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateBaseUrl(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("ftp://example.com")]
    [InlineData("not-a-url")]
    public void ValidateBaseUrl_WithInvalidUrl_ShouldThrowArgumentException(string url)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateBaseUrl(url));
        Assert.True(exception.Message.Contains("must use HTTPS") || exception.Message.Contains("must be a valid"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateBaseUrl_WithEmptyOrWhitespace_ShouldThrowArgumentException(string url)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateBaseUrl(url));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(3600)] // 1 hour
    public void ValidateTimeout_WithValidTimeouts_ShouldNotThrow(int seconds)
    {
        // Act & Assert - Should not throw
        InputValidator.ValidateTimeout(TimeSpan.FromSeconds(seconds));
    }

    [Fact]
    public void ValidateTimeout_WithZero_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateTimeout(TimeSpan.Zero));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public void ValidateTimeout_WithNegative_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateTimeout(TimeSpan.FromSeconds(-1)));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public void ValidateTimeout_WithExceedsOneHour_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateTimeout(TimeSpan.FromHours(2)));
        Assert.Contains("cannot exceed 1 hour", exception.Message);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("a@b.co")]
    public void ValidateAndSanitizeEmail_WithValidEmails_ShouldReturnSanitized(string email)
    {
        // Act
        var result = InputValidator.ValidateAndSanitizeEmail(email);

        // Assert
        Assert.Equal(email.ToLowerInvariant().Trim(), result);
    }

    [Fact]
    public void ValidateAndSanitizeEmail_ShouldTrimAndLowercase()
    {
        // Arrange
        var email = "  TEST@EXAMPLE.COM  ";

        // Act
        var result = InputValidator.ValidateAndSanitizeEmail(email);

        // Assert
        Assert.Equal("test@example.com", result);
    }

    [Fact]
    public void ValidateAndSanitizeEmail_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateAndSanitizeEmail(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidateAndSanitizeEmail_WithEmpty_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateAndSanitizeEmail(""));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidateAndSanitizeEmail_WithMissingAtSymbol_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateAndSanitizeEmail("invalidemail"));
        Assert.Contains("Invalid email address format", exception.Message);
    }

    [Fact]
    public void ValidateAndSanitizeEmail_WithMultipleAtSymbols_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateAndSanitizeEmail("test@@example.com"));
        Assert.Contains("Invalid email address format", exception.Message);
    }

    [Fact]
    public void ValidateAndSanitizeEmail_WithEmptyLocalPart_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateAndSanitizeEmail("@example.com"));
        Assert.Contains("Invalid email address format", exception.Message);
    }

    [Fact]
    public void ValidateAndSanitizeEmail_WithEmptyDomain_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateAndSanitizeEmail("test@"));
        Assert.Contains("Invalid email address format", exception.Message);
    }

    [Fact]
    public void ValidateAndSanitizeEmail_WithTooLong_ShouldThrowArgumentException()
    {
        // Arrange - Email over 254 chars (RFC 5321 limit)
        var longLocal = new string('a', 250);
        var email = $"{longLocal}@example.com"; // 262 chars total

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateAndSanitizeEmail(email));
        Assert.Contains("too long", exception.Message);
    }

    [Theory]
    [InlineData("Stable")]
    [InlineData("beta")]
    [InlineData("Alpha Channel")]
    [InlineData("test_channel-123")]
    [InlineData("a")]
    public void ValidateChannel_WithValidChannels_ShouldNotThrow(string channel)
    {
        // Act & Assert - Should not throw
        InputValidator.ValidateChannel(channel);
    }

    [Fact]
    public void ValidateChannel_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateChannel(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("test@channel")]
    [InlineData("test.channel")]
    public void ValidateChannel_WithInvalidFormat_ShouldThrowArgumentException(string channel)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateChannel(channel));
        Assert.Contains("can only contain alphanumeric", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateChannel_WithEmptyOrWhitespace_ShouldThrowArgumentException(string channel)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateChannel(channel));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Valid Name")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void ValidateCustomerName_WithValidNames_ShouldNotThrow(string? name)
    {
        // Act & Assert - Should not throw
        InputValidator.ValidateCustomerName(name);
    }

    [Fact]
    public void ValidateCustomerName_WithTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var longName = new string('a', 256);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateCustomerName(longName));
        Assert.Contains("cannot exceed 255 characters", exception.Message);
    }

    [Theory]
    [InlineData("valid_metric")]
    [InlineData("metric123")]
    [InlineData("validMetric")]
    [InlineData("a")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void ValidateMetricName_WithValidNames_ShouldNotThrow(string metricName)
    {
        // Act & Assert - Should not throw
        InputValidator.ValidateMetricName(metricName);
    }

    [Fact]
    public void ValidateMetricName_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateMetricName(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidateMetricName_WithTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var longName = new string('a', 101);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateMetricName(longName));
        Assert.Contains("cannot exceed 100 characters", exception.Message);
    }

    [Theory]
    [InlineData("metric-name")]
    [InlineData("metric.name")]
    [InlineData("metric name")]
    [InlineData("metric@name")]
    public void ValidateMetricName_WithInvalidFormat_ShouldThrowArgumentException(string metricName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateMetricName(metricName));
        Assert.Contains("can only contain alphanumeric characters and underscores", exception.Message);
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("1.0.0-beta")]
    [InlineData("v1.0.0_alpha")]
    [InlineData("a")]
    [InlineData("11111111111111111111111111111111111111111111111111")]
    public void ValidateVersion_WithValidVersions_ShouldNotThrow(string version)
    {
        // Act & Assert - Should not throw
        InputValidator.ValidateVersion(version);
    }

    [Fact]
    public void ValidateVersion_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateVersion(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidateVersion_WithTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var longVersion = new string('1', 51);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateVersion(longVersion));
        Assert.Contains("cannot exceed 50 characters", exception.Message);
    }

    [Theory]
    [InlineData("version@invalid")]
    [InlineData("version#invalid")]
    [InlineData("version!invalid")]
    public void ValidateVersion_WithInvalidFormat_ShouldThrowArgumentException(string version)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateVersion(version));
        Assert.Contains("can only contain alphanumeric characters, dots, underscores, and hyphens", exception.Message);
    }

    [Theory]
    [InlineData("running")]
    [InlineData("RUNNING")]
    [InlineData("Running")]
    [InlineData("degraded")]
    [InlineData("missing")]
    [InlineData("unavailable")]
    [InlineData("ready")]
    [InlineData("updating")]
    public void ValidateInstanceStatus_WithValidStatuses_ShouldNotThrow(string status)
    {
        // Act & Assert - Should not throw
        InputValidator.ValidateInstanceStatus(status);
    }

    [Fact]
    public void ValidateInstanceStatus_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateInstanceStatus(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("stopped")]
    public void ValidateInstanceStatus_WithInvalidStatus_ShouldThrowArgumentException(string status)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateInstanceStatus(status));
        Assert.Contains("Invalid status", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateInstanceStatus_WithEmptyOrWhitespace_ShouldThrowArgumentException(string status)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateInstanceStatus(status));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public void ValidateHttpMethod_WithValidMethods_ShouldNotThrow(string method)
    {
        // Act & Assert - Should not throw
        InputValidator.ValidateHttpMethod(method);
    }

    [Theory]
    [InlineData("get")]
    [InlineData("post")]
    [InlineData("Get")]
    public void ValidateHttpMethod_WithCaseInsensitive_ShouldNotThrow(string method)
    {
        // Act & Assert - Should not throw (case-insensitive)
        InputValidator.ValidateHttpMethod(method);
    }

    [Fact]
    public void ValidateHttpMethod_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateHttpMethod(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("CONNECT")]
    public void ValidateHttpMethod_WithInvalidMethod_ShouldThrowArgumentException(string method)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateHttpMethod(method));
        Assert.Contains("must be one of", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateHttpMethod_WithEmptyOrWhitespace_ShouldThrowArgumentException(string method)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateHttpMethod(method));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("/path")]
    [InlineData("/")]
    [InlineData("/very/long/path/with/many/segments")]
    public void ValidateUrlPath_WithValidPaths_ShouldNotThrow(string path)
    {
        // Act & Assert - Should not throw
        InputValidator.ValidateUrlPath(path);
    }

    [Fact]
    public void ValidateUrlPath_WithNull_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateUrlPath(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("path")]
    [InlineData("http://example.com/path")]
    public void ValidateUrlPath_WithInvalidPath_ShouldThrowArgumentException(string path)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateUrlPath(path));
        Assert.Contains("must start with '/'", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUrlPath_WithEmptyOrWhitespace_ShouldThrowArgumentException(string path)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateUrlPath(path));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidateHeaders_WithNull_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        InputValidator.ValidateHeaders(null);
    }

    [Fact]
    public void ValidateHeaders_WithValidHeaders_ShouldNotThrow()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token",
            ["Content-Type"] = "application/json"
        };

        // Act & Assert - Should not throw
        InputValidator.ValidateHeaders(headers);
    }

    [Fact]
    public void ValidateHeaders_WithEmptyKey_ShouldThrowArgumentException()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            [""] = "value"
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateHeaders(headers));
        Assert.Contains("Header key cannot be null or empty", exception.Message);
    }

    [Fact]
    public void ValidateHeaders_WithNullValue_ShouldThrowArgumentException()
    {
        // Arrange
        var headers = new Dictionary<string, string?>
        {
            ["Header"] = null
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateHeaders(headers!));
        Assert.Contains("cannot be null", exception.Message);
    }

    [Fact]
    public void ValidateParameters_WithNull_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        InputValidator.ValidateParameters(null);
    }

    [Fact]
    public void ValidateParameters_WithValidParameters_ShouldNotThrow()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["param1"] = "value1",
            ["param2"] = 123
        };

        // Act & Assert - Should not throw
        InputValidator.ValidateParameters(parameters);
    }

    [Fact]
    public void ValidateParameters_WithEmptyKey_ShouldThrowArgumentException()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            [""] = "value"
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateParameters(parameters));
        Assert.Contains("Parameter key cannot be null or empty", exception.Message);
    }
}

