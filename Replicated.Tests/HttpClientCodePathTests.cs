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
/// Tests that exercise specific code paths in HTTP client error handling.
/// These tests target real-world scenarios that exercise different branches in HandleTypedResponseAsync
/// and ThrowForStatus.
/// </summary>
public class HttpClientCodePathTests
{
    [Fact]
    public async Task TypedPostAsync_WithErrorResponseWithJsonMessageAndCode_ShouldExtractBoth()
    {
        // Exercise: message and code extraction from JSON error body
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                @"{""message"":""Custom error message"",""code"":""CUSTOM_ERROR_CODE""}",
                Encoding.UTF8,
                "application/json")
        });

        var client = CreateHttpClient(handler);

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.TypedPostAsync(
                "/api/v1/app/instance-tags",
                new InstanceTagsRequest(false, new Dictionary<string, string>()),
                ReplicatedJsonContext.Default.InstanceTagsRequest,
                ReplicatedJsonContext.Default.AppInfo));

        Assert.Equal(400, exception.HttpStatus);
        Assert.Equal("Custom error message", exception.Message);
        Assert.Equal("CUSTOM_ERROR_CODE", exception.Code);
        // AOT path: JsonBody is null (only message/code are parsed from ErrorResponse)
        Assert.Null(exception.JsonBody);
    }

    [Fact]
    public async Task TypedPostAsync_WithErrorResponseNullMessage_ShouldUseDefaultMessage()
    {
        // Exercise: null message in JSON should fall back to default
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                @"{""message"":null,""code"":""ERROR""}",
                Encoding.UTF8,
                "application/json")
        });

        var client = CreateHttpClient(handler);

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.TypedPostAsync(
                "/api/v1/app/instance-tags",
                new InstanceTagsRequest(false, new Dictionary<string, string>()),
                ReplicatedJsonContext.Default.InstanceTagsRequest,
                ReplicatedJsonContext.Default.AppInfo));

        Assert.Equal(400, exception.HttpStatus);
        // With null message, default is "HTTP 400"
        Assert.Contains("HTTP", exception.Message);
    }

    [Fact]
    public async Task TypedPostAsync_WithErrorResponseHasMessageButNoCode_ShouldOnlySetMessage()
    {
        // Exercise: JSON has message but no code field
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                @"{""message"":""Resource not found""}",
                Encoding.UTF8,
                "application/json")
        });

        var client = CreateHttpClient(handler);

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.TypedPostAsync(
                "/api/v1/app/instance-tags",
                new InstanceTagsRequest(false, new Dictionary<string, string>()),
                ReplicatedJsonContext.Default.InstanceTagsRequest,
                ReplicatedJsonContext.Default.AppInfo));

        Assert.Equal(404, exception.HttpStatus);
        Assert.Equal("Resource not found", exception.Message);
        Assert.Null(exception.Code);
    }

    [Fact]
    public async Task TypedPostAsync_WithSuccessfulResponseWithNullJson_ShouldReturnDefault()
    {
        // Exercise: successful response with JSON null returns default
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

        var client = CreateHttpClient(handler);

        var result = await client.TypedPostAsync(
            "/api/v1/app/instance-tags",
            new InstanceTagsRequest(false, new Dictionary<string, string>()),
            ReplicatedJsonContext.Default.InstanceTagsRequest,
            ReplicatedJsonContext.Default.AppInfo);

        // null JSON deserializes to null for a record type
        Assert.Null(result);
    }

    [Fact]
    public async Task TypedPostAsync_WithServerErrorWithNullErrorMessage_ShouldUseDefaultMessage()
    {
        // Exercise: 5xx error with null error message uses default
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(@"{""message"":null}", Encoding.UTF8, "application/json")
        });

        var client = CreateHttpClient(handler);

        var exception = await Assert.ThrowsAsync<ReplicatedApiError>(async () =>
            await client.TypedPostAsync(
                "/api/v1/app/instance-tags",
                new InstanceTagsRequest(false, new Dictionary<string, string>()),
                ReplicatedJsonContext.Default.InstanceTagsRequest,
                ReplicatedJsonContext.Default.AppInfo));

        Assert.Equal(500, exception.HttpStatus);
        // Default message for 500 is "HTTP 500"
        Assert.Contains("HTTP", exception.Message);
    }

    [Fact]
    public async Task TypedPostAsync_WithNetworkError_ShouldThrowReplicatedNetworkError()
    {
        // Exercise: HttpRequestException is wrapped as ReplicatedNetworkError
        var handler = new TestHttpMessageHandler(
            _ => throw new HttpRequestException("Connection refused"));

        var client = CreateHttpClient(handler);

        var exception = await Assert.ThrowsAsync<ReplicatedNetworkError>(async () =>
            await client.TypedPostAsync(
                "/api/v1/app/instance-tags",
                new InstanceTagsRequest(false, new Dictionary<string, string>()),
                ReplicatedJsonContext.Default.InstanceTagsRequest,
                ReplicatedJsonContext.Default.AppInfo));

        Assert.Contains("Connection refused", exception.Message);
    }

    [Fact]
    public async Task TypedPostAsync_WithUnauthorizedError_ShouldThrowReplicatedAuthError()
    {
        // Exercise: 401 → ReplicatedAuthError with AOT-parsed message
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                @"{""message"":""Unauthorized"",""code"":""AUTH_FAILED""}",
                Encoding.UTF8,
                "application/json")
        });

        var client = CreateHttpClient(handler);

        var exception = await Assert.ThrowsAsync<ReplicatedAuthError>(async () =>
            await client.TypedPostAsync(
                "/api/v1/app/instance-tags",
                new InstanceTagsRequest(false, new Dictionary<string, string>()),
                ReplicatedJsonContext.Default.InstanceTagsRequest,
                ReplicatedJsonContext.Default.AppInfo));

        Assert.Equal(401, exception.HttpStatus);
        Assert.Equal("Unauthorized", exception.Message);
        // AOT path: no dict-based JsonBody
        Assert.Null(exception.JsonBody);
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
