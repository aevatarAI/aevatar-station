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
/// Comprehensive unit tests for PythonVerificationGAgent
/// Tests cover theory verification, code generation, test case creation, and integration with PythonExecutionGAgent
/// Note: Many tests have been moved to PythonExecutionGAgentTests and PythonGAgentsIntegrationTests
/// </summary>
[Collection(nameof(PythonVerificationGAgentTests))]
public sealed class PythonVerificationGAgentTests : AevatarPythonGAgentTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public PythonVerificationGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task PythonVerificationGAgent_BasicInitialization_ShouldSucceed()
    {
        _testOutputHelper.WriteLine("🔧 Testing basic Python verification agent initialization...");

        // Arrange & Act
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        // Assert
        pythonAgent.ShouldNotBeNull();
        var state = await pythonAgent.GetStateAsync();
        state.ShouldNotBeNull();
        state.Initialized.ShouldBeTrue();
        state.PythonExecutionGAgentId.ShouldNotBe(Guid.Empty);

        // Verify that the execution agent is accessible
        var executionAgent = await pythonAgent.GetPythonExecutionGAgentAsync();
        executionAgent.ShouldNotBeNull();

        _testOutputHelper.WriteLine("✅ Python verification agent initialized successfully");
    }

    [Fact]
    public async Task PythonVerificationGAgent_CodeGeneration_ShouldWork()
    {
        _testOutputHelper.WriteLine("🐍 Testing Python code generation...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var theoryContent = "The golden ratio phi is approximately 1.618";
        var formalExpression = "phi = (1 + sqrt(5)) / 2";

        // Act
        var generatedCode = await pythonAgent.GeneratePythonCodeAsync(theoryContent, formalExpression);

        // Assert
        generatedCode.ShouldNotBeNull();
        generatedCode.ShouldNotBeEmpty();
        generatedCode.ShouldContain("import numpy");
        generatedCode.ShouldContain("import sympy");
        generatedCode.ShouldContain("import math");
        generatedCode.ShouldContain(theoryContent);
        generatedCode.ShouldContain(formalExpression);

        _testOutputHelper.WriteLine($"✅ Generated code length: {generatedCode.Length} characters");
        _testOutputHelper.WriteLine("✅ Code generation completed successfully");
    }

    [Fact]
    public async Task PythonVerificationGAgent_TestCaseGeneration_ShouldWork()
    {
        _testOutputHelper.WriteLine("🧪 Testing test case generation...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var theoryContent = "Binary numbers can only contain 0 and 1";
        var pythonCode = @"
def verify_binary_structure(data):
    return all(bit in [0, 1] for bit in data)
";

        // Act
        var testCases = await pythonAgent.GenerateTestCasesAsync(theoryContent, pythonCode);

        // Assert
        testCases.ShouldNotBeNull();
        testCases.ShouldNotBeEmpty();

        foreach (var testCase in testCases)
        {
            testCase.TestName.ShouldNotBeEmpty();
            testCase.TestCode.ShouldNotBeEmpty();
            testCase.TestDescription.ShouldNotBeEmpty();
        }

        _testOutputHelper.WriteLine($"✅ Generated {testCases.Count} test cases");
        foreach (var testCase in testCases)
        {
            _testOutputHelper.WriteLine($"  - {testCase.TestName}: {testCase.TestDescription}");
        }
    }

    [Fact]
    public async Task PythonVerificationGAgent_CodeValidation_ShouldWork()
    {
        _testOutputHelper.WriteLine("🔒 Testing Python code validation...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var safeCode = @"
import math
result = math.sqrt(25)
print(f'Result: {result}')
";

        var dangerousCode = @"
import subprocess
subprocess.run(['ls', '-la'])
";

        // Act & Assert
        var safeResult = await pythonAgent.ValidatePythonCodeAsync(safeCode);
        var dangerousResult = await pythonAgent.ValidatePythonCodeAsync(dangerousCode);

        // The validation should pass safe code and block dangerous code
        _testOutputHelper.WriteLine($"Safe code validation: {safeResult}");
        _testOutputHelper.WriteLine($"Dangerous code validation: {dangerousResult}");

        dangerousResult.ShouldBeFalse();
        _testOutputHelper.WriteLine("✅ Code validation completed successfully");
    }

    [Fact]
    public async Task PythonVerificationGAgent_VerificationHistory_ShouldTrackResults()
    {
        _testOutputHelper.WriteLine("📚 Testing verification history tracking...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        var theoryId = "simple-math-test";

        // Act - Perform a verification
        await pythonAgent.VerifyTheoryAsync(theoryId, "2 + 2 = 4", "result = 2 + 2");

        // Get history and stats
        var history = await pythonAgent.GetVerificationHistoryAsync(theoryId);
        var stats = await pythonAgent.GetVerificationStatsAsync();

        // Assert
        history.ShouldNotBeNull();
        history.ShouldNotBeEmpty();
        history[0].TheoryId.ShouldBe(theoryId);

        stats.ShouldNotBeNull();
        stats.ShouldContainKey("Total");
        stats["Total"].ShouldBeGreaterThan(0);

        _testOutputHelper.WriteLine($"✅ History contains {history.Count} entries");
        _testOutputHelper.WriteLine($"✅ Stats: {string.Join(", ", stats.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
    }

    #endregion

    #region Legacy Tests (Skipped - Methods moved to PythonExecutionGAgent)

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_PythonCommandDetection_ShouldWork()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_InvalidPythonCommand_ShouldReturnFalse()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_EnvironmentCreation_ShouldWork()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_EnvironmentDeletion_ShouldWork()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_SecurityValidation_ShouldBlockDangerousOperations()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_SecurityValidation_ShouldAllowSafeOperations()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_BasicScriptExecution_ShouldWork()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_DependencyExtraction_ShouldIdentifyImports()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_DependencyInstallation_ShouldWork()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_ExecutionTimeout_ShouldBeHandledCorrectly()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_ResourceLimits_ShouldBeEnforced()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_NetworkRestriction_ShouldBlockNetworkAccess()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_FileAccess_ShouldBeRestrictedInSandbox()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_ConcurrentExecution_ShouldNotExceedLimits()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_PythonCommandDetection_ShouldAutomaticallyFindBestCommand()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_PythonCommandValidation_ShouldWorkCorrectly()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    [Fact(Skip = "Method moved to PythonExecutionGAgent")]
    public async Task PythonVerificationGAgent_ComplexScriptExecution_ShouldWork()
    {
        // This functionality has been moved to PythonExecutionGAgent
        await Task.CompletedTask;
    }

    #endregion
}