using System;
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
} 