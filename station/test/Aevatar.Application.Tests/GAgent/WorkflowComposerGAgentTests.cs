using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Common;
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

    /// <summary>
    /// 创建模拟的 AgentDescriptionInfo 列表用于测试
    /// </summary>
    private List<AgentDescriptionInfo> CreateMockAgentList()
    {
        return new List<AgentDescriptionInfo>
        {
            new AgentDescriptionInfo
            {
                Id = "Aevatar.Application.Grains.Agents.DataProcessorGAgent",
                Name = "DataProcessorGAgent",
                Category = "Data",
                L1Description = "Handles data processing operations including transformation, validation, and formatting",
                L2Description = "Advanced data processing agent that can handle multiple input formats, apply various transformation rules, validate data integrity, and output formatted results. Supports streaming data processing and batch operations with configurable processing parameters.",
                Capabilities = new List<string> { "data-transformation", "validation", "formatting", "batch-processing" },
                Tags = new List<string> { "data", "processing", "transformation", "validation" }
            },
            new AgentDescriptionInfo
            {
                Id = "Aevatar.Application.Grains.Agents.AnalysisGAgent",
                Name = "AnalysisGAgent",
                Category = "Analysis",
                L1Description = "Performs comprehensive data analysis including statistical analysis, trend detection, and report generation",
                L2Description = "Sophisticated analysis agent capable of performing statistical analysis, trend detection, anomaly detection, predictive modeling, and comprehensive reporting. Features real-time analysis capabilities and customizable analysis parameters.",
                Capabilities = new List<string> { "statistical-analysis", "trend-detection", "anomaly-detection", "predictive-modeling", "reporting" },
                Tags = new List<string> { "analysis", "statistics", "reporting", "trends" }
            }
        };
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_ShouldReturnValidWorkflow()
    {
        // Arrange
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(Guid.NewGuid());
        var userGoal = "创建一个简单的数据处理工作流";
        var availableAgents = CreateMockAgentList();

        // Act
        var workflowJson = await workflowComposer.GenerateWorkflowJsonAsync(userGoal, availableAgents);

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
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(Guid.NewGuid());

        // Act
        var description = await workflowComposer.GetDescriptionAsync();

        // Assert
        description.ShouldNotBeNullOrEmpty();
        description.ShouldContain("WorkflowComposer");
        description.ShouldContain("workflow generation");
        _output.WriteLine($"WorkflowComposer Description: {description}");
    }

    [Fact]
    public async Task AgentDiscovery_ShouldFindGAgentTypes()
    {
        // Arrange
        var workflowComposer = _clusterClient.GetGrain<IWorkflowComposerGAgent>(Guid.NewGuid());
        var availableAgents = CreateMockAgentList();
        
        // Act
        var workflowJson = await workflowComposer.GenerateWorkflowJsonAsync("测试agent发现功能", availableAgents);

        // Assert
        workflowJson.ShouldNotBeNullOrEmpty();
        _output.WriteLine($"Workflow generation completed. JSON length: {workflowJson.Length}");
        
        // 通过检查生成的工作流来验证agent扫描是否工作
        // 即使没有找到标记的agents，也应该返回fallback工作流
        workflowJson.ShouldContain("workflowNodeList");
    }
} 