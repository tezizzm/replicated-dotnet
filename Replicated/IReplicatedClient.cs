using Replicated.Services;

namespace Replicated;

/// <summary>
/// Public interface for the Replicated in-cluster SDK client.
/// </summary>
public interface IReplicatedClient
{
    /// <summary>The base URL used to reach the Replicated in-cluster service.</summary>
    string BaseUrl { get; }

    /// <summary>Application endpoints — info, status, updates, metrics, tags.</summary>
    AppService App { get; }

    /// <summary>License endpoints — info, fields.</summary>
    LicenseService License { get; }
}
