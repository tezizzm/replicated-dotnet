using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Replicated;

/// <summary>
/// Manages local SDK state for idempotency and caching.
/// </summary>
public class StateManager
{
    private readonly string _appSlug;
    private readonly string _stateDirectory;
    private readonly string _stateFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateManager"/> class.
    /// </summary>
    /// <param name="appSlug">The application slug.</param>
    /// <param name="stateDirectory">Optional custom state directory. If null, uses platform-specific default.</param>
    public StateManager(string appSlug, string? stateDirectory = null)
    {
        _appSlug = appSlug ?? throw new ArgumentNullException(nameof(appSlug));

        if (!string.IsNullOrEmpty(stateDirectory))
        {
            // Normalize path: expand ~ and resolve relative paths
            _stateDirectory = Path.GetFullPath(stateDirectory.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
        }
        else
        {
            _stateDirectory = GetDefaultStateDirectory();
        }

        // Ensure directory exists (may throw if directory cannot be created)
        try
        {
            Directory.CreateDirectory(_stateDirectory);
        }
        catch (IOException ex) when (ex.Message.Contains("Read-only") || ex.Message.Contains("read-only"))
        {
            throw new ArgumentException(
                $"Cannot create state directory at '{_stateDirectory}'. The directory or one of its parent directories is read-only or cannot be accessed. Please use a writable directory.",
                nameof(stateDirectory),
                ex);
        }

        _stateFilePath = Path.Combine(_stateDirectory, "state.json");
    }

    /// <summary>
    /// Gets the platform-specific state directory.
    /// </summary>
    private string GetDefaultStateDirectory()
    {
        string baseDir;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: ~/Library/Application Support/Replicated/<app_slug>
            baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "Replicated");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: %APPDATA%\Replicated\<app_slug>
            baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Replicated");
        }
        else
        {
            // Linux: ${XDG_STATE_HOME:-~/.local/state}/replicated/<app_slug>
            var xdgStateHome = Environment.GetEnvironmentVariable("XDG_STATE_HOME");
            if (!string.IsNullOrEmpty(xdgStateHome))
            {
                baseDir = Path.Combine(xdgStateHome, "replicated");
            }
            else
            {
                baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local", "state", "replicated");
            }
        }

        return Path.Combine(baseDir, _appSlug);
    }

    /// <summary>
    /// Gets the current state.
    /// </summary>
    /// <returns>A dictionary containing the current state.</returns>
    public Dictionary<string, object> GetState()
    {
        if (File.Exists(_stateFilePath))
        {
            try
            {
                var json = File.ReadAllText(_stateFilePath);
                var state = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                return state ?? new Dictionary<string, object>();
            }
            catch
            {
                // Return empty state on error
            }
        }

        return new Dictionary<string, object>();
    }

    /// <summary>
    /// Saves state to disk.
    /// </summary>
    /// <param name="state">The state dictionary to save.</param>
    private void SaveState(Dictionary<string, object> state)
    {
        try
        {
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_stateFilePath, json);
        }
        catch
        {
            // Silently ignore write errors
        }
    }

    /// <summary>
    /// Gets the cached customer ID.
    /// </summary>
    /// <returns>The customer ID if found, otherwise null.</returns>
    public string? GetCustomerId()
    {
        var state = GetState();
        return state.TryGetValue("customer_id", out var value) ? value?.ToString() : null;
    }

    /// <summary>
    /// Sets the customer ID in state.
    /// </summary>
    /// <param name="customerId">The customer ID.</param>
    public void SetCustomerId(string customerId)
    {
        var state = GetState();
        state["customer_id"] = customerId;
        SaveState(state);
    }

    /// <summary>
    /// Gets the cached instance ID.
    /// </summary>
    /// <returns>The instance ID if found, otherwise null.</returns>
    public string? GetInstanceId()
    {
        var state = GetState();
        return state.TryGetValue("instance_id", out var value) ? value?.ToString() : null;
    }

    /// <summary>
    /// Sets the instance ID in state.
    /// </summary>
    /// <param name="instanceId">The instance ID.</param>
    public void SetInstanceId(string instanceId)
    {
        var state = GetState();
        state["instance_id"] = instanceId;
        SaveState(state);
    }

    /// <summary>
    /// Gets the cached dynamic token.
    /// </summary>
    /// <returns>The dynamic token if found, otherwise null.</returns>
    public string? GetDynamicToken()
    {
        var state = GetState();
        return state.TryGetValue("dynamic_token", out var value) ? value?.ToString() : null;
    }

    /// <summary>
    /// Sets the dynamic token in state.
    /// </summary>
    /// <param name="token">The dynamic token.</param>
    public void SetDynamicToken(string token)
    {
        var state = GetState();
        state["dynamic_token"] = token;
        SaveState(state);
    }

    /// <summary>
    /// Gets the cached customer email.
    /// </summary>
    /// <returns>The customer email if found, otherwise null.</returns>
    public string? GetCustomerEmail()
    {
        var state = GetState();
        return state.TryGetValue("customer_email", out var value) ? value?.ToString() : null;
    }

    /// <summary>
    /// Sets the customer email in state.
    /// </summary>
    /// <param name="email">The customer email.</param>
    public void SetCustomerEmail(string email)
    {
        var state = GetState();
        state["customer_email"] = email;
        SaveState(state);
    }

    /// <summary>
    /// Clears all cached state.
    /// </summary>
    public void ClearState()
    {
        if (File.Exists(_stateFilePath))
        {
            try
            {
                File.Delete(_stateFilePath);
            }
            catch
            {
                // Silently ignore deletion errors
            }
        }
    }
}

