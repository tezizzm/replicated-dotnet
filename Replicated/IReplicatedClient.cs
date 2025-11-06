using System.Collections.Generic;
using System.Threading.Tasks;
using Replicated.Resources;
using Replicated.Services;

namespace Replicated;

/// <summary>
/// Interface for the Replicated client.
/// </summary>
public interface IReplicatedClient
{
    /// <summary>
    /// Gets the publishable key.
    /// </summary>
    string PublishableKey { get; }

    /// <summary>
    /// Gets the app slug.
    /// </summary>
    string AppSlug { get; }

    /// <summary>
    /// Gets the machine ID.
    /// </summary>
    string MachineId { get; }

    /// <summary>
    /// Gets the state manager.
    /// </summary>
    StateManager StateManager { get; }

    /// <summary>
    /// Gets the customer service.
    /// </summary>
    CustomerService Customer { get; }

    /// <summary>
    /// Gets authentication headers for API requests.
    /// </summary>
    /// <returns>A dictionary of authentication headers.</returns>
    Dictionary<string, string> GetAuthHeaders();

    /// <summary>
    /// Makes a synchronous HTTP request.
    /// </summary>
    Dictionary<string, object> MakeRequest(
        string method,
        string url,
        Dictionary<string, string>? headers = null,
        Dictionary<string, object>? jsonData = null,
        Dictionary<string, object>? parameters = null);

    /// <summary>
    /// Makes an asynchronous HTTP request.
    /// </summary>
    Task<Dictionary<string, object>> MakeRequestAsync(
        string method,
        string url,
        Dictionary<string, string>? headers = null,
        Dictionary<string, object>? jsonData = null,
        Dictionary<string, object>? parameters = null);
}

