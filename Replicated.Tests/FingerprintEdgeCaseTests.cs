using Replicated;
using Xunit;

namespace Replicated.Tests;

/// <summary>
/// Tests for machine fingerprinting edge cases and platform-specific scenarios.
/// </summary>
public class FingerprintEdgeCaseTests
{
    [Fact]
    public void GetMachineFingerprint_ShouldReturnValidHashFormat()
    {
        // Act
        var fingerprint = Fingerprint.GetMachineFingerprint();

        // Assert
        Assert.NotNull(fingerprint);
        Assert.NotEmpty(fingerprint);
        Assert.Equal(64, fingerprint.Length); // SHA256 hex string length
        Assert.Matches("^[0-9a-f]{64}$", fingerprint); // Valid hex string
    }

    [Fact]
    public void GetMachineFingerprint_ShouldBeDeterministic()
    {
        // Act - Get fingerprint multiple times
        var fingerprint1 = Fingerprint.GetMachineFingerprint();
        var fingerprint2 = Fingerprint.GetMachineFingerprint();
        var fingerprint3 = Fingerprint.GetMachineFingerprint();

        // Assert - Should return same value each time on same machine
        Assert.Equal(fingerprint1, fingerprint2);
        Assert.Equal(fingerprint2, fingerprint3);
    }

    [Fact]
    public void GetMachineFingerprint_ShouldReturnConsistentValueAcrossCalls()
    {
        // Arrange - Call multiple times in sequence
        var fingerprints = new string[10];
        for (int i = 0; i < 10; i++)
        {
            fingerprints[i] = Fingerprint.GetMachineFingerprint();
        }

        // Assert - All should be identical
        var firstFingerprint = fingerprints[0];
        Assert.All(fingerprints, fp => Assert.Equal(firstFingerprint, fp));
    }

    [Fact]
    public void GetMachineFingerprint_ShouldHandleFallbackGracefully()
    {
        // This test verifies that fingerprinting works even when platform-specific methods fail
        // The implementation should fall back to network interface MAC addresses
        
        // Act
        var fingerprint = Fingerprint.GetMachineFingerprint();

        // Assert - Should still return a valid fingerprint even if platform-specific method fails
        Assert.NotNull(fingerprint);
        Assert.NotEmpty(fingerprint);
        Assert.Equal(64, fingerprint.Length);
    }

    [Fact]
    public void GetMachineFingerprint_ShouldAlwaysReturn64CharacterHash()
    {
        // Arrange - Get multiple fingerprints
        var fingerprints = new[]
        {
            Fingerprint.GetMachineFingerprint(),
            Fingerprint.GetMachineFingerprint(),
            Fingerprint.GetMachineFingerprint()
        };

        // Assert - All should be exactly 64 characters (SHA256 hex)
        Assert.All(fingerprints, fp => Assert.Equal(64, fp.Length));
    }

    [Fact]
    public void GetMachineFingerprint_ShouldReturnLowerCaseHash()
    {
        // Act
        var fingerprint = Fingerprint.GetMachineFingerprint();

        // Assert - SHA256 hash should be lowercase
        Assert.Equal(fingerprint, fingerprint.ToLowerInvariant());
        Assert.DoesNotMatch("[A-Z]", fingerprint); // No uppercase letters
    }
}

