namespace Replicated;

/// <summary>
/// Constants used throughout the Replicated SDK.
/// </summary>
internal static class Constants
{
    // Default base URL for the in-cluster Replicated SDK service.
    public const string DefaultBaseUrl = "http://replicated:3000";

    // HTTP content type
    public const string ContentTypeJson = "application/json";

    // ── App endpoints (/api/v1/app/...) ──────────────────────────────────────
    public const string AppInfoEndpoint = "/api/v1/app/info";
    public const string AppStatusEndpoint = "/api/v1/app/status";
    public const string AppUpdatesEndpoint = "/api/v1/app/updates";
    public const string AppHistoryEndpoint = "/api/v1/app/history";
    public const string AppCustomMetrics = "/api/v1/app/custom-metrics";
    public const string AppInstanceTags = "/api/v1/app/instance-tags";
    public const string AppSupportBundle = "/api/v1/app/supportbundle";

    // ── Support bundle metadata endpoints ────────────────────────────────────
    public const string SupportBundleMetadata = "/api/v1/supportbundle/metadata";

    // ── License endpoints (/api/v1/license/...) ───────────────────────────────
    public const string LicenseInfoEndpoint = "/api/v1/license/info";
    public const string LicenseFieldsEndpoint = "/api/v1/license/fields";
}
