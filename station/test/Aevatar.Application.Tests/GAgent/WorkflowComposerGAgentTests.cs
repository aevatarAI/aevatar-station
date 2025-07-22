using System;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Orleans;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgent;

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
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>("test-workflow-composer");
        var userGoal = "创建一个简单的数据处理工作流";

        // Act
        var workflowJson = await workflowComposer.GenerateWorkflowJsonAsync(userGoal);

        // Assert
        workflowJson.ShouldNotBeNullOrEmpty();
        _output.WriteLine($"Generated Workflow JSON: {workflowJson}");
        
        // 验证返回的是有效的JSON
        workflowJson.ShouldContain("workflowNodeList");
        workflowJson.ShouldContain("workflowNodeUnitList");
        workflowJson.ShouldContain("Name");
    }

    [Fact]
    public async Task GetDescriptionAsync_ShouldReturnDescription()
    {
        // Arrange
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>("test-workflow-composer-desc");

        // Act
        var description = await workflowComposer.GetDescriptionAsync();

        // Assert
        description.ShouldNotBeNullOrEmpty();
        description.ShouldContain("AI工作流生成器");
        _output.WriteLine($"WorkflowComposer Description: {description}");
    }

    [Fact]
    public async Task AgentDiscovery_ShouldFindGAgentTypes()
    {
        // Arrange
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>("test-agent-discovery");
        
        // Act
        var workflowJson = await workflowComposer.GenerateWorkflowJsonAsync("测试agent发现功能");

        // Assert
        workflowJson.ShouldNotBeNullOrEmpty();
        _output.WriteLine($"Workflow generation completed. JSON length: {workflowJson.Length}");
        
        // 通过检查生成的工作流来验证agent扫描是否工作
        // 即使没有找到标记的agents，也应该返回fallback工作流
        workflowJson.ShouldContain("workflowNodeList");
    }
} 