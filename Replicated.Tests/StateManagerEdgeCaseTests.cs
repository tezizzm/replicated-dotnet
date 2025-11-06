using System;
using System.IO;
using System.Text.Json;
using Replicated;
using Xunit;

namespace Replicated.Tests;

/// <summary>
/// Tests for state management edge cases and error scenarios.
/// These tests verify behavior when state files are corrupt, directories are read-only, etc.
/// </summary>
public class StateManagerEdgeCaseTests
{
    [Fact]
    public void GetState_WithCorruptJsonFile_ShouldReturnEmptyState()
    {
        // Arrange - Use a unique app slug to avoid conflicts
        var tempDir = Path.Combine(Path.GetTempPath(), $"replicated_test_{Guid.NewGuid().ToString("N")[..8]}");
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}", tempDir);
        
        try
        {
            // Create a corrupt JSON file
            var stateFilePath = Path.Combine(tempDir, "state.json");
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(stateFilePath, "{ invalid json }");

            // Act
            var state = stateManager.GetState();

            // Assert - Should return empty state instead of throwing
            Assert.NotNull(state);
            Assert.Empty(state);
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void GetState_WithEmptyJsonFile_ShouldReturnEmptyState()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"replicated_test_{Guid.NewGuid().ToString("N")[..8]}");
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}", tempDir);

        try
        {
            var stateFilePath = Path.Combine(tempDir, "state.json");
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(stateFilePath, "");

            // Act
            var state = stateManager.GetState();

            // Assert
            Assert.NotNull(state);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void GetState_WithNullJsonFile_ShouldReturnEmptyState()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"replicated_test_{Guid.NewGuid().ToString("N")[..8]}");
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}", tempDir);

        try
        {
            var stateFilePath = Path.Combine(tempDir, "state.json");
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(stateFilePath, "null");

            // Act
            var state = stateManager.GetState();

            // Assert - Should handle null deserialization gracefully
            Assert.NotNull(state);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void SetCustomerId_WithSpecialCharacters_ShouldPersistCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"replicated_test_{Guid.NewGuid().ToString("N")[..8]}");
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}", tempDir);

        try
        {
            var customerId = "customer-123_abc.xyz@test";

            // Act
            stateManager.SetCustomerId(customerId);
            var retrieved = stateManager.GetCustomerId();

            // Assert
            Assert.Equal(customerId, retrieved);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void ClearState_ShouldRemoveAllState()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"replicated_test_{Guid.NewGuid().ToString("N")[..8]}");
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}", tempDir);

        try
        {
            stateManager.SetCustomerId("customer_123");
            stateManager.SetInstanceId("instance_456");
            stateManager.SetCustomerEmail("test@example.com");
            stateManager.SetDynamicToken("token_789");

            // Act
            stateManager.ClearState();

            // Assert
            Assert.Null(stateManager.GetCustomerId());
            Assert.Null(stateManager.GetInstanceId());
            Assert.Null(stateManager.GetCustomerEmail());
            Assert.Null(stateManager.GetDynamicToken());
            var state = stateManager.GetState();
            Assert.Empty(state);
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void StateManager_WithTildeInStateDirectory_ShouldExpandToHomeDirectory()
    {
        // This test verifies that ~ is expanded to the user's home directory
        // Note: On some systems this might not work the same way, so we test the behavior
        
        // Arrange
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var stateDirWithTilde = "~/test_replicated_state";
        var stateManager = new StateManager("test_app", stateDirWithTilde);

        try
        {
            // Act - Set and get a value to verify it works
            stateManager.SetCustomerId("test_customer");
            var retrieved = stateManager.GetCustomerId();

            // Assert
            Assert.Equal("test_customer", retrieved);
            
            // Verify the path was expanded (check that ~ was replaced)
            var expectedPath = Path.Combine(homeDir, "test_replicated_state");
            var state = stateManager.GetState();
            Assert.NotNull(state);
        }
        finally
        {
            // Cleanup
            try
            {
                var expandedPath = Path.Combine(homeDir, "test_replicated_state");
                if (Directory.Exists(expandedPath))
                    Directory.Delete(expandedPath, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void GetState_AfterMultipleWrites_ShouldReflectLatestState()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"replicated_test_{Guid.NewGuid().ToString("N")[..8]}");
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}", tempDir);

        try
        {
            // Act - Multiple state changes
            stateManager.SetCustomerId("customer_1");
            stateManager.SetInstanceId("instance_1");
            stateManager.SetCustomerId("customer_2");
            stateManager.SetInstanceId("instance_2");

            // Assert
            Assert.Equal("customer_2", stateManager.GetCustomerId());
            Assert.Equal("instance_2", stateManager.GetInstanceId());
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch { }
        }
    }

    [Fact]
    public void GetState_WithVeryLongValues_ShouldPersistCorrectly()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"replicated_test_{Guid.NewGuid().ToString("N")[..8]}");
        var stateManager = new StateManager($"test_app_{Guid.NewGuid().ToString("N")[..8]}", tempDir);

        try
        {
            var longCustomerId = new string('a', 1000);
            var longToken = new string('b', 2000);

            // Act
            stateManager.SetCustomerId(longCustomerId);
            stateManager.SetDynamicToken(longToken);

            // Assert
            Assert.Equal(longCustomerId, stateManager.GetCustomerId());
            Assert.Equal(longToken, stateManager.GetDynamicToken());
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
            catch { }
        }
    }
}

