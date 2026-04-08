using System;
using System.Collections.Generic;
using System.Net.Http;
using Replicated;
using Xunit;

namespace Replicated.Tests;

public class ExceptionExtendedTests
{
    [Fact]
    public void ReplicatedRateLimitError_ShouldInheritFromReplicatedException()
    {
        // Act
        var exception = new ReplicatedRateLimitError("Rate limit exceeded", 429);

        // Assert
        Assert.IsAssignableFrom<ReplicatedException>(exception);
        Assert.Equal("Rate limit exceeded", exception.Message);
        Assert.Equal(429, exception.HttpStatus);
    }

    [Fact]
    public void ReplicatedRateLimitError_WithJsonBody_ShouldPreserveJsonBody()
    {
        // Arrange
        var jsonBody = new Dictionary<string, object> { ["retry_after"] = 60 };

        // Act
        var exception = new ReplicatedRateLimitError("Rate limit exceeded", 429, jsonBody: jsonBody);

        // Assert
        Assert.Equal("Rate limit exceeded", exception.Message);
        Assert.Equal(429, exception.HttpStatus);
        Assert.NotNull(exception.JsonBody);
        Assert.Equal(60, exception.JsonBody["retry_after"]);
    }

    [Fact]
    public void ReplicatedRateLimitError_WithCode_ShouldPreserveCode()
    {
        // Arrange
        var code = "RATE_LIMIT_EXCEEDED";

        // Act
        var exception = new ReplicatedRateLimitError("Rate limit exceeded", 429, code: code);

        // Assert
        Assert.Equal("Rate limit exceeded", exception.Message);
        Assert.Equal(429, exception.HttpStatus);
        Assert.Equal(code, exception.Code);
    }

    [Fact]
    public void ReplicatedNetworkError_WithMessage_ShouldPreserveMessage()
    {
        // Act
        var exception = new ReplicatedNetworkError("Network error occurred");

        // Assert
        Assert.Equal("Network error occurred", exception.Message);
        Assert.IsAssignableFrom<ReplicatedException>(exception);
    }

    [Fact]
    public void ReplicatedApiError_WithEmptyJsonBody_ShouldHandleGracefully()
    {
        // Act
        var exception = new ReplicatedApiError("API error", 400, jsonBody: new Dictionary<string, object>());

        // Assert
        Assert.Equal("API error", exception.Message);
        Assert.Equal(400, exception.HttpStatus);
        Assert.NotNull(exception.JsonBody);
        Assert.Empty(exception.JsonBody);
    }

    [Fact]
    public void ReplicatedApiError_WithComplexJsonBody_ShouldPreserveJsonBody()
    {
        // Arrange
        var jsonBody = new Dictionary<string, object>
        {
            ["error"] = "Validation failed",
            ["details"] = new Dictionary<string, object>
            {
                ["field"] = "email",
                ["message"] = "Invalid format"
            },
            ["code"] = "INVALID_EMAIL"
        };

        // Act
        var exception = new ReplicatedApiError("API error", 400, jsonBody: jsonBody);

        // Assert
        Assert.Equal("API error", exception.Message);
        Assert.Equal(400, exception.HttpStatus);
        Assert.NotNull(exception.JsonBody);
        Assert.Equal("Validation failed", exception.JsonBody["error"]?.ToString());
    }

    [Fact]
    public void ReplicatedAuthError_WithCustomMessage_ShouldPreserveMessage()
    {
        // Act
        var exception = new ReplicatedAuthError("Custom auth error message");

        // Assert
        Assert.Equal("Custom auth error message", exception.Message);
        Assert.IsAssignableFrom<ReplicatedException>(exception);
    }

    [Fact]
    public void ReplicatedException_WithHttpStatusZero_ShouldAcceptZero()
    {
        // Act
        var exception = new ReplicatedException("Error", 0);

        // Assert
        Assert.Equal("Error", exception.Message);
        Assert.Equal(0, exception.HttpStatus);
    }

    [Fact]
    public void ReplicatedException_WithLargeHttpStatus_ShouldAcceptLargeValue()
    {
        // Arrange
        var largeStatus = 599;

        // Act
        var exception = new ReplicatedException("Error", largeStatus);

        // Assert
        Assert.Equal("Error", exception.Message);
        Assert.Equal(largeStatus, exception.HttpStatus);
    }
}

