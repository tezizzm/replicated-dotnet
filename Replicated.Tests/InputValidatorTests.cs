using System.Collections.Generic;
using Replicated.Validation;
using Xunit;

namespace Replicated.Tests;

public class InputValidatorTests
{
    // ── ValidateBaseUrl ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("http://replicated:3000")]
    [InlineData("http://localhost:3000")]
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
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateBaseUrl(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateBaseUrl_WithEmptyOrWhitespace_ShouldThrowArgumentException(string url)
    {
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateBaseUrl(url));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("ftp://example.com")]
    [InlineData("not-a-url")]
    [InlineData("file:///etc/passwd")]
    public void ValidateBaseUrl_WithInvalidScheme_ShouldThrowArgumentException(string url)
    {
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateBaseUrl(url));
        Assert.NotEmpty(exception.Message);
    }

    // ── ValidateTimeout ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(3600)] // 1 hour
    public void ValidateTimeout_WithValidTimeouts_ShouldNotThrow(int seconds)
    {
        InputValidator.ValidateTimeout(TimeSpan.FromSeconds(seconds));
    }

    [Fact]
    public void ValidateTimeout_WithZero_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateTimeout(TimeSpan.Zero));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public void ValidateTimeout_WithNegative_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateTimeout(TimeSpan.FromSeconds(-1)));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public void ValidateTimeout_WithExceedsOneHour_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateTimeout(TimeSpan.FromHours(2)));
        Assert.Contains("cannot exceed 1 hour", exception.Message);
    }

    // ── ValidateUrlPath (internal) ────────────────────────────────────────────

    [Theory]
    [InlineData("/path")]
    [InlineData("/")]
    [InlineData("/very/long/path/with/many/segments")]
    [InlineData("/api/v1/app/info")]
    public void ValidateUrlPath_WithValidPaths_ShouldNotThrow(string path)
    {
        InputValidator.ValidateUrlPath(path);
    }

    [Fact]
    public void ValidateUrlPath_WithNull_ShouldThrowArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateUrlPath(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUrlPath_WithEmptyOrWhitespace_ShouldThrowArgumentException(string path)
    {
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateUrlPath(path));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Theory]
    [InlineData("path")]
    [InlineData("http://example.com/path")]
    [InlineData("no-leading-slash")]
    public void ValidateUrlPath_WithMissingLeadingSlash_ShouldThrowArgumentException(string path)
    {
        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateUrlPath(path));
        Assert.Contains("must start with '/'", exception.Message);
    }

    // ── ValidateHeaders (internal) ────────────────────────────────────────────

    [Fact]
    public void ValidateHeaders_WithNull_ShouldNotThrow()
    {
        // null is explicitly allowed
        InputValidator.ValidateHeaders(null);
    }

    [Fact]
    public void ValidateHeaders_WithValidHeaders_ShouldNotThrow()
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token",
            ["Content-Type"] = "application/json"
        };

        InputValidator.ValidateHeaders(headers);
    }

    [Fact]
    public void ValidateHeaders_WithEmptyKey_ShouldThrowArgumentException()
    {
        var headers = new Dictionary<string, string>
        {
            [""] = "value"
        };

        var exception = Assert.Throws<ArgumentException>(() => InputValidator.ValidateHeaders(headers));
        Assert.NotEmpty(exception.Message);
    }
}
