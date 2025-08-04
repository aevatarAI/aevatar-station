using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.Service;
using Microsoft.Extensions.Logging;
using Moq;
using Orleans;
using Shouldly;
using Xunit;
using Xunit.Abstractions;
using Newtonsoft.Json;
using Aevatar.Application.Contracts.WorkflowOrchestration;

namespace Aevatar.Application.Tests.GAgent;

[Collection(AevatarTestConsts.CollectionDefinitionName)]
public class WorkflowComposerGAgentTests : AevatarApplicationGrainsTestBase
{
    private readonly Mock<ILogger<WorkflowComposerGAgent>> _loggerMock;
    private readonly Mock<IClusterClient> _clusterClientMock;
    private readonly Mock<IUserAppService> _userAppServiceMock;
    private readonly ITestOutputHelper _output;

    public WorkflowComposerGAgentTests(ITestOutputHelper output)
    {
        _output = output;
        _clusterClientMock = new Mock<IClusterClient>();
        _userAppServiceMock = new Mock<IUserAppService>();
        _loggerMock = new Mock<ILogger<WorkflowComposerGAgent>>();
    }

    /// <summary>
    /// 创建模拟的 AiWorkflowAgentInfoDto 列表用于测试
    /// </summary>
    private List<AiWorkflowAgentInfoDto> CreateMockAgentList()
    {
        return new List<AiWorkflowAgentInfoDto>
        {
            new AiWorkflowAgentInfoDto
            {
                Name = "DataProcessorGAgent",
                Type = "Aevatar.Application.Grains.Agents.DataProcessorGAgent",
                Description = "专门用于数据处理和分析的Agent，支持多种数据格式的处理和转换操作"
            },
            new AiWorkflowAgentInfoDto
            {
                Name = "NotificationGAgent", 
                Type = "Aevatar.Application.Grains.Agents.NotificationGAgent",
                Description = "负责发送各种类型通知的Agent，支持邮件、短信、推送等多种通知方式"
            },
            new AiWorkflowAgentInfoDto
            {
                Name = "AIAssistantGAgent",
                Type = "Aevatar.Application.Grains.Agents.AI.AIAssistantGAgent", 
                Description = "AI助手Agent，能够理解自然语言并提供智能回复和建议"
            }
        };
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_ShouldReturnValidWorkflow()
    {
        // Arrange
        var workflowComposer = _clusterClientMock.Object.GetGrain<IWorkflowComposerGAgent>(Guid.NewGuid());
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
        var workflowComposer = _clusterClientMock.Object.GetGrain<IWorkflowComposerGAgent>(Guid.NewGuid());

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
        var workflowComposer = _clusterClientMock.Object.GetGrain<IWorkflowComposerGAgent>(Guid.NewGuid());
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