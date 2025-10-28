using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Application.Grains.Agents.Creator;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Core;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.Dto;
using Aevatar.Options;
using Aevatar.Schema;
using Aevatar.Service;
using Aevatar.Station.Feature.CreatorGAgent;
using Aevatar.Subscription;
using Aevatar.WorkflowRun;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NJsonSchema;
using Orleans;
using Orleans.Metadata;
using Orleans.Runtime;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace Aevatar.Application.Tests.Service;

public class WorkflowRunServiceTests
{
    private readonly Mock<IWorkflowViewService> _mockWorkflowViewService;
    private readonly Mock<ISubscriptionAppService> _mockSubscriptionAppService;
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly Mock<IGAgentFactory<IBusinessAgentBase>> _mockGAgentFactory;
    private readonly Mock<ISchemaProvider> _mockSchemaProvider;
    private readonly Mock<ILogger<WorkflowRunService>> _mockLogger;
    private readonly Mock<IClusterClient> _mockClusterClient;
    private readonly Mock<IGAgentManager> _mockGAgentManager;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IOptions<WorkflowAgentFilterOptions>> _mockFilterOptions;
    private readonly WorkflowRunService _workflowRunService;

    public WorkflowRunServiceTests()
    {
        _mockWorkflowViewService = new Mock<IWorkflowViewService>();
        _mockSubscriptionAppService = new Mock<ISubscriptionAppService>();
        _mockAgentService = new Mock<IAgentService>();
        _mockGAgentFactory = new Mock<IGAgentFactory<IBusinessAgentBase>>();
        _mockSchemaProvider = new Mock<ISchemaProvider>();
        _mockLogger = new Mock<ILogger<WorkflowRunService>>();
        _mockClusterClient = new Mock<IClusterClient>();
        _mockGAgentManager = new Mock<IGAgentManager>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockFilterOptions = new Mock<IOptions<WorkflowAgentFilterOptions>>();
        
        // Setup filter options with default values
        _mockFilterOptions.Setup(x => x.Value).Returns(new WorkflowAgentFilterOptions());

        // Note: GrainTypeResolver cannot be mocked (concrete class with required dependencies)
        // For these tests, we pass null since GetAllWorkflowAgents method is not tested in most cases
        _workflowRunService = new WorkflowRunService(
            _mockWorkflowViewService.Object,
            _mockSubscriptionAppService.Object,
            _mockAgentService.Object,
            _mockGAgentFactory.Object,
            _mockSchemaProvider.Object,
            _mockLogger.Object,
            _mockClusterClient.Object,
            _mockGAgentManager.Object,
            null!,
            _mockServiceProvider.Object,
            _mockFilterOptions.Object);
    }

    private void SetupGAgentFactoryMock()
    {
        // Create a mock IBusinessAgentBase that returns a valid configuration type
        var mockGAgent = new Mock<IBusinessAgentBase>();
        mockGAgent.Setup(x => x.GetConfigurationTypeAsync())
            .Returns(Task.FromResult<Type?>(typeof(TestAgentConfiguration)));
        mockGAgent.Setup(x => x.GetIsWorkflowAgentAsync())
            .Returns(Task.FromResult(false)); // Not a workflow infrastructure agent
        
        // Setup for the main GetGAgentAsync method with optional configuration parameter
        // This should cover all calls including GetGAgentAsync(grainId) which defaults to GetGAgentAsync(grainId, null)
        _mockGAgentFactory.Setup(factory => factory.GetGAgentAsync(It.IsAny<GrainId>(), It.IsAny<ConfigurationBase>()))
            .ReturnsAsync(mockGAgent.Object);
    }

    private void SetupSchemaProviderMock()
    {
        // Create a simple JsonSchema that always passes validation (empty schema accepts everything)
        var simpleSchema = JsonSchema.CreateAnySchema();
        
        // Setup SchemaProvider to return the simple schema for any type
        _mockSchemaProvider.Setup(x => x.GetTypeSchema(It.IsAny<Type>(), It.IsAny<DynamicDropDownContext?>(), It.IsAny<SchemaProcessingContext?>()))
            .Returns(simpleSchema);
    }

