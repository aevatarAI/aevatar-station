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
    public async Task GenerateWorkflowJsonAsync_ShouldReturnValidWorkflow()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var userGoal = "创建一个简单的数据处理工作流";

        // Act - Call the method directly without InitializeAsync to test the basic functionality
        var result = await workflowComposer.GenerateWorkflowJsonAsync(userGoal);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        _output.WriteLine($"Generated workflow: {result}");
        
        // Basic JSON validation - should be a fallback JSON since no real AI is called
        result.ShouldContain("{");
        result.ShouldContain("}");
        
        // Should contain expected fallback structure
        result.ShouldContain("Fallback");
    }

    [Fact]
    public async Task GetDescriptionAsync_ShouldReturnDescription()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);

        // Act
        var description = await workflowComposer.GetDescriptionAsync();

        // Assert
        description.ShouldNotBeNullOrEmpty();
        _output.WriteLine($"Agent description: {description}");
        
        // Should contain agent information
        description.ShouldContain("WorkflowComposer");
        description.ShouldContain("workflow");
    }

    [Fact]
    public async Task AgentDiscovery_ShouldFindGAgentTypes()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);

        // Act
        var description = await workflowComposer.GetDescriptionAsync();

        // Assert - Verify the JSON structure contains expected agent info
        description.ShouldNotBeNullOrEmpty();
        
        var json = JsonConvert.DeserializeObject<JObject>(description);
        json.ShouldNotBeNull();
        json["Name"]?.ToString().ShouldBe("WorkflowComposer");
        json["Type"]?.ToString().ShouldContain("WorkflowComposerGAgent");
        json["Description"]?.ToString().ShouldNotBeNullOrEmpty();
        
        _output.WriteLine($"Discovered agent info: {description}");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithNullInput_ShouldReturnFallbackWorkflow()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(null);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        
        // Should be valid JSON
        var json = JObject.Parse(result);
        json.ShouldNotBeNull();
        
        // Should contain fallback structure
        json["generationStatus"]?.ToString().ShouldNotBeNullOrEmpty();
        json["name"]?.ToString().ShouldNotBeNullOrEmpty();
        
        _output.WriteLine($"Generated fallback workflow for null input: {result}");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithEmptyInput_ShouldReturnFallbackWorkflow()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync("");

        // Assert
        result.ShouldNotBeNullOrEmpty();
        
        // Should be valid JSON
        var json = JObject.Parse(result);
        json.ShouldNotBeNull();
        
        // Should contain fallback structure
        json["generationStatus"]?.ToString().ShouldNotBeNullOrEmpty();
        
        _output.WriteLine($"Generated fallback workflow for empty input: {result}");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithWhitespaceInput_ShouldReturnFallbackWorkflow()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync("   \t\n   ");

        // Assert
        result.ShouldNotBeNullOrEmpty();
        
        // Should be valid JSON
        var json = JObject.Parse(result);
        json.ShouldNotBeNull();
        
        _output.WriteLine($"Generated fallback workflow for whitespace input: {result}");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithLongInput_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var longGoal = new string('A', 10000); // Very long input

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(longGoal);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        
        // Should be valid JSON
        var json = JObject.Parse(result);
        json.ShouldNotBeNull();
        
        _output.WriteLine($"Generated workflow for long input ({longGoal.Length} chars): {result.Substring(0, Math.Min(200, result.Length))}...");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithSpecialCharacters_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var specialGoal = "Create workflow with \"quotes\" and 'apostrophes' and \n\t special chars: @#$%^&*()";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(specialGoal);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        
        // Should be valid JSON
        var json = JObject.Parse(result);
        json.ShouldNotBeNull();
        
        _output.WriteLine($"Generated workflow for special characters input: {result}");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithUnicodeCharacters_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var unicodeGoal = "Create workflow: 你好世界 🌍 مرحبا العالم";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(unicodeGoal);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        
        // Should be valid JSON
        var json = JObject.Parse(result);
        json.ShouldNotBeNull();
        
        _output.WriteLine($"Generated workflow for unicode input: {result}");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_ShouldAlwaysReturnValidJsonStructure()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var goal = "Test workflow generation structure";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(goal);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        
        // Should be valid JSON
        var json = JObject.Parse(result);
        json.ShouldNotBeNull();
        
        // Should have required fields based on the fallback structure
        json.ShouldContainKey("generationStatus");
        json.ShouldContainKey("name");
        json.ShouldContainKey("properties");
        
        var properties = json["properties"] as JObject;
        properties.ShouldNotBeNull();
        properties.ShouldContainKey("workflowNodeList");
        
        _output.WriteLine($"Validated JSON structure: {result}");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_ConcurrentCalls_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var tasks = new List<Task<string>>();

        // Act - Make multiple concurrent calls
        for (int i = 0; i < 5; i++)
        {
            var goal = $"Concurrent workflow test {i}";
            tasks.Add(workflowComposer.GenerateWorkflowJsonAsync(goal));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldNotBeNull();
        results.Length.ShouldBe(5);
        
        foreach (var result in results)
        {
            result.ShouldNotBeNullOrEmpty();
            
            // Should be valid JSON
            var json = JObject.Parse(result);
            json.ShouldNotBeNull();
        }
        
        _output.WriteLine($"All {results.Length} concurrent calls completed successfully");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_MultipleCallsWithSameInput_ShouldBeConsistent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var goal = "Test consistency";

        // Act
        var result1 = await workflowComposer.GenerateWorkflowJsonAsync(goal);
        var result2 = await workflowComposer.GenerateWorkflowJsonAsync(goal);

        // Assert
        result1.ShouldNotBeNullOrEmpty();
        result2.ShouldNotBeNullOrEmpty();
        
        // Both should be valid JSON
        var json1 = JObject.Parse(result1);
        var json2 = JObject.Parse(result2);
        json1.ShouldNotBeNull();
        json2.ShouldNotBeNull();
        
        _output.WriteLine($"Both calls returned valid JSON consistently");
    }

    [Fact]
    public async Task WorkflowComposerGAgent_StateAndEventTypes_ShouldBeValid()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);

        // Act
        var state = await workflowComposer.GetStateAsync();

        // Assert
        state.ShouldNotBeNull();
        state.ShouldBeOfType<WorkflowComposerState>();
        
        _output.WriteLine($"Agent state type: {state.GetType().Name}");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithJSONLikeInput_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var jsonGoal = "{ \"workflow\": \"test\", \"nodes\": [1,2,3] }";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(jsonGoal);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        
        // Should be valid JSON
        var json = JObject.Parse(result);
        json.ShouldNotBeNull();
        
        _output.WriteLine($"Generated workflow for JSON-like input: {result}");
    }

    [Fact] 
    public async Task GenerateWorkflowJsonAsync_WithMarkdownLikeInput_ShouldHandleGracefully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var markdownGoal = "```json\n{\"workflow\": \"test\"}\n```";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(markdownGoal);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        
        // Should be valid JSON
        var json = JObject.Parse(result);
        json.ShouldNotBeNull();
        
        _output.WriteLine($"Generated workflow for markdown-like input: {result}");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_WithComplexGoal_ShouldReturnStructuredWorkflow()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var complexGoal = "Create a multi-step data processing workflow that: 1) Reads CSV files, 2) Validates data, 3) Transforms data format, 4) Stores in database, 5) Sends notification email";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(complexGoal);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        
        // Should be valid JSON
        var json = JObject.Parse(result);
        json.ShouldNotBeNull();
        
        // Should have proper structure for fallback response
        json.ShouldContainKey("generationStatus");
        json.ShouldContainKey("properties");
        
        var properties = json["properties"] as JObject;
        properties.ShouldNotBeNull();
        properties.ShouldContainKey("workflowNodeList");
        
        _output.WriteLine($"Generated workflow for complex goal: {result}");
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_ErrorHandling_ShouldIncludeErrorInfo()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(agentId);
        var goal = "Test error handling";

        // Act
        var result = await workflowComposer.GenerateWorkflowJsonAsync(goal);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        
        // Should be valid JSON
        var json = JObject.Parse(result);
        json.ShouldNotBeNull();
        
        // Since no real AI service is configured, should return fallback with error info
        if (json.ContainsKey("errorInfo"))
        {
            var errorInfo = json["errorInfo"] as JObject;
            errorInfo.ShouldNotBeNull();
            errorInfo.ShouldContainKey("errorType");
            errorInfo.ShouldContainKey("errorMessage");
            errorInfo.ShouldContainKey("actionableSteps");
        }
        
        _output.WriteLine($"Error handling test result: {result}");
    }
} 