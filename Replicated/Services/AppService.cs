using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Replicated.Services;

/// <summary>
/// Service for interacting with the application endpoints of the in-cluster
/// Replicated SDK API (<c>/api/v1/app/...</c>).
/// </summary>
public class AppService
{
    private readonly IHttpClientContext _context;

    internal AppService(IHttpClientContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <summary>
    /// Returns details about the current application instance.
    /// Calls GET /api/v1/app/info.
    /// </summary>
    public Task<AppInfo> GetInfoAsync(CancellationToken cancellationToken = default)
        => _context.GetAsync(Constants.AppInfoEndpoint,
            ReplicatedJsonContext.Default.AppInfo, cancellationToken);

    /// <summary>
    /// Returns current application resource status.
    /// Calls GET /api/v1/app/status.
    /// </summary>
    public Task<AppStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        => _context.GetAsync(Constants.AppStatusEndpoint,
            ReplicatedJsonContext.Default.AppStatus, cancellationToken);

    /// <summary>
    /// Returns available releases for upgrade.
    /// Calls GET /api/v1/app/updates.
    /// </summary>
    public Task<AppRelease[]> GetUpdatesAsync(CancellationToken cancellationToken = default)
        => _context.GetAsync(Constants.AppUpdatesEndpoint,
            ReplicatedJsonContext.Default.AppReleaseArray, cancellationToken);

    /// <summary>
    /// Returns previously installed releases.
    /// Calls GET /api/v1/app/history.
    /// </summary>
    public Task<AppRelease[]> GetHistoryAsync(CancellationToken cancellationToken = default)
        => _context.GetAsync(Constants.AppHistoryEndpoint,
            ReplicatedJsonContext.Default.AppReleaseArray, cancellationToken);

    /// <summary>
    /// Sends (replaces) all custom metrics.
    /// Calls POST /api/v1/app/custom-metrics.
    /// </summary>
    /// <param name="metrics">Metric names and their current numeric values.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    public Task SendCustomMetricsAsync(Dictionary<string, double> metrics,
        CancellationToken cancellationToken = default)
    {
        if (metrics == null) throw new ArgumentNullException(nameof(metrics));
        return _context.PostAsync(
            Constants.AppCustomMetrics,
            new CustomMetricsRequest { Data = metrics },
            ReplicatedJsonContext.Default.CustomMetricsRequest,
            cancellationToken);
    }

    /// <summary>
    /// Merges (upserts) custom metrics without replacing existing ones.
    /// Calls PATCH /api/v1/app/custom-metrics.
    /// </summary>
    /// <param name="metrics">Metric names and their current numeric values.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    public Task UpsertCustomMetricsAsync(Dictionary<string, double> metrics,
        CancellationToken cancellationToken = default)
    {
        if (metrics == null) throw new ArgumentNullException(nameof(metrics));
        return _context.PatchAsync(
            Constants.AppCustomMetrics,
            new CustomMetricsRequest { Data = metrics },
            ReplicatedJsonContext.Default.CustomMetricsRequest,
            cancellationToken);
    }

    /// <summary>
    /// Deletes a specific custom metric.
    /// Calls DELETE /api/v1/app/custom-metrics/{metricName}.
    /// </summary>
    /// <param name="metricName">The name of the metric to delete.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    public Task DeleteCustomMetricAsync(string metricName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name cannot be null or empty.", nameof(metricName));
        return _context.DeleteAsync(
            $"{Constants.AppCustomMetrics}/{Uri.EscapeDataString(metricName)}",
            cancellationToken);
    }

    /// <summary>
    /// Sets instance tags as key-value pairs.
    /// Calls POST /api/v1/app/instance-tags.
    /// </summary>
    /// <param name="tags">Tag names and values. Set a value to an empty string to delete a tag.</param>
    /// <param name="force">When true, existing tags not in <paramref name="tags"/> are removed.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    public Task SetInstanceTagsAsync(Dictionary<string, string> tags, bool force = false,
        CancellationToken cancellationToken = default)
    {
        if (tags == null) throw new ArgumentNullException(nameof(tags));
        return _context.PostAsync(
            Constants.AppInstanceTags,
            new InstanceTagsRequest(force, tags),
            ReplicatedJsonContext.Default.InstanceTagsRequest,
            cancellationToken);
    }
}
