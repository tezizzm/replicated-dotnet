using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Replicated;
using Xunit;

namespace Replicated.Tests;

/// <summary>
/// Tests that execute actual HTTP client code paths to improve coverage.
/// Uses HttpMessageHandler to mock HTTP responses and test TypedPostAsync methods.
/// </summary>
public class HttpClientExecutionTests
{
    [Fact]
    public async Task TypedPostAsync_WithSuccessfulJsonResponse_ShouldParseJson()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                @"{""instanceID"":""123"",""appSlug"":""my-app"",""appName"":""My Application""}",
                Encoding.UTF8,
                "application/json")
        });

        var client = CreateHttpClient(handler);

        // Act
        var result = await client.TypedPostAsync(
            "/api/v1/app/instance-tags",
            new InstanceTagsRequest(false, new Dictionary<string, string>()),
            ReplicatedJsonContext.Default.InstanceTagsRequest,
            ReplicatedJsonContext.Default.AppInfo);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("123", result.InstanceId);
        Assert.Equal("my-app", result.AppSlug);
    }

    [Fact]
    public async Task TypedPostAsync_WithSuccessfulEmptyResponse_ShouldReturnDefault()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        });

        var client = CreateHttpClient(handler);

        // Act
        var result = await client.TypedPostAsync(
            "/api/v1/app/instance-tags",
            new InstanceTagsRequest(false, new Dictionary<string, string>()),
            ReplicatedJsonContext.Default.InstanceTagsRequest,
            ReplicatedJsonContext.Default.AppInfo);

        // Assert: empty body returns default (null record)
        Assert.Null(result);
    }

    [Fact]
    public async Task TypedPostAsync_WithUnauthorizedError_ShouldThrowReplicatedAuthError()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                @"{""message"":""Invalid credentials"",""code"":""AUTH_FAILED""}",
                Encoding.UTF8,
                "application/json")
        });

        var client = CreateHttpClient(handler);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ReplicatedAuthError>(async () =>
            await client.TypedPostAsync(
                "/api/v1/app/instance-tags",
                new InstanceTagsRequest(false, new Dictionary<string, string>()),
                ReplicatedJsonContext.Default.InstanceTagsRequest,
                ReplicatedJsonContext.Default.AppInfo));

        Assert.Equal(401, exception.HttpStatus);
        Assert.Equal("Invalid credentials", exception.Message);
        Assert.Equal("AUTH_FAILED", exception.Code);
    }

    [Fact]
    public async Task TypedPostAsync_WithRateLimitError_ShouldThrowReplicatedRateLimitError()
    {
        // Arrange
        var response = new HttpResponseMessage((HttpStatusCode)429)
        {
            Content = new StringContent(
                @"{""message"":""Rate limit exceeded""}",
                Encoding.UTF8,
                "application/json")
        };
        response.Headers.Add("Retry-After", "60");
        var handler = new TestHttpMessageHandler(response);

        var client = CreateHttpClient(handler);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ReplicatedRateLimitError>(async () =>
            await client.TypedPostAsync(
                "/api/v1/app/instance-tags",
                new InstanceTagsRequest(false, new Dictionary<string, string>()),
                ReplicatedJsonContext.Default.InstanceTagsRequest,
                ReplicatedJsonContext.Default.AppInfo));

        Assert.Equal(429, exception.HttpStatus);
        Assert.Equal("Rate limit exceeded", exception.Message);
    }

    [Fact]
    public async Task TypedPostAsync_WithServerError_ShouldThrowReplicatedApiError()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(
                @"{""message"":""Server error occurred""}",
                Encoding.UTF8,
                "application/json")
        });

        var client = CreateHttpClient(handler);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.TypedPostAsync(
                "/api/v1/app/instance-tags",
                new InstanceTagsRequest(false, new Dictionary<string, string>()),
                ReplicatedJsonContext.Default.InstanceTagsRequest,
                ReplicatedJsonContext.Default.AppInfo));

        Assert.Equal(500, exception.HttpStatus);
        Assert.Equal("Server error occurred", exception.Message);
    }

    [Fact]
    public async Task TypedPostAsync_WithClientError_ShouldThrowReplicatedApiError()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                @"{""message"":""Bad request""}",
                Encoding.UTF8,
                "application/json")
        });

        var client = CreateHttpClient(handler);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.TypedPostAsync(
                "/api/v1/app/instance-tags",
                new InstanceTagsRequest(false, new Dictionary<string, string>()),
                ReplicatedJsonContext.Default.InstanceTagsRequest,
                ReplicatedJsonContext.Default.AppInfo));

        Assert.Equal(400, exception.HttpStatus);
        Assert.Equal("Bad request", exception.Message);
    }

    [Fact]
    public async Task TypedPostAsync_WithErrorResponseWithoutJsonMessage_ShouldUseDefaultMessage()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        });

        var client = CreateHttpClient(handler);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.TypedPostAsync(
                "/api/v1/app/instance-tags",
                new InstanceTagsRequest(false, new Dictionary<string, string>()),
                ReplicatedJsonContext.Default.InstanceTagsRequest,
                ReplicatedJsonContext.Default.AppInfo));

        Assert.Equal(400, exception.HttpStatus);
        Assert.Contains("HTTP", exception.Message);
    }

    [Fact]
    public async Task TypedPostAsync_WithJsonBody_ShouldSerializeRequestBody()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new TestHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            },
            request =>
            {
                if (request.Content != null)
                {
                    capturedBody = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
            });

        var client = CreateHttpClient(handler);

        // Act
        await client.TypedPostAsync(
            "/api/v1/app/instance-tags",
            new InstanceTagsRequest(true, new Dictionary<string, string> { ["env"] = "prod", ["region"] = "us-east-1" }),
            ReplicatedJsonContext.Default.InstanceTagsRequest,
            ReplicatedJsonContext.Default.AppInfo);

        // Assert
        Assert.NotNull(capturedBody);
        Assert.Contains("env", capturedBody);
        Assert.Contains("prod", capturedBody);
        Assert.Contains("region", capturedBody);
    }

    [Fact]
    public async Task TypedPostAsync_WithHttpRequestException_ShouldThrowReplicatedNetworkError()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            _ => throw new HttpRequestException("Network error"));

        var client = CreateHttpClient(handler);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ReplicatedNetworkError>(async () =>
            await client.TypedPostAsync(
                "/api/v1/app/instance-tags",
                new InstanceTagsRequest(false, new Dictionary<string, string>()),
                ReplicatedJsonContext.Default.InstanceTagsRequest,
                ReplicatedJsonContext.Default.AppInfo));

        Assert.Contains("Network error", exception.Message);
    }

    private static ReplicatedHttpClientAsync CreateHttpClient(TestHttpMessageHandler handler)
        => new ReplicatedHttpClientAsync(
            "http://test-replicated:3000",
            TimeSpan.FromSeconds(30),
            handler,
            new RetryPolicy { MaxRetries = 0 });

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _response;
        private readonly Func<HttpRequestMessage, HttpResponseMessage>? _responseFactory;
        private readonly Action<HttpRequestMessage>? _requestCallback;

        public TestHttpMessageHandler(
            HttpResponseMessage response,
            Action<HttpRequestMessage>? requestCallback = null)
        {
            _response = response;
            _requestCallback = requestCallback;
        }

        public TestHttpMessageHandler(
            Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _requestCallback?.Invoke(request);

            if (_responseFactory != null)
            {
                return Task.FromResult(_responseFactory(request));
            }

            return Task.FromResult(_response!);
        }
    }
}
