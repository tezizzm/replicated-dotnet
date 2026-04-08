using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Replicated;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for all types serialized or
/// deserialized by the Replicated SDK. Using a source-generated context removes all
/// reflection-based JSON paths, making the library fully compatible with Native AOT
/// and aggressive IL-trimming configurations.
/// </summary>
[JsonSerializable(typeof(AppInfo))]
[JsonSerializable(typeof(CurrentRelease))]
[JsonSerializable(typeof(AppStatus))]
[JsonSerializable(typeof(ResourceState[]))]
[JsonSerializable(typeof(ResourceState))]
[JsonSerializable(typeof(AppRelease[]))]
[JsonSerializable(typeof(AppRelease))]
[JsonSerializable(typeof(LicenseInfo))]
[JsonSerializable(typeof(LicenseEntitlement[]))]
[JsonSerializable(typeof(LicenseEntitlement))]
[JsonSerializable(typeof(LicenseField[]))]
[JsonSerializable(typeof(LicenseField))]
[JsonSerializable(typeof(CustomMetricsRequest))]
[JsonSerializable(typeof(Dictionary<string, double>))]
[JsonSerializable(typeof(InstanceTagsRequest))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSourceGenerationOptions(
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    PropertyNameCaseInsensitive = true)]
internal partial class ReplicatedJsonContext : JsonSerializerContext { }
