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
/// These tests target real-world scenarios that exercise different branches in HandleResponse.
/// </summary>
public class HttpClientCodePathTests
{
    [Fact]
    public void MakeRequest_WithErrorResponseWithJsonMessageAndCode_ShouldExtractBoth()
    {
        // Exercise: errorMessage and errorCode extraction from JSON body
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                @"{""message"":""Custom error message"",""code"":""CUSTOM_ERROR_CODE""}",
                Encoding.UTF8,
                "application/json")
        });

        var client = CreateHttpClient(handler);

        var exception = Assert.Throws<ReplicatedApiError>(() => client.MakeRequest("GET", "/test"));
        Assert.Equal(400, exception.HttpStatus);
        Assert.Equal("Custom error message", exception.Message);
        Assert.Equal("CUSTOM_ERROR_CODE", exception.Code);
        Assert.NotNull(exception.JsonBody);
    }

    [Fact]
    public void MakeRequest_WithErrorResponseNullMessage_ShouldUseDefaultMessage()
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

        var exception = Assert.Throws<ReplicatedApiError>(() => client.MakeRequest("GET", "/test"));
        Assert.Equal(400, exception.HttpStatus);
        // With null message, default is formatted as "Client error: BadRequest"
        Assert.Contains("BadRequest", exception.Message);
    }

    [Fact]
    public void MakeRequest_WithErrorResponseHasMessageButNoCode_ShouldOnlySetMessage()
    {
        // Exercise: JSON has message but no code
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                @"{""message"":""Resource not found""}",
                Encoding.UTF8,
                "application/json")
        });

        var client = CreateHttpClient(handler);

        var exception = Assert.Throws<ReplicatedApiError>(() => client.MakeRequest("GET", "/test"));
        Assert.Equal(404, exception.HttpStatus);
        Assert.Equal("Resource not found", exception.Message);
        Assert.Null(exception.Code);
    }

    [Fact]
    public void MakeRequest_WithSuccessfulResponseWithNullJson_ShouldReturnEmptyDictionary()
    {
        // Exercise: successful response with null JSON should return empty dict
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

        var client = CreateHttpClient(handler);

        var result = client.MakeRequest("GET", "/test");
        Assert.NotNull(result);
        // Null JSON deserializes to null, which becomes empty dict
    }

    [Fact]
    public async Task MakeRequestAsync_WithSuccessfulResponseWithNullJson_ShouldReturnEmptyDictionary()
    {
        // Exercise: async version with null JSON
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

        var client = CreateHttpClient(handler);

        var result = await client.MakeRequestAsync("GET", "/test");
        Assert.NotNull(result);
    }

    [Fact]
    public void MakeRequest_WithErrorResponseExtractsHeaders_ShouldIncludeInException()
    {
        // Exercise: header extraction for errors
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(@"{""message"":""Error""}", Encoding.UTF8, "application/json")
        };
        response.Headers.Add("X-Custom-Header", "custom-value");

        var handler = new TestHttpMessageHandler(response);
        var client = CreateHttpClient(handler);

        var exception = Assert.Throws<ReplicatedApiError>(() => client.MakeRequest("GET", "/test"));
        Assert.NotNull(exception.Headers);
        // Just check that we have at least one header
        Assert.True(exception.Headers.Count > 0);
        Assert.Contains("X-Custom-Header", exception.Headers.Keys);
    }

    [Fact]
    public async Task MakeRequestAsync_WithErrorResponseExtractsHeaders_ShouldIncludeInException()
    {
        // Exercise: async version of header extraction
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(@"{""message"":""Unauthorized""}", Encoding.UTF8, "application/json")
        };
        response.Headers.Add("WWW-Authenticate", "Bearer");

        var handler = new TestHttpMessageHandler(response);
        var client = CreateHttpClient(handler);

        var exception = await Assert.ThrowsAsync<ReplicatedAuthError>(async () => 
            await client.MakeRequestAsync("GET", "/test"));
        
        Assert.NotNull(exception.Headers);
        Assert.True(exception.Headers.ContainsKey("WWW-Authenticate"));
    }

    [Fact]
    public void MakeRequest_WithServerErrorWithNullErrorMessage_ShouldUseDefaultMessage()
    {
        // Exercise: 5xx error with null error message uses default
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(@"{""message"":null}", Encoding.UTF8, "application/json")
        });

        var client = CreateHttpClient(handler);

        var exception = Assert.Throws<ReplicatedApiError>(() => client.MakeRequest("GET", "/test"));
        Assert.Equal(500, exception.HttpStatus);
        Assert.Contains("Server error", exception.Message);
    }

    [Fact]
    public void MakeRequest_WithBuildHeadersOverride_ShouldMergeDefaultAndProvidedHeaders()
    {
        // Exercise: BuildHeaders method merging default and provided headers
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });

        var defaultHeaders = new Dictionary<string, string>
        {
            ["X-Default"] = "default-value",
            ["X-Shared"] = "default-shared"
        };

        // Need to test BuildHeaders indirectly through request
        var client = new ReplicatedHttpClientAsync(
            "https://test.replicated.app",
            TimeSpan.FromSeconds(30),
            defaultHeaders,
            new RetryPolicy { MaxRetries = 0 });

        Dictionary<string, string>? capturedHeaders = null;
        var testHandler = new TestHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            },
            request =>
            {
                capturedHeaders = new Dictionary<string, string>();
                foreach (var header in request.Headers)
                {
                    capturedHeaders[header.Key] = string.Join(", ", header.Value);
                }
            });

        var field = typeof(ReplicatedHttpClientAsync).GetField(
            "_httpClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(client, new HttpClient(testHandler));

        var providedHeaders = new Dictionary<string, string>
        {
            ["X-Provided"] = "provided-value",
            ["X-Shared"] = "provided-shared" // Should override default
        };

        client.MakeRequest("GET", "/test", providedHeaders, null, null);

        // Assert: provided headers override defaults, both are present
        Assert.NotNull(capturedHeaders);
        Assert.Equal("provided-shared", capturedHeaders["X-Shared"]);
        Assert.Equal("default-value", capturedHeaders["X-Default"]);
        Assert.Equal("provided-value", capturedHeaders["X-Provided"]);
    }

    [Fact]
    public void MakeRequest_WithEmptyParameters_ShouldNotAddQueryString()
    {
        // Exercise: BuildQueryString with empty/null parameters
        string? capturedQuery = null;
        var handler = new TestHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            },
            request => capturedQuery = request.RequestUri?.Query);

        var client = CreateHttpClient(handler);

        client.MakeRequest("GET", "/test", null, null, new Dictionary<string, object>());

        // Assert: empty dictionary should result in no query string
        Assert.True(string.IsNullOrEmpty(capturedQuery) || capturedQuery == "");
    }

    [Fact]
    public void MakeRequest_WithNullParameters_ShouldNotAddQueryString()
    {
        // Exercise: BuildQueryString with null parameters
        string? capturedQuery = null;
        var handler = new TestHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            },
            request => capturedQuery = request.RequestUri?.Query);

        var client = CreateHttpClient(handler);

        client.MakeRequest("GET", "/test", null, null, null);

        // Assert: null parameters should result in no query string
        Assert.True(string.IsNullOrEmpty(capturedQuery) || capturedQuery == "");
    }

    private static ReplicatedHttpClientAsync CreateHttpClient(TestHttpMessageHandler handler)
    {
        var client = new ReplicatedHttpClientAsync(
            "https://test.replicated.app",
            TimeSpan.FromSeconds(30),
            null,
            new RetryPolicy { MaxRetries = 0 });

        var field = typeof(ReplicatedHttpClientAsync).GetField(
            "_httpClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(client, new HttpClient(handler));
        }

        return client;
    }

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
            return Task.FromResult(Send(request, cancellationToken));
        }

        protected override HttpResponseMessage Send(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            _requestCallback?.Invoke(request);

            if (_responseFactory != null)
            {
                return _responseFactory(request);
            }

            return _response!;
        }
    }
}

