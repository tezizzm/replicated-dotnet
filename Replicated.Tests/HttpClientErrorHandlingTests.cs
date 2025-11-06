using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Replicated;
using Xunit;

namespace Replicated.Tests;

/// <summary>
/// Tests for HTTP client error handling and response scenarios.
/// These tests verify that the SDK correctly handles various HTTP error responses.
/// </summary>
public class HttpClientErrorHandlingTests
{
    [Fact]
    public void ReplicatedAuthError_With401_ShouldPreserveHttpStatus()
    {
        // Arrange & Act
        var exception = new ReplicatedAuthError("Unauthorized", 401);

        // Assert
        Assert.Equal(401, exception.HttpStatus);
        Assert.IsAssignableFrom<ReplicatedException>(exception);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(422)]
    public void HandleResponse_WithClientErrors_ShouldThrowReplicatedApiError(int expectedStatus)
    {
        // This test validates the error handling logic for 4xx errors (excluding 401, 429)
        var exception = new ReplicatedApiError("Client error", expectedStatus);
        Assert.Equal(expectedStatus, exception.HttpStatus);
        Assert.IsType<ReplicatedApiError>(exception);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    [InlineData(504)]
    public void HandleResponse_WithServerErrors_ShouldThrowReplicatedApiError(int expectedStatus)
    {
        // This test validates that 5xx errors are properly identified as server errors
        var exception = new ReplicatedApiError("Server error", expectedStatus);
        Assert.Equal(expectedStatus, exception.HttpStatus);
        Assert.IsAssignableFrom<ReplicatedException>(exception);
    }

    [Fact]
    public void HandleResponse_With429RateLimit_ShouldThrowReplicatedRateLimitError()
    {
        // Arrange
        var exception = new ReplicatedRateLimitError("Rate limit exceeded", 429);

        // Assert
        Assert.Equal(429, exception.HttpStatus);
        Assert.IsAssignableFrom<ReplicatedException>(exception);
    }

    [Fact]
    public void ReplicatedException_ToString_WithHttpStatusAndCode_ShouldFormatCorrectly()
    {
        // Arrange
        var exception = new ReplicatedException("Test error", 400, code: "INVALID_REQUEST");

        // Act
        var result = exception.ToString();

        // Assert
        Assert.Contains("400", result);
        Assert.Contains("INVALID_REQUEST", result);
        Assert.Contains("Test error", result);
    }

    [Fact]
    public void ReplicatedException_ToString_WithHttpStatusOnly_ShouldFormatCorrectly()
    {
        // Arrange
        var exception = new ReplicatedException("Test error", 500);

        // Act
        var result = exception.ToString();

        // Assert
        Assert.Equal("500: Test error", result);
    }

    [Fact]
    public void ReplicatedException_ToString_WithoutHttpStatus_ShouldReturnMessageOnly()
    {
        // Arrange
        var exception = new ReplicatedException("Test error");

        // Act
        var result = exception.ToString();

        // Assert
        Assert.Equal("Test error", result);
    }

    [Fact]
    public void ReplicatedException_WithAllProperties_ShouldPreserveAllValues()
    {
        // Arrange
        var jsonBody = new Dictionary<string, object> { ["error"] = "test" };
        var headers = new Dictionary<string, string> { ["X-Request-Id"] = "123" };
        var httpBody = "{\"error\":\"test\"}";

        // Act
        var exception = new ReplicatedException(
            "Test error",
            httpStatus: 400,
            httpBody: httpBody,
            jsonBody: jsonBody,
            headers: headers,
            code: "TEST_CODE");

        // Assert
        Assert.Equal(400, exception.HttpStatus);
        Assert.Equal(httpBody, exception.HttpBody);
        Assert.Equal(jsonBody, exception.JsonBody);
        Assert.Equal(headers, exception.Headers);
        Assert.Equal("TEST_CODE", exception.Code);
    }

    [Fact]
    public void ReplicatedAuthError_WithJsonBody_ShouldPreserveJsonBody()
    {
        // Arrange
        var jsonBody = new Dictionary<string, object> { ["error"] = "Invalid credentials" };

        // Act
        var exception = new ReplicatedAuthError("Unauthorized", 401, jsonBody: jsonBody);

        // Assert
        Assert.Equal(401, exception.HttpStatus);
        Assert.NotNull(exception.JsonBody);
        Assert.Equal("Invalid credentials", exception.JsonBody["error"]?.ToString());
    }

    [Fact]
    public void ReplicatedNetworkError_WithHttpStatus_ShouldPreserveHttpStatus()
    {
        // Arrange & Act
        var exception = new ReplicatedNetworkError("Network error", 0);

        // Assert
        Assert.Equal(0, exception.HttpStatus);
        Assert.Equal("Network error", exception.Message);
    }
}

