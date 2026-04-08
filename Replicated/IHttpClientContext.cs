using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Replicated;

/// <summary>
/// Internal interface providing AOT/trim-safe HTTP operations for SDK services.
/// All serialization uses source-generated <see cref="JsonTypeInfo{T}"/> parameters.
/// </summary>
internal interface IHttpClientContext
{
    /// <summary>GET request returning a typed response.</summary>
    Task<TResp> GetAsync<TResp>(string path, JsonTypeInfo<TResp> responseTypeInfo,
        CancellationToken cancellationToken = default);

    /// <summary>POST request with a typed request body and typed response.</summary>
    Task<TResp> PostAsync<TReq, TResp>(
        string path,
        TReq body,
        JsonTypeInfo<TReq> requestTypeInfo,
        JsonTypeInfo<TResp> responseTypeInfo,
        CancellationToken cancellationToken = default);

    /// <summary>POST request with a typed request body and no meaningful response body.</summary>
    Task PostAsync<TReq>(string path, TReq body, JsonTypeInfo<TReq> requestTypeInfo,
        CancellationToken cancellationToken = default);

    /// <summary>PATCH request with a typed request body and no meaningful response body.</summary>
    Task PatchAsync<TReq>(string path, TReq body, JsonTypeInfo<TReq> requestTypeInfo,
        CancellationToken cancellationToken = default);

    /// <summary>DELETE request with no body or response.</summary>
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}
