using System.Text.Json.Serialization;

namespace Replicated;

// ── GET /api/v1/app/info ──────────────────────────────────────────────────────

/// <summary>Details about the current application instance.</summary>
public sealed record AppInfo(
    [property: JsonPropertyName("instanceID")] string? InstanceId,
    [property: JsonPropertyName("appSlug")] string? AppSlug,
    [property: JsonPropertyName("appName")] string? AppName,
    [property: JsonPropertyName("appStatus")] string? AppStatus,
    [property: JsonPropertyName("helmChartURL")] string? HelmChartUrl,
    [property: JsonPropertyName("currentRelease")] CurrentRelease? CurrentRelease);

/// <summary>Details about the currently installed release.</summary>
public sealed record CurrentRelease(
    [property: JsonPropertyName("versionLabel")] string? VersionLabel,
    [property: JsonPropertyName("channelName")] string? ChannelName,
    [property: JsonPropertyName("createdAt")] string? CreatedAt,
    [property: JsonPropertyName("releaseNotes")] string? ReleaseNotes);

// ── GET /api/v1/app/status ────────────────────────────────────────────────────

/// <summary>Current application resource status.</summary>
public sealed record AppStatus(
    [property: JsonPropertyName("updatedAt")] string? UpdatedAt,
    [property: JsonPropertyName("sequence")] long? Sequence,
    [property: JsonPropertyName("resources")] ResourceState[]? Resources);

/// <summary>State of a single Kubernetes resource.</summary>
public sealed record ResourceState(
    [property: JsonPropertyName("kind")] string? Kind,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("namespace")] string? Namespace,
    [property: JsonPropertyName("state")] string? State);

// ── GET /api/v1/app/updates and /api/v1/app/history ──────────────────────────

/// <summary>An available or previously installed release.</summary>
public sealed record AppRelease(
    [property: JsonPropertyName("versionLabel")] string? VersionLabel,
    [property: JsonPropertyName("createdAt")] string? CreatedAt,
    [property: JsonPropertyName("releaseNotes")] string? ReleaseNotes);
