using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Replicated;

// ── POST/PATCH /api/v1/app/custom-metrics ─────────────────────────────────────

internal sealed class CustomMetricsRequest
{
    [JsonPropertyName("data")]
    public Dictionary<string, double> Data { get; init; } = new();
}

// ── POST /api/v1/app/instance-tags ───────────────────────────────────────────

internal sealed record InstanceTagsRequest(
    [property: JsonPropertyName("force")] bool Force,
    [property: JsonPropertyName("tags")] Dictionary<string, string> Tags);

// ── Error body parsing ────────────────────────────────────────────────────────

internal sealed record ErrorResponse(
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("code")] string? Code);
