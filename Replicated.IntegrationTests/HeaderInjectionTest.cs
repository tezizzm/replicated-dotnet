using System.Net.Http;
using System.Threading.Tasks;
using Replicated;
using Xunit;

namespace Replicated.IntegrationTests;

/// <summary>
/// Tests to verify header injection is working correctly.
/// </summary>
public class HeaderInjectionTest : IntegrationTestBase, IClassFixture<ServerFixture>
{
    public HeaderInjectionTest(ServerFixture server) : base(server)
    {
    }

    [Fact]
    public void HeaderInjection_ShouldWork()
    {
        // Create client with status code injection
        var client = CreateClient("401");

        // Navigate the reflection chain to the underlying System.Net.Http.HttpClient
        var asyncClientField = typeof(ReplicatedClient).GetField("_httpClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var asyncClient = asyncClientField?.GetValue(client);
        Assert.NotNull(asyncClient);

        var coreField = asyncClient!.GetType().GetField("_core",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var core = coreField?.GetValue(asyncClient);
        Assert.NotNull(core);

        var httpClientProp = core!.GetType().GetProperty("HttpClientInstance",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var httpClient = httpClientProp?.GetValue(core) as HttpClient;
        Assert.NotNull(httpClient);

        // Verify X-Test-Status is present in DefaultRequestHeaders
        Assert.True(httpClient!.DefaultRequestHeaders.Contains("X-Test-Status"),
            "X-Test-Status header should be present in DefaultRequestHeaders.");
        Assert.Equal("401", string.Join(",", httpClient.DefaultRequestHeaders.GetValues("X-Test-Status")));
    }

    [Fact]
    public async Task HeaderInjection_ShouldBeSentInActualRequest()
    {
        // Create client with status code injection
        var client = CreateClient("401");

        // Make an actual request — this should throw ReplicatedAuthError if header is sent.
        // If header is NOT sent, it will return 200 (success) and no exception will be thrown.
        var exception = await Assert.ThrowsAsync<ReplicatedAuthError>(async () =>
            await client.App.GetInfoAsync());

        Assert.Equal(401, exception.HttpStatus);
        Assert.Contains("Unauthorized", exception.Message);
    }
}
