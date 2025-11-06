using System;
using Xunit;

namespace Replicated.IntegrationTests;

/// <summary>
/// Server fixture for integration tests. 
/// Uses mock server at https://localhost:5001 if available, otherwise skips tests.
/// </summary>
public class ServerFixture : IDisposable
{
    public string? BaseUrl { get; }

    public ServerFixture()
    {
        // Check for custom TEST_BASE_URL first
        BaseUrl = Environment.GetEnvironmentVariable("TEST_BASE_URL");
        
        // If not set, try to use the mock server
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            BaseUrl = "https://localhost:5001";
        }
        
        // Note: We don't throw SkipTestException here because we want tests to run
        // if the mock server is available. Tests will fail with connection errors
        // if the server is not running, which is the expected behavior.
    }

    public void Dispose()
    {
        // No-op: external server managed by the user
    }
}

public sealed class SkipTestException : Exception
{
    public SkipTestException(string message) : base(message) { }
}


