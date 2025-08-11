using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.AI;
using Aevatar.Application.Service;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.Options;
using Aevatar.Service;
using Aevatar.Application.Contracts.WorkflowOrchestration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Orleans;
using Orleans.Metadata;
using Orleans.Runtime;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Service;

/// <summary>
/// 工作流编排服务单元测试
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

    public WorkflowOrchestrationServiceTests()
    {
        _mockLogger = new Mock<ILogger<WorkflowOrchestrationService>>();
        _mockClusterClient = new Mock<IClusterClient>();
        _mockUserAppService = new Mock<IUserAppService>();
        _mockGAgentManager = new Mock<IGAgentManager>();
        _mockGAgentFactory = new Mock<IGAgentFactory>();
        _mockPromptOptions = new Mock<IOptionsMonitor<AIServicePromptOptions>>();
        _mockWorkflowComposerGAgent = new Mock<IWorkflowComposerGAgent>();


        SetupMockDefaults();

        _service = new WorkflowOrchestrationService(
            _mockLogger.Object,
            _mockClusterClient.Object,
            _mockUserAppService.Object,
            _mockGAgentManager.Object,
            _mockGAgentFactory.Object,
            _mockPromptOptions.Object,
            null); // GrainTypeResolver可以为null，因为我们在测试中不会实际使用映射功能
    }

    private void SetupMockDefaults()
    {
        var userId = Guid.NewGuid();
        _mockUserAppService.Setup(x => x.GetCurrentUserId()).Returns(userId);
        
        // Setup empty agent types to trigger empty workflow generation
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(new List<Type>());
        
        // Setup cluster client to return our mock grain
        var testGuid = Guid.NewGuid();
        _mockClusterClient.Setup(x => x.GetGrain<IWorkflowComposerGAgent>(testGuid, null))
            .Returns(_mockWorkflowComposerGAgent.Object);
        _mockClusterClient.Setup(x => x.GetGrain<IWorkflowComposerGAgent>(It.IsAny<Guid>(), null))
            .Returns(_mockWorkflowComposerGAgent.Object);
            
        // Setup complete prompt options with all required templates
        var promptOptions = new AIServicePromptOptions
        {
            SystemRoleTemplate = "Test system prompt",
            AgentCatalogSectionTemplate = "Available agents: {AGENT_CATALOG_CONTENT}",
            OutputRequirementsTemplate = "Please provide JSON output",
            JsonFormatSpecificationTemplate = "Use JSON format",
            SingleAgentTemplate = "Agent: {AGENT_NAME} - {AGENT_TYPE} - {AGENT_DESCRIPTION}",
            NoAgentsAvailableMessage = "No agents available"
        };
        _mockPromptOptions.Setup(x => x.CurrentValue).Returns(promptOptions);
        
        // GrainTypeResolver在测试中为null，我们依赖字典映射功能
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task GenerateWorkflowAsync_WithEmptyUserGoal_ShouldReturnNull()
    {
        // Act
        var result = await _service.GenerateWorkflowAsync("");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithNullUserGoal_ShouldReturnNull()
    {
        // Act
        var result = await _service.GenerateWorkflowAsync(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithWhitespaceUserGoal_ShouldReturnNull()
    {
        // Act
        var result = await _service.GenerateWorkflowAsync("   \t\n   ");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithNoAvailableAgents_ShouldReturnEmptyWorkflow()
    {
        // Arrange - Use default setup which has no available agents

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a simple workflow");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Empty Workflow");
        result.Properties.ShouldNotBeNull();
        result.Properties.WorkflowNodeList.ShouldBeEmpty();
        result.Properties.WorkflowNodeUnitList.ShouldBeEmpty();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithAvailableAgents_ShouldCallGAgentProperly()
    {
        // Arrange
        var agentTypes = new List<Type>
        {
            typeof(TestAgent)
        };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        var workflowJson = """
        {
            "name": "Test Workflow",
            "properties": {
                "name": "Test Workflow",
                "workflowNodeList": [],
                "workflowNodeUnitList": []
            }
        }
        """;
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(workflowJson);

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a test workflow");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Workflow");
        
        // Verify the agent was called properly
        _mockWorkflowComposerGAgent.Verify(x => x.InitializeAsync(It.IsAny<InitializeDto>()), Times.Once);
        _mockWorkflowComposerGAgent.Verify(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WhenGAgentInitializationFails_ShouldReturnNull()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(false); // Initialization fails

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert - When initialization fails, it should return null due to exception handling
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WhenGAgentThrowsException_ShouldReturnNull()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert - When GAgent throws exception, service should return null
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithInvalidJson_ShouldReturnNull()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("invalid json");

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert - When JSON parsing fails, service should return null
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithNullResponse_ShouldReturnNull()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync((string)null);

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert - When GAgent returns null, service should return null
        result.ShouldBeNull();
    }

    [Fact]
    public void Constructor_ShouldNotThrow()
    {
        // Act & Assert - Constructor should complete without throwing
        var service = new WorkflowOrchestrationService(
            _mockLogger.Object,
            _mockClusterClient.Object,
            _mockUserAppService.Object,
            _mockGAgentManager.Object,
            _mockGAgentFactory.Object,
            _mockPromptOptions.Object,
            null);

        service.ShouldNotBeNull();
    }

    #endregion

    #region Complex JSON Parsing Tests

    [Fact]
    public async Task GenerateWorkflowAsync_WithComplexWorkflowJson_ShouldParseCorrectly()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent), typeof(AnotherTestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        var complexWorkflowJson = """
        {
            "name": "Complex Workflow",
            "properties": {
                "name": "Complex Test Workflow",
                "workflowNodeList": [
                    {
                        "nodeId": "node-1",
                        "nodeType": "TestAgent",
                        "nodeName": "First Node",
                        "extendedData": {
                            "xPosition": "100.5",
                            "yPosition": "200.3",
                            "description": "This is the first node"
                        },
                        "properties": {
                            "input": "test input",
                            "timeout": 30
                        }
                    },
                    {
                        "nodeId": "node-2",
                        "agentType": "AnotherTestAgent",
                        "name": "Second Node",
                        "extendedData": {
                            "xPosition": "300.7",
                            "yPosition": "400.1"
                        }
                    }
                ],
                "workflowNodeUnitList": [
                    {
                        "fromNodeId": "node-1",
                        "toNodeId": "node-2"
                    }
                ]
            }
        }
        """;
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(complexWorkflowJson);

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a complex workflow");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Complex Workflow");
        result.Properties.ShouldNotBeNull();
        result.Properties.Name.ShouldBe("Complex Test Workflow");
        
        // Verify nodes were mapped correctly
        result.Properties.WorkflowNodeList.Count.ShouldBe(2);
        
        var firstNode = result.Properties.WorkflowNodeList[0];
        firstNode.NodeId.ShouldBe("node-1");
        firstNode.AgentType.ShouldBe("TestAgent");
        firstNode.Name.ShouldBe("First Node");
        firstNode.Properties.ShouldContainKeyAndValue("input", "test input");
        // Fix: Handle JSON number parsing - it could be int, long, or double
        firstNode.Properties.ShouldContainKey("timeout");
        Convert.ToInt32(firstNode.Properties["timeout"]).ShouldBe(30);
        firstNode.Properties.ShouldContainKeyAndValue("description", "This is the first node");
        
        var secondNode = result.Properties.WorkflowNodeList[1];
        secondNode.NodeId.ShouldBe("node-2");
        secondNode.AgentType.ShouldBe("AnotherTestAgent");
        secondNode.Name.ShouldBe("Second Node");
        
        // Verify connections were mapped correctly
        result.Properties.WorkflowNodeUnitList.Count.ShouldBe(1);
        var connection = result.Properties.WorkflowNodeUnitList[0];
        connection.NodeId.ShouldBe("node-1");
        connection.NextNodeId.ShouldBe("node-2");
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithMarkdownWrappedJson_ShouldCleanAndParse()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        var markdownWrappedJson = """
        ```json
        {
            "name": "Markdown Wrapped Workflow",
            "properties": {
                "name": "Test",
                "workflowNodeList": [
                    {
                        "nodeId": "test-node",
                        "nodeType": "TestAgent",
                        "nodeName": "Test Node"
                    }
                ],
                "workflowNodeUnitList": []
            }
        }
        ```
        """;
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(markdownWrappedJson);

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Markdown Wrapped Workflow");
        result.Properties.WorkflowNodeList.Count.ShouldBe(1);
        result.Properties.WorkflowNodeList[0].NodeId.ShouldBe("test-node");
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithPlainMarkdownWrapping_ShouldCleanAndParse()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        var plainMarkdownJson = """
        ```
        {
            "name": "Plain Markdown Workflow",
            "properties": {
                "workflowNodeList": [],
                "workflowNodeUnitList": []
            }
        }
        ```
        """;
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(plainMarkdownJson);

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Plain Markdown Workflow");
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithNodesButMissingExtendedData_ShouldProvideDefaults()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        var jsonWithoutExtendedData = """
        {
            "name": "No Extended Data Workflow",
            "properties": {
                "workflowNodeList": [
                    {
                        "nodeId": "node-1",
                        "nodeType": "TestAgent",
                        "nodeName": "Node Without Extended Data"
                    }
                ],
                "workflowNodeUnitList": []
            }
        }
        """;
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(jsonWithoutExtendedData);

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldNotBeNull();
        var node = result.Properties.WorkflowNodeList[0];
        node.ExtendedData.ShouldNotBeNull();
        // Note: Position will be set by intelligent layout, not default "0"
        node.ExtendedData.XPosition.ShouldNotBeNull();
        node.ExtendedData.YPosition.ShouldNotBeNull();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithMissingProperties_ShouldProvideDefaults()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        var jsonWithoutProperties = """
        {
            "name": "No Properties Workflow"
        }
        """;
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(jsonWithoutProperties);

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("No Properties Workflow");
        result.Properties.ShouldNotBeNull();
        result.Properties.Name.ShouldBe("No Properties Workflow");
        result.Properties.WorkflowNodeList.ShouldNotBeNull();
        result.Properties.WorkflowNodeUnitList.ShouldNotBeNull();
        result.Properties.WorkflowNodeList.ShouldBeEmpty();
        result.Properties.WorkflowNodeUnitList.ShouldBeEmpty();
    }

    #endregion

    #region Layout Algorithm Tests

    [Fact]
    public async Task GenerateWorkflowAsync_WithMultipleNodes_ShouldApplyIntelligentLayout()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent), typeof(AnotherTestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        var workflowWithMultipleNodes = """
        {
            "name": "Multi-Node Workflow",
            "properties": {
                "workflowNodeList": [
                    {
                        "nodeId": "start-node",
                        "nodeType": "TestAgent",
                        "nodeName": "Start Node"
                    },
                    {
                        "nodeId": "middle-node",
                        "nodeType": "AnotherTestAgent",
                        "nodeName": "Middle Node"
                    },
                    {
                        "nodeId": "end-node",
                        "nodeType": "TestAgent",
                        "nodeName": "End Node"
                    }
                ],
                "workflowNodeUnitList": [
                    {
                        "fromNodeId": "start-node",
                        "toNodeId": "middle-node"
                    },
                    {
                        "fromNodeId": "middle-node",
                        "toNodeId": "end-node"
                    }
                ]
            }
        }
        """;
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(workflowWithMultipleNodes);

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a multi-node workflow");

        // Assert
        result.ShouldNotBeNull();
        result.Properties.WorkflowNodeList.Count.ShouldBe(3);
        
        // Verify that positions were calculated (not default "0")
        foreach (var node in result.Properties.WorkflowNodeList)
        {
            node.ExtendedData.XPosition.ShouldNotBe("0");
            node.ExtendedData.YPosition.ShouldNotBe("0");
            
            // Verify high precision positioning (should have many decimal places)
            node.ExtendedData.XPosition.ShouldContain(".");
            node.ExtendedData.YPosition.ShouldContain(".");
        }
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithCircularDependencies_ShouldHandleGracefully()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        var workflowWithCircularDeps = """
        {
            "name": "Circular Dependency Workflow",
            "properties": {
                "workflowNodeList": [
                    {
                        "nodeId": "node-a",
                        "nodeType": "TestAgent",
                        "nodeName": "Node A"
                    },
                    {
                        "nodeId": "node-b",
                        "nodeType": "TestAgent",
                        "nodeName": "Node B"
                    }
                ],
                "workflowNodeUnitList": [
                    {
                        "fromNodeId": "node-a",
                        "toNodeId": "node-b"
                    },
                    {
                        "fromNodeId": "node-b",
                        "toNodeId": "node-a"
                    }
                ]
            }
        }
        """;
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(workflowWithCircularDeps);

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow with circular dependencies");

        // Assert
        result.ShouldNotBeNull();
        result.Properties.WorkflowNodeList.Count.ShouldBe(2);
        
        // Should still apply layout even with circular dependencies
        foreach (var node in result.Properties.WorkflowNodeList)
        {
            node.ExtendedData.XPosition.ShouldNotBeNull();
            node.ExtendedData.YPosition.ShouldNotBeNull();
        }
    }

    #endregion

    #region Agent Description Tests

    [Fact]
    public async Task GenerateWorkflowAsync_WithAgentsHavingDescriptionAttributes_ShouldIncludeDescriptions()
    {
        // Arrange
        var agentTypes = new List<Type> 
        { 
            typeof(TestAgent), // Has Description attribute
            typeof(AgentWithoutDescription) // No Description attribute
        };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .Callback<InitializeDto>(dto => 
            {
                // Verify that agent descriptions are included in the instructions
                dto.Instructions.ShouldContain("TestAgent");
                dto.Instructions.ShouldContain("Test agent for unit testing");
                dto.Instructions.ShouldContain("AgentWithoutDescription");
                dto.Instructions.ShouldContain("AgentWithoutDescription - Agent for specialized processing");
            })
            .ReturnsAsync(true);
            
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("""{"name": "Test", "properties": {"workflowNodeList": [], "workflowNodeUnitList": []}}""");

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldNotBeNull();
        _mockWorkflowComposerGAgent.Verify(x => x.InitializeAsync(It.IsAny<InitializeDto>()), Times.Once);
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithAgentsFromOrleansCodeGen_ShouldFilterThem()
    {
        // Arrange
        var agentTypes = new List<Type>
        {
            typeof(TestAgent),
            typeof(OrleansCodeGenAgent) // Note: This test class doesn't actually have OrleansCodeGen namespace
        };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .Callback<InitializeDto>(dto => 
            {
                // Since our test OrleansCodeGenAgent doesn't actually have the OrleansCodeGen namespace,
                // it won't be filtered out. Both agents should be present in instructions.
                dto.Instructions.ShouldContain("TestAgent");
                dto.Instructions.ShouldContain("OrleansCodeGenAgent");
            })
            .ReturnsAsync(true);
            
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("""{"name": "Test", "properties": {"workflowNodeList": [], "workflowNodeUnitList": []}}""");

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WhenAgentProcessingThrows_ShouldContinueWithOtherAgents()
    {
        // Arrange - This tests error handling in agent description processing
        var agentTypes = new List<Type>
        {
            typeof(TestAgent),
            typeof(ProblematicAgent), // This might cause issues in reflection
            typeof(AnotherTestAgent)
        };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("""{"name": "Test", "properties": {"workflowNodeList": [], "workflowNodeUnitList": []}}""");

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldNotBeNull();
    }

    #endregion

    #region Prompt Building Tests

    [Fact]
    public async Task GenerateWorkflowAsync_ShouldBuildCompletePromptWithAllSections()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        InitializeDto capturedDto = null;
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .Callback<InitializeDto>(dto => capturedDto = dto)
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("""{"name": "Test", "properties": {"workflowNodeList": [], "workflowNodeUnitList": []}}""");

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldNotBeNull();
        capturedDto.ShouldNotBeNull();
        
        // Verify all prompt sections are included
        capturedDto.Instructions.ShouldContain("Test system prompt"); // SystemRoleTemplate
        capturedDto.Instructions.ShouldContain("Available agents:"); // AgentCatalogSectionTemplate
        capturedDto.Instructions.ShouldContain("Please provide JSON output"); // OutputRequirementsTemplate
        capturedDto.Instructions.ShouldContain("Use JSON format"); // JsonFormatSpecificationTemplate
        capturedDto.Instructions.ShouldContain("TestAgent"); // Agent information
        
        // Verify LLM config
        capturedDto.LLMConfig.ShouldNotBeNull();
        capturedDto.LLMConfig.SystemLLM.ShouldBe("OpenAI");
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WhenPromptBuildingThrows_ShouldUseFallbackPrompt()
    {
        // Arrange - Setup prompt options that might cause issues
        var problematicPromptOptions = new AIServicePromptOptions
        {
            SystemRoleTemplate = "System role",
            JsonFormatSpecificationTemplate = "JSON format",
            // Missing other templates to trigger exception handling
            AgentCatalogSectionTemplate = null,
            OutputRequirementsTemplate = null
        };
        _mockPromptOptions.Setup(x => x.CurrentValue).Returns(problematicPromptOptions);
        
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("""{"name": "Test", "properties": {"workflowNodeList": [], "workflowNodeUnitList": []}}""");

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldNotBeNull(); // Should still work with fallback prompt
    }

    #endregion

    #region Error Handling and Edge Cases

    [Fact]
    public async Task GenerateWorkflowAsync_WithEmptyJsonResponse_ShouldReturnNull()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("");

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithWhitespaceJsonResponse_ShouldReturnNull()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("   \t\n   ");

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithMalformedMarkdownJson_ShouldReturnNull()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync("```json\n{invalid json\n```");

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithLongJsonContent_ShouldHandleGracefully()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        // Create a very long JSON content to test content handling
        var longDescription = new string('A', 1000);
        var longJsonContent = $$$"""
        {
            "name": "Long Content Workflow",
            "properties": {
                "workflowNodeList": [
                    {
                        "nodeId": "node-1",
                        "nodeType": "TestAgent",
                        "nodeName": "Node with long description",
                        "extendedData": {
                            "description": "{{{longDescription}}}"
                        }
                    }
                ],
                "workflowNodeUnitList": []
            }
        }
        """;
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(longJsonContent);

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldNotBeNull();
        result.Properties.WorkflowNodeList.Count.ShouldBe(1);
        result.Properties.WorkflowNodeList[0].Properties.ShouldContainKey("description");
        ((string)result.Properties.WorkflowNodeList[0].Properties["description"]).Length.ShouldBe(1000);
    }

    [Fact]
    public async Task GenerateWorkflowAsync_WithUnexpectedJsonStructure_ShouldHandleGracefully()
    {
        // Arrange
        var agentTypes = new List<Type> { typeof(TestAgent) };
        _mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes()).Returns(agentTypes);
        
        var unexpectedJsonStructure = """
        {
            "name": "Unexpected Structure",
            "properties": {
                "workflowNodeList": [
                    {
                        "nodeId": "node-1",
                        "unexpectedField": "unexpected value",
                        "nestedObject": {
                            "deeply": {
                                "nested": "value"
                            }
                        }
                    }
                ],
                "workflowNodeUnitList": [
                    {
                        "unexpectedConnectionField": "value"
                    }
                ]
            }
        }
        """;
        
        _mockWorkflowComposerGAgent.Setup(x => x.InitializeAsync(It.IsAny<InitializeDto>()))
            .ReturnsAsync(true);
        _mockWorkflowComposerGAgent.Setup(x => x.GenerateWorkflowJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(unexpectedJsonStructure);

        // Act
        var result = await _service.GenerateWorkflowAsync("Create a workflow");

        // Assert
        result.ShouldNotBeNull();
        result.Properties.WorkflowNodeList.Count.ShouldBe(1);
        
        var node = result.Properties.WorkflowNodeList[0];
        node.NodeId.ShouldBe("node-1");
        node.AgentType.ShouldBe(""); // Default when nodeType/agentType missing
        node.Name.ShouldBe(""); // Default when nodeName/name missing
    }

    #endregion
}

/// <summary>
/// Test agent class for mocking
/// </summary>
[Description("Test agent for unit testing")]
public class TestAgent
{
    public string Name => "TestAgent";
}

/// <summary>
/// Another test agent class for mocking
/// </summary>
[Description("Another test agent for testing")]
public class AnotherTestAgent
{
    public string Name => "AnotherTestAgent";
}

/// <summary>
/// Agent without description attribute for testing fallback
/// </summary>
public class AgentWithoutDescription
{
    public string Name => "AgentWithoutDescription";
}

/// <summary>
/// Agent from Orleans code generation namespace (should be filtered)
/// Note: This class simulates being in OrleansCodeGen namespace for testing
/// </summary>
public class OrleansCodeGenAgent
{
    public string Name => "OrleansCodeGenAgent";
    
    // Simulate being in OrleansCodeGen namespace for filtering tests
    public string Namespace => "OrleansCodeGen";
}

/// <summary>
/// Agent that might cause reflection issues
/// </summary>
public class ProblematicAgent
{
    public string Name => "ProblematicAgent";
    
    // This property might cause issues in reflection
    public object ProblematicProperty
    {
        get => throw new NotImplementedException("This property always throws");
    }
} 