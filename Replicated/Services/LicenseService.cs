using System;
using System.Threading;
using System.Threading.Tasks;

namespace Replicated.Services;

/// <summary>
/// Service for interacting with the license endpoints of the in-cluster
/// Replicated SDK API (<c>/api/v1/license/...</c>).
/// </summary>
public class LicenseService
{
    private readonly IHttpClientContext _context;

    internal LicenseService(IHttpClientContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <summary>
    /// Returns license information for the current installation.
    /// Calls GET /api/v1/license/info.
    /// </summary>
    public Task<LicenseInfo> GetInfoAsync(CancellationToken cancellationToken = default)
        => _context.GetAsync(Constants.LicenseInfoEndpoint,
            ReplicatedJsonContext.Default.LicenseInfo, cancellationToken);

    /// <summary>
    /// Returns all license fields.
    /// Calls GET /api/v1/license/fields.
    /// </summary>
    public Task<LicenseField[]> GetFieldsAsync(CancellationToken cancellationToken = default)
        => _context.GetAsync(Constants.LicenseFieldsEndpoint,
            ReplicatedJsonContext.Default.LicenseFieldArray, cancellationToken);

    /// <summary>
    /// Returns a specific license field by name.
    /// Calls GET /api/v1/license/fields/{fieldName}.
    /// </summary>
    /// <param name="fieldName">The name of the license field.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    public Task<LicenseField> GetFieldAsync(string fieldName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            throw new ArgumentException("Field name cannot be null or empty.", nameof(fieldName));
        return _context.GetAsync(
            $"{Constants.LicenseFieldsEndpoint}/{Uri.EscapeDataString(fieldName)}",
            ReplicatedJsonContext.Default.LicenseField,
            cancellationToken);
    }
}
