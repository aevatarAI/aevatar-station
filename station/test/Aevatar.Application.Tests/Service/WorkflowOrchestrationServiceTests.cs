using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Aevatar.Application.Service;
using Aevatar.Core.Abstractions;
using Aevatar.Options;
using Aevatar.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Orleans;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Service;

/// <summary>
/// 工作流编排服务单元测试（简化版）
/// </summary>
public class WorkflowOrchestrationServiceTests
{
    private readonly Mock<ILogger<WorkflowOrchestrationService>> _mockLogger;
    private readonly Mock<IClusterClient> _mockClusterClient;
    private readonly Mock<IUserAppService> _mockUserAppService;
    private readonly Mock<IGAgentManager> _mockGAgentManager;
    private readonly Mock<IGAgentFactory> _mockGAgentFactory;
    private readonly Mock<IOptionsMonitor<AIServicePromptOptions>> _mockPromptOptions;
    private readonly Mock<IWorkflowComposerGAgent> _mockWorkflowComposerGAgent;
    private readonly WorkflowOrchestrationService _service;
    private readonly AIServicePromptOptions _promptOptions;

    public WorkflowOrchestrationServiceTests()
    {
        _mockLogger = new Mock<ILogger<WorkflowOrchestrationService>>();
        _mockClusterClient = new Mock<IClusterClient>();
        _mockUserAppService = new Mock<IUserAppService>();
        _mockGAgentManager = new Mock<IGAgentManager>();
        _mockGAgentFactory = new Mock<IGAgentFactory>();
        _mockPromptOptions = new Mock<IOptionsMonitor<AIServicePromptOptions>>();
        _mockWorkflowComposerGAgent = new Mock<IWorkflowComposerGAgent>();

        // Setup default prompt options
        _promptOptions = new AIServicePromptOptions
        {
            SystemRoleTemplate = "You are a workflow orchestration AI assistant.",
            AgentCatalogSectionTemplate = "Available Agents:\n{AGENT_CATALOG_CONTENT}",
            OutputRequirementsTemplate = "Generate a valid JSON workflow.",
            JsonFormatSpecificationTemplate = "JSON format specification",
            NoAgentsAvailableMessage = "No agents available for workflow creation.",
            SingleAgentTemplate = "Agent: {AGENT_NAME}\nType: {AGENT_TYPE}\nDescription: {AGENT_DESCRIPTION}"
        };

        _mockPromptOptions.Setup(x => x.CurrentValue).Returns(_promptOptions);

        _service = new WorkflowOrchestrationService(
            _mockLogger.Object,
            _mockClusterClient.Object,
            _mockUserAppService.Object,
            _mockGAgentManager.Object,
            _mockGAgentFactory.Object,
            _mockPromptOptions.Object);
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithValidUserGoal_ShouldReturnWorkflowConfig()
    {
        // Arrange
        var userGoal = "Create a data processing workflow";
        var userId = Guid.NewGuid();
        var availableAgentTypes = CreateMockAgentTypes();
        var mockWorkflowJson = CreateValidWorkflowJson();

        SetupMockDependencies(userId, availableAgentTypes, mockWorkflowJson);

        // Act
        var result = await _service.GenerateWorkflowAsync(userGoal);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Workflow");
        result.Properties.ShouldNotBeNull();
        result.Properties.WorkflowNodeList.ShouldNotBeEmpty();
        result.Properties.WorkflowNodeUnitList.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GenerateWorkflowAsync_WithEmptyUserGoal_ShouldReturnNull(string userGoal)
    {
        // Act
        var result = await _service.GenerateWorkflowAsync(userGoal);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithNoAvailableAgents_ShouldUseEmptyWorkflow()
    {
        // Arrange
        var userGoal = "Create a workflow";
        var userId = Guid.NewGuid();
        var emptyAgentTypes = new List<Type>();
        var emptyWorkflowJson = CreateEmptyWorkflowJson();

        SetupMockDependencies(userId, emptyAgentTypes, emptyWorkflowJson);

        // Act
        var result = await _service.GenerateWorkflowAsync(userGoal);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Empty Workflow");
        result.Properties.WorkflowNodeList.ShouldBeEmpty();
        result.Properties.WorkflowNodeUnitList.ShouldBeEmpty();
    }

    #region Helper Methods

    private void SetupMockDependencies(Guid userId, List<Type> agentTypes, string workflowJson)
    {
        _mockUserAppService.Setup(x => x.GetCurrentUserId()).Returns(userId);
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        _mockClusterClient.Setup(x => x.GetGrain<IWorkflowComposerGAgent>(It.IsAny<Guid>(), null))
            .Returns(_mockWorkflowComposerGAgent.Object);
        
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(workflowJson);
    }

    private List<Type> CreateMockAgentTypes()
    {
        return new List<Type>
        {
            typeof(MockDataProcessorAgent),
            typeof(MockValidatorAgent),
            typeof(MockReportGeneratorAgent)
        };
    }

    private string CreateValidWorkflowJson()
    {
        return @"{
            ""name"": ""Test Workflow"",
            ""properties"": {
                ""name"": ""Test Workflow"",
                ""workflowNodeList"": [
                    {
                        ""nodeId"": ""node1"",
                        ""nodeType"": ""MockDataProcessorAgent"",
                        ""nodeName"": ""Data Processor"",
                        ""properties"": {},
                        ""extendedData"": {
                            ""xPosition"": ""100"",
                            ""yPosition"": ""100"",
                            ""description"": ""Processes input data""
                        }
                    },
                    {
                        ""nodeId"": ""node2"",
                        ""nodeType"": ""MockValidatorAgent"",
                        ""nodeName"": ""Validator"",
                        ""properties"": {},
                        ""extendedData"": {
                            ""xPosition"": ""300"",
                            ""yPosition"": ""100"",
                            ""description"": ""Validates processed data""
                        }
                    }
                ],
                ""workflowNodeUnitList"": [
                    {
                        ""fromNodeId"": ""node1"",
                        ""toNodeId"": ""node2""
                    }
                ]
            }
        }";
    }

    private string CreateEmptyWorkflowJson()
    {
        return @"{
            ""name"": ""Empty Workflow"",
            ""properties"": {
                ""name"": ""Empty Workflow"",
                ""workflowNodeList"": [],
                ""workflowNodeUnitList"": []
            }
        }";
    }

    #endregion

    #region Mock Agent Types

    public class MockDataProcessorAgent { }
    
    [System.ComponentModel.Description("Mock validator agent for testing")]
    public class MockValidatorAgent { }
    
    [System.ComponentModel.Description("Mock report generator for workflow testing")]
    public class MockReportGeneratorAgent { }

    #endregion
} 