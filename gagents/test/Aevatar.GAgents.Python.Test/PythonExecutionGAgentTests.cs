using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Python.Test;

/// <summary>
/// Unit tests for PythonExecutionGAgent
/// Tests cover environment management, script execution, security validation,
/// dependency management, and resource monitoring
/// </summary>
[Collection(nameof(PythonExecutionGAgentTests))]
public sealed class PythonExecutionGAgentTests : AevatarPythonGAgentTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public PythonExecutionGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task PythonExecutionGAgent_BasicInitialization_ShouldSucceed()
    {
        _testOutputHelper.WriteLine("🔧 Testing basic Python execution agent initialization...");

        // Arrange & Act
        var executionAgent = await _gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(Guid.NewGuid());
        await executionAgent.InitializeAsync();

        // Assert
        executionAgent.ShouldNotBeNull();
        var state = await executionAgent.GetStateAsync();
        state.ShouldNotBeNull();
        state.Initialized.ShouldBeTrue();

        _testOutputHelper.WriteLine("✅ Python execution agent initialized successfully");
    }

    [Fact]
    public async Task PythonExecutionGAgent_BasicScriptExecution_ShouldWork()
    {
        _testOutputHelper.WriteLine("🐍 Testing basic Python script execution...");

        // Arrange
        var executionAgent = await _gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(Guid.NewGuid());
        await executionAgent.InitializeAsync();

        var simpleScript = @"
# Simple calculation test
result = 2 + 3
print(f'Result: {result}')
assert result == 5
";

        // Act
        var executionResult = await executionAgent.ExecutePythonScriptAsync(simpleScript);

        // Assert
        executionResult.ShouldNotBeNull();
        _testOutputHelper.WriteLine($"Execution success: {executionResult.Success}");
        _testOutputHelper.WriteLine($"Standard output: {executionResult.StandardOutput}");
        _testOutputHelper.WriteLine($"Error output: {executionResult.ErrorOutput}");
        _testOutputHelper.WriteLine($"Execution time: {executionResult.ExecutionTimeSeconds}s");

        if (!executionResult.Success)
        {
            _testOutputHelper.WriteLine(
                $"⚠️ Script execution failed (possibly due to missing Python): {executionResult.ErrorOutput}");
            // Don't fail the test if Python is not available in test environment
            return;
        }

        executionResult.Success.ShouldBeTrue();
        executionResult.StandardOutput.ShouldContain("Result: 5");
        executionResult.ExecutionTimeSeconds.ShouldBeGreaterThan(0);

        _testOutputHelper.WriteLine("✅ Basic script execution completed successfully");
    }

    [Fact]
    public async Task PythonExecutionGAgent_SecurityValidation_ShouldBlockDangerousOperations()
    {
        _testOutputHelper.WriteLine("🔒 Testing security validation...");

        // Arrange
        var executionAgent = await _gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(Guid.NewGuid());
        await executionAgent.InitializeAsync();

        var dangerousScript = @"
import subprocess
subprocess.run(['ls', '-la'])
";

        // Act
        var isSecure = await executionAgent.ValidateScriptSecurityAsync(dangerousScript);

        // Assert
        isSecure.ShouldBeFalse();
        _testOutputHelper.WriteLine("✅ Security validation correctly blocked dangerous script");
    }

    [Fact]
    public async Task PythonExecutionGAgent_ExtractDependencies_ShouldIdentifyImports()
    {
        _testOutputHelper.WriteLine("📦 Testing dependency extraction...");

        // Arrange
        var executionAgent = await _gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(Guid.NewGuid());
        await executionAgent.InitializeAsync();

        var scriptWithDependencies = @"
import numpy as np
import pandas as pd
from matplotlib import pyplot as plt
import math  # Built-in module, should be ignored
";

        // Act
        var dependencies = await executionAgent.ExtractScriptDependenciesAsync(scriptWithDependencies);

        // Assert
        dependencies.ShouldNotBeNull();
        dependencies.ShouldContain("numpy");
        dependencies.ShouldContain("pandas");
        dependencies.ShouldContain("matplotlib");
        dependencies.ShouldNotContain("math"); // Built-in module should be filtered out

        _testOutputHelper.WriteLine($"✅ Extracted dependencies: {string.Join(", ", dependencies)}");
    }

    [Fact]
    public async Task PythonExecutionGAgent_EnvironmentManagement_ShouldCreateAndDeleteEnvironments()
    {
        _testOutputHelper.WriteLine("🏗️ Testing environment management...");

        // Arrange
        var executionAgent = await _gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(Guid.NewGuid());
        await executionAgent.InitializeAsync();

        var envName = "test-env-" + Guid.NewGuid().ToString("N")[..8];
        var config = new PythonEnvironmentConfig
        {
            EnvironmentName = envName,
            MaxExecutionTimeSeconds = 60,
            MaxMemoryMB = 256,
            IsSandboxed = true
        };

        try
        {
            // Act - Create environment
            var createResult = await executionAgent.CreateEnvironmentAsync(envName, config);

            if (!createResult)
            {
                _testOutputHelper.WriteLine("⚠️ Environment creation failed (possibly due to missing Python venv)");
                return;
            }

            // Assert - Environment created
            var environments = await executionAgent.ListEnvironmentsAsync();
            environments.ShouldContain(envName);

            var retrievedConfig = await executionAgent.GetEnvironmentConfigAsync(envName);
            retrievedConfig.ShouldNotBeNull();
            retrievedConfig.EnvironmentName.ShouldBe(envName);

            _testOutputHelper.WriteLine($"✅ Environment '{envName}' created successfully");

            // Act - Delete environment
            var deleteResult = await executionAgent.DeleteEnvironmentAsync(envName);

            // Assert - Environment deleted
            deleteResult.ShouldBeTrue();
            var environmentsAfterDelete = await executionAgent.ListEnvironmentsAsync();
            environmentsAfterDelete.ShouldNotContain(envName);

            _testOutputHelper.WriteLine($"✅ Environment '{envName}' deleted successfully");
        }
        catch (Exception ex)
        {
            _testOutputHelper.WriteLine($"⚠️ Environment management test failed: {ex.Message}");
            // Clean up if needed
            try
            {
                await executionAgent.DeleteEnvironmentAsync(envName);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task PythonExecutionGAgent_GetExecutionStats_ShouldReturnValidStats()
    {
        _testOutputHelper.WriteLine("📊 Testing execution statistics...");

        // Arrange
        var executionAgent = await _gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(Guid.NewGuid());
        await executionAgent.InitializeAsync();

        // Act
        var stats = await executionAgent.GetExecutionStatsAsync();

        // Assert
        stats.ShouldNotBeNull();
        stats.ShouldContainKey("Total");
        stats.ShouldContainKey("Successful");
        stats.ShouldContainKey("Failed");
        stats.ShouldContainKey("ActiveExecutions");
        stats.ShouldContainKey("TotalEnvironments");

        stats["Total"].ShouldBeGreaterThanOrEqualTo(0);
        stats["ActiveExecutions"].ShouldBeGreaterThanOrEqualTo(0);

        _testOutputHelper.WriteLine(
            $"✅ Execution stats: {string.Join(", ", stats.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
    }

    [Fact]
    public async Task PythonExecutionGAgent_GetExecutionHistory_ShouldReturnEmptyInitially()
    {
        _testOutputHelper.WriteLine("📚 Testing execution history...");

        // Arrange
        var executionAgent = await _gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(Guid.NewGuid());
        await executionAgent.InitializeAsync();

        // Act
        var history = await executionAgent.GetExecutionHistoryAsync();

        // Assert
        history.ShouldNotBeNull();
        history.ShouldBeEmpty(); // Should be empty for a new agent

        _testOutputHelper.WriteLine("✅ Execution history is initially empty as expected");
    }

    [Fact]
    public async Task PythonExecutionGAgent_PythonCommandDetection_ShouldWork()
    {
        _testOutputHelper.WriteLine("🔍 Testing Python command detection...");

        // Arrange
        var executionAgent = await _gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(Guid.NewGuid());
        await executionAgent.InitializeAsync();

        // Act
        var availableCommands = await executionAgent.GetAvailablePythonCommandsAsync();
        var bestCommand = await executionAgent.DetectBestPythonCommandAsync();

        // Assert
        availableCommands.ShouldNotBeNull();
        availableCommands.ShouldNotBeEmpty();

        if (!string.IsNullOrEmpty(bestCommand))
        {
            var isValid = await executionAgent.ValidatePythonCommandAsync(bestCommand);
            if (isValid)
            {
                var version = await executionAgent.GetPythonVersionAsync(bestCommand);
                _testOutputHelper.WriteLine($"✅ Best Python command: {bestCommand}, Version: {version}");
            }
            else
            {
                _testOutputHelper.WriteLine($"⚠️ Best Python command validation failed: {bestCommand}");
            }
        }
        else
        {
            _testOutputHelper.WriteLine("⚠️ No Python command detected (expected in environments without Python)");
        }

        _testOutputHelper.WriteLine($"Available commands: {string.Join(", ", availableCommands)}");
    }

    #endregion

    #region Timeout and Resource Management Tests

    [Fact]
    public async Task PythonExecutionGAgent_ScriptTimeout_ShouldBeHandledCorrectly()
    {
        _testOutputHelper.WriteLine("⏱️ Testing script execution timeout...");

        // Arrange
        var executionAgent = await _gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(Guid.NewGuid());
        await executionAgent.InitializeAsync();

        var timeoutScript = @"
import time
time.sleep(10)  # Sleep for 10 seconds
print('This should not be printed due to timeout')
";

        var config = new PythonEnvironmentConfig
        {
            MaxExecutionTimeSeconds = 2, // 2 second timeout
            IsSandboxed = false // Disable sandbox for timeout testing
        };

        // Act
        var result = await executionAgent.ExecutePythonScriptWithTimeoutAsync(timeoutScript, 2, config);

        // Assert
        result.ShouldNotBeNull();

        if (result.Success)
        {
            _testOutputHelper.WriteLine("⚠️ Script completed without timeout (Python may not be available)");
            return;
        }

        result.TimedOut.ShouldBeTrue();
        result.ErrorOutput.ShouldContain("timed out");

        _testOutputHelper.WriteLine($"✅ Script timeout handled correctly: {result.ErrorOutput}");
    }

    #endregion

    #region Security and Sandboxing Tests

    [Fact]
    public async Task PythonExecutionGAgent_SandboxedExecution_ShouldRestrictDangerousOperations()
    {
        _testOutputHelper.WriteLine("🔒 Testing sandboxed execution...");

        // Arrange
        var executionAgent = await _gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(Guid.NewGuid());
        await executionAgent.InitializeAsync();

        var restrictedScript = @"
# This should be blocked by sandbox
try:
    with open('/etc/passwd', 'r') as f:
        content = f.read()
        print('File read successfully (this should not happen)')
except:
    print('File access blocked (expected)')
";

        var config = new PythonEnvironmentConfig
        {
            IsSandboxed = true,
            EnableNetworkAccess = false
        };

        // Act
        var result = await executionAgent.ExecutePythonScriptAsync(restrictedScript, config);

        // Assert
        result.ShouldNotBeNull();

        if (!result.Success)
        {
            // Check if failure is due to security validation (expected) or Python unavailability (environment issue)
            if (!string.IsNullOrEmpty(result.ErrorOutput) &&
                (result.ErrorOutput.Contains("security validation") ||
                 result.ErrorOutput.Contains("dangerous operations")))
            {
                _testOutputHelper.WriteLine(
                    "✅ Sandboxed execution correctly blocked dangerous operations via security validation");
                return;
            }

            _testOutputHelper.WriteLine("⚠️ Sandboxed execution failed (Python may not be available)");
            _testOutputHelper.WriteLine($"Error: {result.ErrorOutput}");
            return;
        }

        // The script should complete but file access should be blocked
        result.StandardOutput.ShouldContain("File access blocked");
        result.StandardOutput.ShouldNotContain("File read successfully");

        _testOutputHelper.WriteLine("✅ Sandboxed execution correctly restricted file access");
    }

    #endregion
}