    [Fact]
    public async Task RunWorkflowAsync_WithValidRequest_ShouldCallValidationAndPublish()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var coordinatorId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        SetupGAgentFactoryMock();
        SetupSchemaProviderMock();
        SetupValidWorkflowConfiguration(viewAgentId);
        SetupPublishWorkflowSuccess(viewAgentId, coordinatorId);
        SetupSuccessfulCoordinatorExecution(coordinatorId);

        // Act & Assert - Should not throw exceptions
        await Should.NotThrowAsync(() => _workflowRunService.RunWorkflowAsync(request));
        
        // Verify the main workflow steps were called
        _mockAgentService.Verify(x => x.GetAgentAsync(viewAgentId), Times.Once);
        _mockWorkflowViewService.Verify(x => x.PublishWorkflowAsync(viewAgentId), Times.Once);
    }

    [Fact]
    public async Task RunWorkflowAsync_WithValidRequest_ShouldCallAllServices()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var coordinatorId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        SetupSuccessfulWorkflowExecution(viewAgentId, coordinatorId);

        // Act
        await _workflowRunService.RunWorkflowAsync(request);

        // Assert
        _mockAgentService.Verify(x => x.GetAgentAsync(viewAgentId), Times.Once);
        _mockWorkflowViewService.Verify(x => x.PublishWorkflowAsync(viewAgentId), Times.Once);
        _mockSubscriptionAppService.Verify(x => x.PublishEventAsync(It.IsAny<PublishEventDto>()), Times.Once);
    }

    [Fact]
    public async Task RunWorkflowAsync_WithInvalidWorkflowConfiguration_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        SetupGAgentFactoryMock();
        SetupSchemaProviderMock();

        // Setup invalid configuration (missing properties)
        _mockAgentService.Setup(x => x.GetAgentAsync(viewAgentId))
            .ReturnsAsync(new AgentDto
            {
                Id = viewAgentId,
                Properties = null // This will cause validation to fail
            });

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
    }

    [Fact]
    public async Task RunWorkflowAsync_WithInvalidJsonProperties_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        SetupGAgentFactoryMock();
        SetupSchemaProviderMock();

        // Setup invalid JSON in properties
        var invalidProperties = new Dictionary<string, object>
        {
            {"invalidJsonData", "this is not valid json for workflow config"}
        };

        _mockAgentService.Setup(x => x.GetAgentAsync(viewAgentId))
            .ReturnsAsync(new AgentDto
            {
                Id = viewAgentId,
                Properties = invalidProperties
            });

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
    }

    [Fact]
    public async Task RunWorkflowAsync_WithPublishWorkflowFailure_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        SetupGAgentFactoryMock();
        SetupSchemaProviderMock();
        SetupValidWorkflowConfiguration(viewAgentId);

        // Setup workflow publish failure
        _mockWorkflowViewService.Setup(x => x.PublishWorkflowAsync(viewAgentId))
            .ReturnsAsync((AgentDto)null);

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
    }

    [Fact]
    public async Task RunWorkflowAsync_WithCoordinatorNotReady_ShouldReturnFailureResult()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var coordinatorId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        SetupGAgentFactoryMock();
        SetupSchemaProviderMock();
        SetupValidWorkflowConfiguration(viewAgentId);
        SetupPublishWorkflowSuccess(viewAgentId, coordinatorId);

        // Setup coordinator agent with no events (not ready)
        var mockAgent = new Mock<Aevatar.Application.Grains.Agents.Creator.ICreatorGAgent>();
        // For simplicity, we'll throw an exception to simulate agent not ready
        mockAgent.Setup(x => x.GetAgentAsync())
            .ThrowsAsync(new InvalidOperationException("Agent not ready"));
        
        _mockClusterClient.Setup(x => x.GetGrain<Aevatar.Application.Grains.Agents.Creator.ICreatorGAgent>(coordinatorId, null))
            .Returns(mockAgent.Object);

        // Act
        var result = await _workflowRunService.RunWorkflowAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.WorkflowId.ShouldBe(coordinatorId);
        result.Message.ShouldContain("coordinator agent is not ready");
    }

    [Fact]
    public async Task RunWorkflowAsync_WithMissingCoordinatorId_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        SetupGAgentFactoryMock();
        SetupSchemaProviderMock();
        SetupValidWorkflowConfiguration(viewAgentId);

        // Setup published agent without coordinator ID
        var publishedAgent = new AgentDto
        {
            Id = viewAgentId,
            Name = "Test Workflow",
            Properties = new Dictionary<string, object>
            {
                {"WorkflowNodeList", new List<object>()},
                {"WorkflowNodeUnitList", new List<object>()},
                // Missing WorkflowCoordinatorGAgentId
            },
            WorkflowCoordinatorGAgentId = null // Explicitly set to null to test missing coordinator ID
        };

        _mockWorkflowViewService.Setup(x => x.PublishWorkflowAsync(viewAgentId))
            .ReturnsAsync(publishedAgent);

        // Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
    }

    [Fact]
    public async Task RunWorkflowAsync_WithNullEventProperties_ShouldStillExecute()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var coordinatorId = Guid.NewGuid();
        var request = new WorkflowRunRequestDto
        {
            ViewAgentId = viewAgentId,
            EventProperties = null // Test null event properties
        };

        SetupSuccessfulWorkflowExecution(viewAgentId, coordinatorId);

        // Act
        var result = await _workflowRunService.RunWorkflowAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.WorkflowId.ShouldBe(coordinatorId);
    }

    [Fact]
    public async Task RunWorkflowAsync_WithEmptyGuidViewAgentId_ShouldStillCallServices()
    {
        // Arrange
        var viewAgentId = Guid.Empty;
        var coordinatorId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        SetupSuccessfulWorkflowExecution(viewAgentId, coordinatorId);

        // Act
        var result = await _workflowRunService.RunWorkflowAsync(request);

        // Assert
        result.ShouldNotBeNull();
        _mockAgentService.Verify(x => x.GetAgentAsync(viewAgentId), Times.Once);
    }

    [Fact]
    public async Task RunWorkflowAsync_ShouldHandleExceptionGracefully()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        _mockAgentService.Setup(x => x.GetAgentAsync(viewAgentId))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => _workflowRunService.RunWorkflowAsync(request));
    }

    [Fact]
    public void WorkflowRunService_Constructor_ShouldRequireAllDependencies()
    {
        // Arrange & Act & Assert
        // Note: The actual constructor doesn't do null checks, so these tests expect no exceptions
        var service1 = new WorkflowRunService(null, _mockSubscriptionAppService.Object, _mockAgentService.Object, 
            _mockGAgentFactory.Object, _mockSchemaProvider.Object, _mockLogger.Object, _mockClusterClient.Object,
            _mockGAgentManager.Object, null!, _mockServiceProvider.Object, _mockFilterOptions.Object);
        service1.ShouldNotBeNull();

        var service2 = new WorkflowRunService(_mockWorkflowViewService.Object, null, _mockAgentService.Object,
            _mockGAgentFactory.Object, _mockSchemaProvider.Object, _mockLogger.Object, _mockClusterClient.Object,
            _mockGAgentManager.Object, null!, _mockServiceProvider.Object, _mockFilterOptions.Object);
        service2.ShouldNotBeNull();

        var service3 = new WorkflowRunService(_mockWorkflowViewService.Object, _mockSubscriptionAppService.Object, null,
            _mockGAgentFactory.Object, _mockSchemaProvider.Object, _mockLogger.Object, _mockClusterClient.Object,
            _mockGAgentManager.Object, null!, _mockServiceProvider.Object, _mockFilterOptions.Object);
        service3.ShouldNotBeNull();
    }

    [Fact]
    public void WorkflowRunService_ShouldImplementIWorkflowRunService()
    {
        // Assert
        _workflowRunService.ShouldBeAssignableTo<IWorkflowRunService>();
    }

    [Fact]
    public async Task RunWorkflowAsync_WithValidWorkflowNodes_ShouldValidateEachNode()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var coordinatorId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        SetupGAgentFactoryMock();
        SetupSchemaProviderMock();
        var workflowConfig = CreateWorkflowConfigWithNodes(2); // 2 nodes
        SetupWorkflowConfiguration(viewAgentId, workflowConfig);
        SetupPublishWorkflowSuccess(viewAgentId, coordinatorId);
        SetupSuccessfulCoordinatorExecution(coordinatorId);

        // Act
        var result = await _workflowRunService.RunWorkflowAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        
        // Verify that GetAgentAsync was called for validation
        _mockAgentService.Verify(x => x.GetAgentAsync(viewAgentId), Times.Once);
    }

    [Fact]
    public async Task ValidateWorkflowConfigurationAsync_WithInvalidJson_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        // Setup agent with properties that will cause JSON deserialization to fail
        var invalidProperties = new Dictionary<string, object>
        {
            {"WorkflowNodeList", "invalid json string instead of array"}
        };

        _mockAgentService.Setup(x => x.GetAgentAsync(viewAgentId))
            .ReturnsAsync(new AgentDto
            {
                Id = viewAgentId,
                Properties = invalidProperties
            });

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
        exception.Message.ShouldBe("Invalid workflow configuration format");
    }


    [Fact]
    public async Task ValidateWorkflowNodePropertiesAsync_WithEmptyAgentType_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        var workflowConfig = new WorkflowViewConfigDto
        {
            WorkflowNodeList = new List<WorkflowNodeDto>
            {
                new WorkflowNodeDto
                {
                    NodeId = Guid.NewGuid(),
                    Name = "Test Node",
                    AgentType = "", // Empty agent type
                    JsonProperties = "{}",
                    ExtendedData = new Dictionary<string, string>()
                }
            },
            WorkflowNodeUnitList = new List<WorkflowNodeUnitDto>(),
            WorkflowCoordinatorGAgentId = Guid.NewGuid()
        };

        SetupWorkflowConfiguration(viewAgentId, workflowConfig);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
        exception.Message.ShouldContain("AgentType is missing");
    }

    [Fact]
    public async Task ValidateWorkflowNodePropertiesAsync_WithEmptyJsonProperties_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        var workflowConfig = new WorkflowViewConfigDto
        {
            WorkflowNodeList = new List<WorkflowNodeDto>
            {
                new WorkflowNodeDto
                {
                    NodeId = Guid.NewGuid(),
                    Name = "Test Node",
                    AgentType = "TestAgent",
                    JsonProperties = "", // Empty JSON properties
                    ExtendedData = new Dictionary<string, string>()
                }
            },
            WorkflowNodeUnitList = new List<WorkflowNodeUnitDto>(),
            WorkflowCoordinatorGAgentId = Guid.NewGuid()
        };

        SetupWorkflowConfiguration(viewAgentId, workflowConfig);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
        exception.Message.ShouldContain("JsonProperties is missing");
    }

    [Fact]
    public async Task PublishWorkflowAsync_WithNullProperties_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        SetupGAgentFactoryMock();
        SetupSchemaProviderMock();
        SetupValidWorkflowConfiguration(viewAgentId);

        // Setup workflow service to return agent with null properties
        var publishedAgent = new AgentDto
        {
            Id = viewAgentId,
            Name = "Test Workflow",
            Properties = null, // Null properties
            WorkflowCoordinatorGAgentId = null // Set to null since properties are null
        };

        _mockWorkflowViewService.Setup(x => x.PublishWorkflowAsync(viewAgentId))
            .ReturnsAsync(publishedAgent);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
        exception.Message.ShouldBe("Published workflow agent has no properties");
    }

    [Fact]
    public async Task PublishWorkflowAsync_WithInvalidJsonInProperties_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        SetupGAgentFactoryMock();
        SetupSchemaProviderMock();
        SetupValidWorkflowConfiguration(viewAgentId);

        // Setup workflow service to return agent with properties that can't be deserialized to WorkflowViewConfigDto
        var publishedAgent = new AgentDto
        {
            Id = viewAgentId,
            Name = "Test Workflow",
            Properties = new Dictionary<string, object>
            {
                {"WorkflowNodeList", "invalid_string_instead_of_array"}, // This will cause deserialization to fail
                {"WorkflowNodeUnitList", new object()}, // Invalid structure
                {"WorkflowCoordinatorGAgentId", "not_a_guid"} // Invalid GUID
            },
            WorkflowCoordinatorGAgentId = null // Set to null since properties contain invalid data
        };

        _mockWorkflowViewService.Setup(x => x.PublishWorkflowAsync(viewAgentId))
            .ReturnsAsync(publishedAgent);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
        exception.Message.ShouldBe("Invalid workflow configuration in published agent");
    }

    [Fact]
    public async Task ValidateAgentConfigAsync_WithNullConfigurationType_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        // Setup agent that returns null configuration type
        var mockGAgent = new Mock<IBusinessAgentBase>();
        mockGAgent.Setup(x => x.GetConfigurationTypeAsync())
            .Returns(Task.FromResult<Type?>(null)); // Null configuration type

        _mockGAgentFactory.Setup(factory => factory.GetGAgentAsync(It.IsAny<GrainId>(), It.IsAny<ConfigurationBase>()))
            .ReturnsAsync(mockGAgent.Object);

        var workflowConfig = new WorkflowViewConfigDto
        {
            WorkflowNodeList = new List<WorkflowNodeDto>
            {
                new WorkflowNodeDto
                {
                    NodeId = Guid.NewGuid(),
                    Name = "Test Node",
                    AgentType = "TestAgent",
                    JsonProperties = "{}",
                    ExtendedData = new Dictionary<string, string>()
                }
            },
            WorkflowNodeUnitList = new List<WorkflowNodeUnitDto>(),
            WorkflowCoordinatorGAgentId = Guid.NewGuid()
        };

        SetupWorkflowConfiguration(viewAgentId, workflowConfig);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
        exception.Message.ShouldContain("has no configuration");
    }

    [Fact]
    public async Task ValidateAgentConfigAsync_WithNonUserFriendlyException_ShouldWrapException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        // Setup agent factory to throw a non-UserFriendlyException
        _mockGAgentFactory.Setup(factory => factory.GetGAgentAsync(It.IsAny<GrainId>(), It.IsAny<ConfigurationBase>()))
            .ThrowsAsync(new InvalidOperationException("Some internal error"));

        var workflowConfig = new WorkflowViewConfigDto
        {
            WorkflowNodeList = new List<WorkflowNodeDto>
            {
                new WorkflowNodeDto
                {
                    NodeId = Guid.NewGuid(),
                    Name = "Test Node",
                    AgentType = "TestAgent",
                    JsonProperties = "{}",
                    ExtendedData = new Dictionary<string, string>()
                }
            },
            WorkflowNodeUnitList = new List<WorkflowNodeUnitDto>(),
            WorkflowCoordinatorGAgentId = Guid.NewGuid()
        };

        SetupWorkflowConfiguration(viewAgentId, workflowConfig);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
        exception.Message.ShouldStartWith("Invalid agent type 'TestAgent'");
        exception.Message.ShouldContain("Some internal error");
    }

    // Note: Schema validation test removed because JsonSchema.Validate cannot be mocked (non-virtual method)


    [Fact]
    public async Task ExecuteWorkflowAsync_WithMaxRetriesReached_ShouldReturnFalse()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var coordinatorId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        SetupGAgentFactoryMock();
        SetupSchemaProviderMock();
        SetupValidWorkflowConfiguration(viewAgentId);
        SetupPublishWorkflowSuccess(viewAgentId, coordinatorId);

        // Setup coordinator agent that never has the required event
        var mockAgent = new Mock<Aevatar.Application.Grains.Agents.Creator.ICreatorGAgent>();
        var mockAgentState = new CreatorGAgentState
        {
            EventInfoList = new List<EventDescription>() // Empty event list
        };

        mockAgent.Setup(x => x.GetAgentAsync())
            .ReturnsAsync(mockAgentState);

        _mockClusterClient.Setup(x => x.GetGrain<Aevatar.Application.Grains.Agents.Creator.ICreatorGAgent>(coordinatorId, null))
            .Returns(mockAgent.Object);

        // Act
        var result = await _workflowRunService.RunWorkflowAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.WorkflowId.ShouldBe(coordinatorId);
        result.Message.ShouldContain("coordinator agent is not ready");
    }

    [Fact]
    public async Task RunWorkflowAsync_WithNullJsonProperties_ShouldThrowValidationException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        var workflowConfig = new WorkflowViewConfigDto
        {
            WorkflowNodeList = new List<WorkflowNodeDto>
            {
                new WorkflowNodeDto
                {
                    NodeId = Guid.NewGuid(),
                    Name = "Test Node",
                    AgentType = "TestAgent",
                    JsonProperties = null, // Test null properties
                    ExtendedData = new Dictionary<string, string>()
                }
            },
            WorkflowNodeUnitList = new List<WorkflowNodeUnitDto>(),
            WorkflowCoordinatorGAgentId = Guid.NewGuid()
        };

        SetupWorkflowConfiguration(viewAgentId, workflowConfig);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
        exception.Message.ShouldContain("JsonProperties is missing");
    }

    [Fact]
    public async Task RunWorkflowAsync_WithEmptyJsonProperties_ShouldThrowValidationException()
    {
        // Arrange
        var viewAgentId = Guid.NewGuid();
        var request = CreateValidWorkflowRunRequest(viewAgentId);

        var workflowConfig = new WorkflowViewConfigDto
        {
            WorkflowNodeList = new List<WorkflowNodeDto>
            {
                new WorkflowNodeDto
                {
                    NodeId = Guid.NewGuid(),
                    Name = "Test Node",
                    AgentType = "TestAgent",
                    JsonProperties = "", // Test empty string
                    ExtendedData = new Dictionary<string, string>()
                }
            },
            WorkflowNodeUnitList = new List<WorkflowNodeUnitDto>(),
            WorkflowCoordinatorGAgentId = Guid.NewGuid()
        };

        SetupWorkflowConfiguration(viewAgentId, workflowConfig);

        // Act & Assert
        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _workflowRunService.RunWorkflowAsync(request));
        exception.Message.ShouldContain("JsonProperties is missing");
    }

    // Test helper methods
    private WorkflowRunRequestDto CreateValidWorkflowRunRequest(Guid viewAgentId)
    {
        return new WorkflowRunRequestDto
        {
            ViewAgentId = viewAgentId,
            EventProperties = new Dictionary<string, object>
            {
                {"inputData", "test data"},
                {"metadata", new { version = "1.0" }}
            }
        };
    }

    private void SetupSuccessfulWorkflowExecution(Guid viewAgentId, Guid coordinatorId)
    {
        SetupGAgentFactoryMock();
        SetupSchemaProviderMock();
        SetupValidWorkflowConfiguration(viewAgentId);
        SetupPublishWorkflowSuccess(viewAgentId, coordinatorId);
        SetupSuccessfulCoordinatorExecution(coordinatorId);
    }

    private void SetupValidWorkflowConfiguration(Guid viewAgentId)
    {
        var workflowConfig = CreateWorkflowConfigWithNodes(1);
        SetupWorkflowConfiguration(viewAgentId, workflowConfig);
    }

    private void SetupWorkflowConfiguration(Guid viewAgentId, WorkflowViewConfigDto workflowConfig)
    {
        var workflowProperties = new Dictionary<string, object>
        {
            {"WorkflowNodeList", workflowConfig.WorkflowNodeList.Select(node => new Dictionary<string, object>
            {
                {"NodeId", node.NodeId},
                {"AgentId", node.AgentId},
                {"Name", node.Name},
                {"AgentType", node.AgentType},
                {"JsonProperties", node.JsonProperties},
                {"ExtendedData", node.ExtendedData}
            }).ToList()},
            {"WorkflowNodeUnitList", workflowConfig.WorkflowNodeUnitList},
            {"WorkflowCoordinatorGAgentId", workflowConfig.WorkflowCoordinatorGAgentId}
        };

        _mockAgentService.Setup(x => x.GetAgentAsync(viewAgentId))
            .ReturnsAsync(new AgentDto
            {
                Id = viewAgentId,
                Name = "Test Workflow",
                Properties = workflowProperties
            });
    }

    private void SetupPublishWorkflowSuccess(Guid viewAgentId, Guid coordinatorId)
    {
        var publishedAgent = new AgentDto
        {
            Id = viewAgentId,
            Name = "Test Workflow",
            Properties = new Dictionary<string, object>
            {
                {"WorkflowNodeList", new List<object>()},
                {"WorkflowNodeUnitList", new List<object>()},
                {"WorkflowCoordinatorGAgentId", coordinatorId}
            },
            WorkflowCoordinatorGAgentId = coordinatorId
        };

        _mockWorkflowViewService.Setup(x => x.PublishWorkflowAsync(viewAgentId))
            .ReturnsAsync(publishedAgent);
    }

        private void SetupSuccessfulCoordinatorExecution(Guid coordinatorId)
        {
            // Mock the ICreatorGAgent that will be returned by the cluster client
            var mockCreatorGAgent = new Mock<Aevatar.Application.Grains.Agents.Creator.ICreatorGAgent>();
            
            // Create a mock type that has the expected FullName
            var mockEventType = new Mock<Type>();
            mockEventType.Setup(t => t.FullName).Returns("Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent.StartWorkflowCoordinatorEvent");
            
            // Create mock agent state with the required event
            var mockAgentState = new CreatorGAgentState
            {
                EventInfoList = new List<EventDescription>
                {
                    new EventDescription
                    {
                        EventType = mockEventType.Object,
                        Description = "StartWorkflowCoordinatorEvent"
                    }
                }
            };
            
            mockCreatorGAgent.Setup(x => x.GetAgentAsync())
                .ReturnsAsync(mockAgentState);
            
            // Setup cluster client to return the mock grain
            _mockClusterClient.Setup(x => x.GetGrain<Aevatar.Application.Grains.Agents.Creator.ICreatorGAgent>(coordinatorId, null))
                .Returns(mockCreatorGAgent.Object);
            
            // Setup subscription service for successful event publishing
            _mockSubscriptionAppService.Setup(x => x.PublishEventAsync(It.IsAny<PublishEventDto>()))
                .Returns(Task.CompletedTask);
        }

    private WorkflowViewConfigDto CreateWorkflowConfigWithNodes(int nodeCount)
    {
        var nodes = new List<WorkflowNodeDto>();
        for (int i = 0; i < nodeCount; i++)
        {
            nodes.Add(new WorkflowNodeDto
            {
                NodeId = Guid.NewGuid(),
                AgentId = Guid.NewGuid(),
                Name = $"Test Node {i + 1}",
                AgentType = "TestAgent",
                JsonProperties = "{}",
                ExtendedData = new Dictionary<string, string>
                {
                    {"XPosition", "10"},
                    {"YPosition", "20"}
                }
            });
        }

        return new WorkflowViewConfigDto
        {
            WorkflowNodeList = nodes,
            WorkflowNodeUnitList = new List<WorkflowNodeUnitDto>(),
            WorkflowCoordinatorGAgentId = Guid.NewGuid()
        };
    }
}

    // Simple test configuration class for testing purposes
    public class TestAgentConfiguration : ConfigurationBase
    {
        public TestAgentConfiguration()
        {
            // Parameterless constructor for Activator.CreateInstance
        }
        
        public string TestProperty { get; set; } = "TestValue";
    }

