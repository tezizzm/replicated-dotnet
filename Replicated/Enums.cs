namespace Replicated;

/// <summary>
/// Instance status enumeration.
/// </summary>
public enum InstanceStatus
{
    /// <summary>
    /// Instance is running normally.
    /// </summary>
    Running,

    /// <summary>
    /// Instance is degraded but operational.
    /// </summary>
    Degraded,

    /// <summary>
    /// Instance is missing.
    /// </summary>
    Missing,

    /// <summary>
    /// Instance is unavailable.
    /// </summary>
    Unavailable,

    /// <summary>
    /// Instance is ready.
    /// </summary>
    Ready,

    /// <summary>
    /// Instance is updating.
    /// </summary>
    Updating
}

