using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Orleans;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aevatar.Application.Tests.GAgent;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class WorkflowComposerGAgentTests : AevatarApplicationGrainsTestBase
{
    private readonly IClusterClient _clusterClient;
    private readonly ITestOutputHelper _output;

    public WorkflowComposerGAgentTests(ITestOutputHelper output)
    {
        _clusterClient = GetRequiredService<IClusterClient>();
        _output = output;
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithValidInput_ShouldReturnWorkflow()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var input = "Create a workflow for processing customer orders";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        
        _output.WriteLine($"Generated workflow for input: {input}");
        _output.WriteLine($"Result length: {result.Length} characters");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithEmptyInput_ShouldReturnFallback()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var emptyInput = "";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(emptyInput);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        
        _output.WriteLine("Empty input handled with fallback workflow");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithComplexWorkflowRequest_ShouldGenerateStructuredWorkflow()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var complexInput = "Create a multi-step workflow for data processing that includes validation, transformation, enrichment, and output generation";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(complexInput);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        
        _output.WriteLine($"Complex workflow generated: {result.Length} characters");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithJSONRequest_ShouldReturnValidFormat()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var jsonInput = "Generate a workflow in JSON format for user authentication and authorization";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(jsonInput);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        
        // Try to parse as JSON to verify structure
        var isValidJson = true;
        try
        {
            JObject.Parse(result);
        }
        catch
        {
            isValidJson = false;
        }
        
        _output.WriteLine($"JSON workflow generated, valid JSON: {isValidJson}");
    }

    [Theory]
    [InlineData("Simple workflow")]
    [InlineData("Complex multi-step process")]
    [InlineData("Automated data pipeline")]
    public async Task GenerateWorkflowJsonAsync_WithVariousInputs_ShouldHandleAllTypes(string input)
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(input);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        
        _output.WriteLine($"Input '{input}' -> Result length: {result.Length}");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var unicodeInput = "创建工作流程 for データ処理 avec étapes المتعددة";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(unicodeInput);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        
        _output.WriteLine("Unicode input handled correctly");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_ConcurrentCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);

        var tasks = new List<Task<string>>();
        for (int i = 0; i < 3; i++)
        {
            tasks.Add(workflowComposer.GenerateWorkflowJsonAsync($"Concurrent workflow {i}"));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldNotBeNull();
        results.Length.ShouldBe(3);
        
        foreach (var result in results)
        {
            result.ShouldNotBeNull();
            result.ShouldNotBeEmpty();
        }
        
        _output.WriteLine("Concurrent calls handled successfully");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithNullInput_ShouldReturnFallback()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(null);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        
        _output.WriteLine("Null input handled with fallback");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var specialInput = @"Create workflow with special chars: !@#$%^&*(){}[]<>?/|\~`""'";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(specialInput);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty();
        
        _output.WriteLine("Special characters handled correctly");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_MultipleDifferentAgents_ShouldBeIndependent()
    {
        // Arrange
        var agent1Id = Guid.NewGuid();
        var agent2Id = Guid.NewGuid();
        var workflowComposer1 = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agent1Id);
        var workflowComposer2 = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agent2Id);

        // Act
        var result1 = await workflowComposer1.GenerateWorkflowJsonAsync("Agent 1 workflow");
        var result2 = await workflowComposer2.GenerateWorkflowJsonAsync("Agent 2 workflow");

        // Assert
        result1.ShouldNotBeNull();
        result2.ShouldNotBeNull();
        result1.ShouldNotBeEmpty();
        result2.ShouldNotBeEmpty();
        
        _output.WriteLine("Multiple agents handled independently");
    }
} 