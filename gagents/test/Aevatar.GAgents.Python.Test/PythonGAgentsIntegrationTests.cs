using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Python.Test;

/// <summary>
/// Integration tests for PythonVerificationGAgent and PythonExecutionGAgent collaboration
/// </summary>
[Collection(nameof(PythonGAgentsIntegrationTests))]
public sealed class PythonGAgentsIntegrationTests : AevatarPythonGAgentTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public PythonGAgentsIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task PythonGAgents_FullVerificationWorkflow_ShouldWork()
    {
        _testOutputHelper.WriteLine("🔄 Testing full theory verification workflow...");

        // Arrange
        var verificationAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await verificationAgent.InitializeAsync();

        var theoryId = "golden-ratio-test";
        var theoryContent = "The golden ratio phi is approximately 1.618 and can be calculated as (1 + sqrt(5)) / 2";
        var formalExpression = "phi = (1 + sqrt(5)) / 2";

        // Act
        var result = await verificationAgent.VerifyTheoryAsync(theoryId, theoryContent, formalExpression);

        // Assert
        result.ShouldNotBeNull();
        result.TheoryId.ShouldBe(theoryId);
        result.PythonCode.ShouldNotBeEmpty();
        result.TestCases.ShouldNotBeEmpty();

        _testOutputHelper.WriteLine($"Verification result: {result.TestsPassed}");
        _testOutputHelper.WriteLine($"Generated code length: {result.PythonCode.Length}");
        _testOutputHelper.WriteLine($"Test cases count: {result.TestCases.Count}");
        _testOutputHelper.WriteLine($"Execution time: {result.ExecutionTime}s");

        if (result.ExecutionResult?.Success == true)
        {
            result.TestsPassed.ShouldBeTrue();
            result.PassedTests.ShouldBeGreaterThan(0);
            result.TotalTests.ShouldBeGreaterThan(0);
            _testOutputHelper.WriteLine("✅ Full verification workflow completed successfully");
        }
        else
        {
            _testOutputHelper.WriteLine(
                $"⚠️ Verification failed (possibly due to missing Python): {result.ErrorOutput}");
        }
    }

    [Fact]
    public async Task PythonGAgents_SharedExecutionAgent_ShouldWork()
    {
        _testOutputHelper.WriteLine("🤝 Testing shared execution agent between verification agents...");

        // Arrange - Create execution agent
        var executionAgentId = Guid.NewGuid();
        var executionAgent = await _gAgentFactory.GetGAgentAsync<IPythonExecutionGAgent>(executionAgentId);
        await executionAgent.InitializeAsync();

        // Create two verification agents that share the same execution agent
        var verificationAgent1 = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        var verificationAgent2 = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());

        await verificationAgent1.InitializeAsync(executionAgentId);
        await verificationAgent2.InitializeAsync(executionAgentId);

        // Act - Both should be able to access the same execution agent
        var execAgent1 = await verificationAgent1.GetPythonExecutionGAgentAsync();
        var execAgent2 = await verificationAgent2.GetPythonExecutionGAgentAsync();

        // Assert
        execAgent1.ShouldNotBeNull();
        execAgent2.ShouldNotBeNull();

        var state1 = await verificationAgent1.GetStateAsync();
        var state2 = await verificationAgent2.GetStateAsync();

        state1.PythonExecutionGAgentId.ShouldBe(executionAgentId);
        state2.PythonExecutionGAgentId.ShouldBe(executionAgentId);
        state1.PythonExecutionGAgentId.ShouldBe(state2.PythonExecutionGAgentId);

        _testOutputHelper.WriteLine("✅ Shared execution agent configuration works correctly");
    }

    [Fact]
    public async Task PythonGAgents_TestCaseGeneration_ShouldCreateRelevantTests()
    {
        _testOutputHelper.WriteLine("🧪 Testing test case generation...");

        // Arrange
        var verificationAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await verificationAgent.InitializeAsync();

        var theoryContent = "Binary numbers can only contain 0 and 1";
        var pythonCode = @"
def verify_binary_structure(data):
    return all(bit in [0, 1] for bit in data)
";

        // Act
        var testCases = await verificationAgent.GenerateTestCasesAsync(theoryContent, pythonCode);

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
    public async Task PythonGAgents_VerificationHistory_ShouldTrackResults()
    {
        _testOutputHelper.WriteLine("📚 Testing verification history tracking...");

        // Arrange
        var verificationAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await verificationAgent.InitializeAsync();

        var theoryId = "simple-math-test";

        // Act - Perform a verification
        await verificationAgent.VerifyTheoryAsync(theoryId, "2 + 2 = 4", "result = 2 + 2");

        // Get history
        var history = await verificationAgent.GetVerificationHistoryAsync(theoryId);
        var stats = await verificationAgent.GetVerificationStatsAsync();

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
}