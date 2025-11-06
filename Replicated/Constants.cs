namespace Replicated;

/// <summary>
/// Constants used throughout the Replicated SDK.
/// </summary>
internal static class Constants
{
    // HTTP Headers
    public const string AuthorizationHeader = "Authorization";
    public const string ContentTypeJson = "application/json";
    public const string BearerPrefix = "Bearer ";
    
    // API Endpoints
    public const string CustomerEndpoint = "/v3/customer";
    public const string InstanceEndpoint = "/v3/instance";
    public const string MetricsEndpoint = "/application/custom-metrics";
    public const string InstanceInfoEndpoint = "/kots_metrics/license_instance/info";
    
    // Instance Status Values
    public const string StatusRunning = "running";
    public const string StatusDegraded = "degraded";
    public const string StatusMissing = "missing";
    public const string StatusUnavailable = "unavailable";
    public const string StatusReady = "ready";
    public const string StatusUpdating = "updating";
    
    // HTTP Methods
    public const string HttpMethodPost = "POST";
    
    // Default Values
    public const string DefaultChannel = "Stable";
    public const string DefaultAppStatus = "missing";
}

