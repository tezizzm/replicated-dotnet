using System.Text.Json.Serialization;

namespace Replicated;

// ── GET /api/v1/license/info ──────────────────────────────────────────────────

/// <summary>License information for the current installation.</summary>
public sealed record LicenseInfo(
    [property: JsonPropertyName("licenseID")] string? LicenseId,
    [property: JsonPropertyName("licenseType")] string? LicenseType,
    [property: JsonPropertyName("customerName")] string? CustomerName,
    [property: JsonPropertyName("customerEmail")] string? CustomerEmail,
    [property: JsonPropertyName("channelName")] string? ChannelName,
    [property: JsonPropertyName("entitlements")] LicenseEntitlement[]? Entitlements);

/// <summary>A single license entitlement.</summary>
public sealed record LicenseEntitlement(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("description")] string? Description);

// ── GET /api/v1/license/fields and /api/v1/license/fields/{name} ──────────────

/// <summary>A license field with its current value and optional signature.</summary>
public sealed record LicenseField(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("signature")] string? Signature);
