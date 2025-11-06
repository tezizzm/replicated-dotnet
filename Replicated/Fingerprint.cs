using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Replicated;

/// <summary>
/// Provides machine fingerprinting capabilities.
/// </summary>
public static class Fingerprint
{
    /// <summary>
    /// Gets a unique machine fingerprint based on platform.
    /// Returns a SHA256 hash of the platform-specific identifier.
    /// </summary>
    /// <returns>A SHA256 hash string of the machine identifier.</returns>
    public static string GetMachineFingerprint()
    {
        string identifier = "";

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: Use IOPlatformUUID
                identifier = GetMacOSPlatformUUID();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: Use D-Bus machine ID
                identifier = GetLinuxMachineId();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: Use Machine GUID from registry
                identifier = GetWindowsMachineGuid();
            }
        }
        catch
        {
            // Fall through to fallback
        }

        // Fallback: use network interface MAC address
        if (string.IsNullOrEmpty(identifier))
        {
            identifier = GetFallbackIdentifier();
        }

        // Hash the identifier for privacy
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(identifier));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    private static string GetMacOSPlatformUUID()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ioreg",
                    Arguments = "-rd1 -c IOPlatformExpertDevice",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                foreach (var line in output.Split('\n'))
                {
                    if (line.Contains("IOPlatformUUID"))
                    {
                        var parts = line.Split('"');
                        if (parts.Length >= 4)
                        {
                            return parts[3];
                        }
                    }
                }
            }
        }
        catch
        {
            // Fall through
        }

        return "";
    }

    private static string GetLinuxMachineId()
    {
        try
        {
            // Try /var/lib/dbus/machine-id first
            if (File.Exists("/var/lib/dbus/machine-id"))
            {
                return File.ReadAllText("/var/lib/dbus/machine-id").Trim();
            }

            // Try /etc/machine-id as fallback
            if (File.Exists("/etc/machine-id"))
            {
                return File.ReadAllText("/etc/machine-id").Trim();
            }
        }
        catch
        {
            // Fall through
        }

        return "";
    }

    private static string GetWindowsMachineGuid()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "reg",
                    Arguments = "query HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Cryptography /v MachineGuid",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                foreach (var line in output.Split('\n'))
                {
                    if (line.Contains("MachineGuid"))
                    {
                        var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            return parts[parts.Length - 1];
                        }
                    }
                }
            }
        }
        catch
        {
            // Fall through
        }

        return "";
    }

    private static string GetFallbackIdentifier()
    {
        // Use a combination of machine name and user for fallback
        try
        {
            return $"{Environment.MachineName}_{Environment.UserName}";
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }
}

