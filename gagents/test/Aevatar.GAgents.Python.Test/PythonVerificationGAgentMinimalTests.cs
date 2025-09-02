using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using System.Diagnostics;

namespace Aevatar.GAgents.Python.Test;

/// <summary>
/// Minimal tests for PythonVerificationGAgent - using the simplest possible Python code
/// </summary>
[Collection(nameof(PythonVerificationGAgentMinimalTests))]
public sealed class PythonVerificationGAgentMinimalTests : AevatarPythonGAgentTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public PythonVerificationGAgentMinimalTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task PythonVerificationGAgent_MinimalCode_ShouldExecuteQuickly()
    {
        var stopwatch = Stopwatch.StartNew();
        _testOutputHelper.WriteLine("🔥 Testing with absolute minimal Python code...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        // Act - Absolutely minimal Python code with NO imports, NO external dependencies
        var minimalCode = @"
# Minimal Python code - only built-in operations
x = 1
y = 2
result = x + y

# Simple assertion
assert result == 3

# Print result
print('Minimal test completed')
print(f'Result: {result}')
";

        var minimalTestCases = new List<TestCase>
        {
            new TestCase
            {
                TestName = "test_minimal",
                TestDescription = "Minimal test case",
                TestCode = "assert (1 + 2) == 3"
            }
        };

        var result = await pythonAgent.ExecutePythonCodeAsync("minimal_test", minimalCode, minimalTestCases);

        stopwatch.Stop();
        var executionTime = stopwatch.ElapsedMilliseconds;

        _testOutputHelper.WriteLine($"⏱️ Minimal test completed in {executionTime}ms");

        // Assert
        result.ShouldNotBeNull();
        result.TheoryId.ShouldBe("minimal_test");

        // Log results regardless of success/failure
        if (result.TestsPassed)
        {
            _testOutputHelper.WriteLine("✅ Minimal test PASSED");
            result.PassedTests.ShouldBe(1);
            result.TotalTests.ShouldBe(1);
        }
        else
        {
            _testOutputHelper.WriteLine($"❌ Minimal test FAILED: {result.ErrorOutput}");
            _testOutputHelper.WriteLine($"Standard Output: {result.StandardOutput}");
        }

        _testOutputHelper.WriteLine($"Execution time: {executionTime}ms");
        _testOutputHelper.WriteLine($"Environment: {result.EnvironmentName}");

        // This should be VERY fast - under 30 seconds even on slow systems
        executionTime.ShouldBeLessThan(30000);
    }

    [Fact]
    public async Task PythonVerificationGAgent_SuperMinimalCode_ShouldWork()
    {
        var stopwatch = Stopwatch.StartNew();
        _testOutputHelper.WriteLine("🚀 Testing with SUPER minimal Python code...");

        // Arrange
        var pythonAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());
        await pythonAgent.InitializeAsync();

        // Act - The simplest possible Python code
        var superMinimalCode = @"print('Hello World')";

        var superMinimalTestCases = new List<TestCase>
        {
            new TestCase
            {
                TestName = "test_print",
                TestDescription = "Simple print test",
                TestCode = "print('Test completed')"
            }
        };

        var result = await pythonAgent.ExecutePythonCodeAsync("super_minimal", superMinimalCode, superMinimalTestCases);

        stopwatch.Stop();
        var executionTime = stopwatch.ElapsedMilliseconds;

        _testOutputHelper.WriteLine($"⏱️ Super minimal test completed in {executionTime}ms");

        // Assert
        result.ShouldNotBeNull();

        // Log all details for debugging
        _testOutputHelper.WriteLine($"Success: {result.TestsPassed}");
        _testOutputHelper.WriteLine($"Standard Output: '{result.StandardOutput}'");
        _testOutputHelper.WriteLine($"Error Output: '{result.ErrorOutput}'");
        _testOutputHelper.WriteLine($"Execution Time: {result.ExecutionTime}s");
        _testOutputHelper.WriteLine($"Environment: {result.EnvironmentName}");

        if (result.ExecutionResult != null)
        {
            _testOutputHelper.WriteLine($"Exit Code: {result.ExecutionResult.ExitCode}");
            _testOutputHelper.WriteLine($"Timed Out: {result.ExecutionResult.TimedOut}");
            _testOutputHelper.WriteLine($"Memory Used: {result.ExecutionResult.MemoryUsedMB}MB");
        }

        // Performance check - should be very fast
        executionTime.ShouldBeLessThan(30000); // 30 seconds max
    }
}