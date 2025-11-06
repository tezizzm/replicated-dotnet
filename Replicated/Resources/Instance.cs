using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Replicated.Validation;

namespace Replicated.Resources;

/// <summary>
/// Represents a customer instance.
/// </summary>
public class Instance
{
    private readonly IReplicatedClient _client;
    private readonly string _customerId;
    private readonly string _machineId;
    private readonly Dictionary<string, object> _data;
    private string? _instanceId;
    private string _status = "ready";
    private string _version = "";
    private readonly Dictionary<string, object> _metrics = new();

    /// <summary>
    /// Gets the customer ID.
    /// </summary>
    public string CustomerId => _customerId;

    /// <summary>
    /// Gets the instance ID.
    /// </summary>
    public string? InstanceId => _instanceId;

    /// <summary>
    /// Initializes a new instance of the <see cref="Instance"/> class.
    /// </summary>
    /// <param name="client">The Replicated client.</param>
    /// <param name="customerId">The customer ID.</param>
    /// <param name="instanceId">Optional instance ID.</param>
    /// <param name="data">Additional instance data.</param>
    public Instance(
        IReplicatedClient client,
        string customerId,
        string? instanceId = null,
        Dictionary<string, object>? data = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _customerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
        _instanceId = instanceId;
        _machineId = client.MachineId;
        _data = data ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Sends a metric for this instance to the Replicated API.
    /// </summary>
    /// <param name="name">The metric name. Must contain only alphanumeric characters and underscores.</param>
    /// <param name="value">The metric value. Can be a number, string, boolean, or other JSON-serializable value.</param>
    /// <exception cref="ArgumentException">Thrown when metric name is invalid or contains invalid characters.</exception>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ReplicatedApiError">Thrown when the API returns an error response.</exception>
    /// <exception cref="ReplicatedNetworkError">Thrown when a network error occurs.</exception>
    /// <remarks>
    /// Metrics are accumulated in memory and sent together in a single request. Each call to SendMetric updates the metric value.
    /// The instance ID is automatically created if it doesn't exist. Metrics are sent to the "/application/custom-metrics" endpoint.
    /// </remarks>
    /// <example>
    /// <code>
    /// instance.SendMetric("cpu_usage", 0.75);
    /// instance.SendMetric("memory_usage", 0.60);
    /// instance.SendMetric("active_users", 150);
    /// </code>
    /// </example>
    public void SendMetric(string name, object value)
    {
        // Validate inputs
        InputValidator.ValidateMetricName(name);
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        
        if (string.IsNullOrEmpty(_instanceId))
        {
            EnsureInstance();
        }

        // Merge metric with existing metrics (overwrite = false behavior)
        _metrics[name] = value;

        // Build headers with instance data
        var headers = new Dictionary<string, string>(_client.GetAuthHeaders())
        {
            ["X-Replicated-InstanceID"] = _instanceId!,
            ["X-Replicated-ClusterID"] = _machineId,
            ["X-Replicated-AppStatus"] = _status
        };

        _client.MakeRequest(
            Constants.HttpMethodPost,
            Constants.MetricsEndpoint,
            headers,
            new Dictionary<string, object> { ["data"] = _metrics });
    }

    /// <summary>
    /// Sends a metric for this instance (async).
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The metric value.</param>
    /// <exception cref="ArgumentException">Thrown when metric name is invalid.</exception>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public async Task SendMetricAsync(string name, object value)
    {
        // Validate inputs
        InputValidator.ValidateMetricName(name);
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        
        if (string.IsNullOrEmpty(_instanceId))
        {
            await EnsureInstanceAsync();
        }

        // Merge metric with existing metrics (overwrite = false behavior)
        _metrics[name] = value;

        // Build headers with instance data
        var headers = new Dictionary<string, string>(_client.GetAuthHeaders())
        {
            ["X-Replicated-InstanceID"] = _instanceId!,
            ["X-Replicated-ClusterID"] = _machineId,
            ["X-Replicated-AppStatus"] = _status
        };

        await _client.MakeRequestAsync(
            Constants.HttpMethodPost,
            Constants.MetricsEndpoint,
            headers,
            new Dictionary<string, object> { ["data"] = _metrics });
    }

    /// <summary>
    /// Sets the status of this instance for telemetry reporting.
    /// </summary>
    /// <param name="status">The instance status. Must be one of: Running, Degraded, Missing, Unavailable, Ready, Updating (case-insensitive).</param>
    /// <exception cref="ArgumentException">Thrown when status is invalid or not a recognized instance status.</exception>
    /// <exception cref="ReplicatedApiError">Thrown when the API returns an error response.</exception>
    /// <remarks>
    /// The status is reported to the Replicated telemetry endpoint and included in subsequent metric requests.
    /// Common statuses: "running" (normal operation), "degraded" (operational but with issues), "missing" (instance not found), 
    /// "unavailable" (temporarily unavailable), "ready" (initialized and ready), "updating" (currently updating).
    /// </remarks>
    /// <example>
    /// <code>
    /// instance.SetStatus("running");
    /// </code>
    /// </example>
    public void SetStatus(string status)
    {
        // Validate inputs
        InputValidator.ValidateInstanceStatus(status);
        
        if (string.IsNullOrEmpty(_instanceId))
        {
            EnsureInstance();
        }

        _status = status;
        ReportInstance();
    }

    /// <summary>
    /// Sets the status of this instance for telemetry reporting (async).
    /// </summary>
    /// <param name="status">The instance status.</param>
    /// <exception cref="ArgumentException">Thrown when status is invalid.</exception>
    public async Task SetStatusAsync(string status)
    {
        // Validate inputs
        InputValidator.ValidateInstanceStatus(status);
        
        if (string.IsNullOrEmpty(_instanceId))
        {
            await EnsureInstanceAsync();
        }

        _status = status;
        await ReportInstanceAsync();
    }

    /// <summary>
    /// Sets the version of this instance for telemetry reporting.
    /// </summary>
    /// <param name="version">The instance version string (e.g., "1.0.0", "v2.3.1"). Must contain only alphanumeric characters, dots, underscores, and hyphens.</param>
    /// <exception cref="ArgumentException">Thrown when version is invalid or contains invalid characters.</exception>
    /// <exception cref="ReplicatedApiError">Thrown when the API returns an error response.</exception>
    /// <remarks>
    /// The version is reported to the Replicated telemetry endpoint and included in subsequent metric requests.
    /// This is useful for tracking which version of your application is running on each instance.
    /// </remarks>
    /// <example>
    /// <code>
    /// instance.SetVersion("1.2.3");
    /// instance.SetVersion("v2.0.0-beta");
    /// </code>
    /// </example>
    public void SetVersion(string version)
    {
        // Validate inputs
        InputValidator.ValidateVersion(version);
        
        if (string.IsNullOrEmpty(_instanceId))
        {
            EnsureInstance();
        }

        _version = version;
        ReportInstance();
    }

    /// <summary>
    /// Sets the version of this instance for telemetry reporting (async).
    /// </summary>
    /// <param name="version">The instance version.</param>
    /// <exception cref="ArgumentException">Thrown when version is invalid.</exception>
    public async Task SetVersionAsync(string version)
    {
        // Validate inputs
        InputValidator.ValidateVersion(version);
        
        if (string.IsNullOrEmpty(_instanceId))
        {
            await EnsureInstanceAsync();
        }

        _version = version;
        await ReportInstanceAsync();
    }

    /// <summary>
    /// Gets additional instance data by key.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <returns>The data value if found, otherwise null.</returns>
    public object? GetData(string key)
    {
        return _data.TryGetValue(key, out var value) ? value : null;
    }

    private void EnsureInstance()
    {
        if (!string.IsNullOrEmpty(_instanceId))
        {
            return;
        }

        // Check if instance ID is cached
        var cachedInstanceId = _client.StateManager.GetInstanceId();
        if (!string.IsNullOrEmpty(cachedInstanceId))
        {
            _instanceId = cachedInstanceId;
            return;
        }

        // Create new instance
        var fingerprint = Fingerprint.GetMachineFingerprint();
        var response = _client.MakeRequest(
            Constants.HttpMethodPost,
            Constants.InstanceEndpoint,
            _client.GetAuthHeaders(),
            new Dictionary<string, object>
            {
                ["machine_fingerprint"] = fingerprint,
                ["app_status"] = Constants.DefaultAppStatus
            });

        _instanceId = response["instance_id"]?.ToString() ?? throw new InvalidOperationException("Failed to create instance");
        _client.StateManager.SetInstanceId(_instanceId);
    }

    private async Task EnsureInstanceAsync()
    {
        if (!string.IsNullOrEmpty(_instanceId))
        {
            return;
        }

        // Check if instance ID is cached
        var cachedInstanceId = _client.StateManager.GetInstanceId();
        if (!string.IsNullOrEmpty(cachedInstanceId))
        {
            _instanceId = cachedInstanceId;
            return;
        }

        // Create new instance
        var fingerprint = Fingerprint.GetMachineFingerprint();
        var response = await _client.MakeRequestAsync(
            Constants.HttpMethodPost,
            Constants.InstanceEndpoint,
            _client.GetAuthHeaders(),
            new Dictionary<string, object>
            {
                ["machine_fingerprint"] = fingerprint,
                ["app_status"] = Constants.DefaultAppStatus
            });

        _instanceId = response["instance_id"]?.ToString() ?? throw new InvalidOperationException("Failed to create instance");
        _client.StateManager.SetInstanceId(_instanceId);
    }

    private void ReportInstance()
    {
        if (string.IsNullOrEmpty(_instanceId))
        {
            EnsureInstance();
        }

        try
        {
            // Get hostname for instance tag
            string hostname;
            try
            {
                hostname = Dns.GetHostName();
            }
            catch
            {
                hostname = "unknown";
            }

            // Create instance tags with hostname as instance name
            var instanceTags = new { force = true, tags = new { name = hostname } };
            var instanceTagsJson = JsonSerializer.Serialize(instanceTags);
            var instanceTagsB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(instanceTagsJson));

            var headers = new Dictionary<string, string>(_client.GetAuthHeaders())
            {
                ["X-Replicated-InstanceID"] = _instanceId!,
                ["X-Replicated-ClusterID"] = _machineId,
                ["X-Replicated-AppStatus"] = _status,
                ["X-Replicated-VersionLabel"] = _version,
                ["X-Replicated-InstanceTagData"] = instanceTagsB64
            };

            _client.MakeRequest(
            Constants.HttpMethodPost,
            Constants.InstanceInfoEndpoint,
                headers,
                new Dictionary<string, object>());
        }
        catch
        {
            // Telemetry is optional - don't fail if it doesn't work
        }
    }

    private async Task ReportInstanceAsync()
    {
        if (string.IsNullOrEmpty(_instanceId))
        {
            await EnsureInstanceAsync();
        }

        try
        {
            // Get hostname for instance tag
            string hostname;
            try
            {
                hostname = Dns.GetHostName();
            }
            catch
            {
                hostname = "unknown";
            }

            // Create instance tags with hostname as instance name
            var instanceTags = new { force = true, tags = new { name = hostname } };
            var instanceTagsJson = JsonSerializer.Serialize(instanceTags);
            var instanceTagsB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(instanceTagsJson));

            var headers = new Dictionary<string, string>(_client.GetAuthHeaders())
            {
                ["X-Replicated-InstanceID"] = _instanceId!,
                ["X-Replicated-ClusterID"] = _machineId,
                ["X-Replicated-AppStatus"] = _status,
                ["X-Replicated-VersionLabel"] = _version,
                ["X-Replicated-InstanceTagData"] = instanceTagsB64
            };

            await _client.MakeRequestAsync(
            Constants.HttpMethodPost,
            Constants.InstanceInfoEndpoint,
                headers,
                new Dictionary<string, object>());
        }
        catch
        {
            // Telemetry is optional - don't fail if it doesn't work
        }
    }
}
