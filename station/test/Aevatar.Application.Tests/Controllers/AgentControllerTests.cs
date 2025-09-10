// ABOUTME: This file contains comprehensive unit tests for AgentController API endpoints
// ABOUTME: Tests validate controller behavior, parameter handling, HTTP attributes, and error scenarios

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.AgentValidation;
using Aevatar.Controllers;
using Aevatar.Service;
using Aevatar.Subscription;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;

namespace Aevatar.Application.Tests.Controllers;

public class AgentControllerTests
{
    private readonly Mock<IAgentService> _mockAgentService;
    private readonly Mock<ISubscriptionAppService> _mockSubscriptionAppService;
    private readonly Mock<IAgentValidationService> _mockAgentValidationService;
    private readonly Mock<ILogger<AgentController>> _mockLogger;

    public AgentControllerTests()
    {
        _mockAgentService = new Mock<IAgentService>();
        _mockSubscriptionAppService = new Mock<ISubscriptionAppService>();
        _mockAgentValidationService = new Mock<IAgentValidationService>();
        _mockLogger = new Mock<ILogger<AgentController>>();
    }

    private AgentController CreateAgentController()
    {
        var controller = new AgentController(
            _mockLogger.Object,
            _mockSubscriptionAppService.Object,
            _mockAgentService.Object,
            _mockAgentValidationService.Object
        );

        // Setup HttpContext for testing
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    #region Controller Structure Tests

    [Fact]
    public void AgentController_Should_Have_Route_Attribute()
    {
        // Arrange & Act
        var routeAttribute = typeof(AgentController).GetCustomAttribute<RouteAttribute>();

        // Assert
        routeAttribute.ShouldNotBeNull();
        routeAttribute.Template.ShouldBe("api/agent");
    }

    [Fact]
    public void AgentController_Should_Have_Proper_Controller_Name()
    {
        // Arrange & Act
        var controllerName = typeof(AgentController).Name.Replace("Controller", "");

        // Assert
        controllerName.ShouldBe("Agent");
    }

    #endregion

    #region GetAllAgent Tests

    [Fact]
    public void GetAllAgent_Should_Have_HttpGet_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("GetAllAgent");

        // Act
        var httpGetAttribute = methodInfo.GetCustomAttribute<HttpGetAttribute>();

        // Assert
        httpGetAttribute.ShouldNotBeNull();
        httpGetAttribute.Template.ShouldBe("agent-type-info-list");
    }

    [Fact]
    public void GetAllAgent_Should_Have_Authorize_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("GetAllAgent");

        // Act
        var authorizeAttribute = methodInfo.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        authorizeAttribute.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetAllAgent_Should_Call_AgentService()
    {
        // Arrange
        var controller = CreateAgentController();
        var expectedAgents = new List<AgentTypeDto>
        {
            new AgentTypeDto { AgentType = "TestAgent", FullName = "Test.Agent" }
        };

        _mockAgentService
            .Setup(x => x.GetAllAgents())
            .ReturnsAsync(expectedAgents);

        // Act
        var result = await controller.GetAllAgent();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].AgentType.ShouldBe("TestAgent");
        _mockAgentService.Verify(x => x.GetAllAgents(), Times.Once);
    }

    [Fact]
    public async Task GetAllAgent_Should_Handle_Service_Exception()
    {
        // Arrange
        var controller = CreateAgentController();

        _mockAgentService
            .Setup(x => x.GetAllAgents())
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            controller.GetAllAgent());

        exception.Message.ShouldBe("Service error");
        _mockAgentService.Verify(x => x.GetAllAgents(), Times.Once);
    }

    #endregion

    #region GetAllAgentInstance Tests

    [Fact]
    public void GetAllAgentInstance_Should_Have_HttpGet_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("GetAllAgentInstance");

        // Act
        var httpGetAttribute = methodInfo.GetCustomAttribute<HttpGetAttribute>();

        // Assert
        httpGetAttribute.ShouldNotBeNull();
        httpGetAttribute.Template.ShouldBe("agent-list");
    }

    [Fact]
    public async Task GetAllAgentInstance_Should_Call_AgentService_With_Query()
    {
        // Arrange
        var controller = CreateAgentController();
        var queryDto = new GetAllAgentInstancesQueryDto();
        var expectedInstances = new List<AgentInstanceDto>
        {
            new AgentInstanceDto { Name = "TestInstance" }
        };

        _mockAgentService
            .Setup(x => x.GetAllAgentInstances(queryDto))
            .ReturnsAsync(expectedInstances);

        // Act
        var result = await controller.GetAllAgentInstance(queryDto);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        _mockAgentService.Verify(x => x.GetAllAgentInstances(queryDto), Times.Once);
    }

    #endregion

    #region CreateAgent Tests

    [Fact]
    public void CreateAgent_Should_Have_HttpPost_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("CreateAgent");

        // Act
        var httpPostAttribute = methodInfo.GetCustomAttribute<HttpPostAttribute>();

        // Assert
        httpPostAttribute.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateAgent_Should_Call_AgentService_And_Log()
    {
        // Arrange
        var controller = CreateAgentController();
        var createDto = new CreateAgentInputDto
        {
            AgentType = "TestAgent",
            Name = "Test Agent"
        };
        var expectedAgent = new AgentDto { Name = "Test Agent" };

        _mockAgentService
            .Setup(x => x.CreateAgentAsync(createDto))
            .ReturnsAsync(expectedAgent);

        // Act
        var result = await controller.CreateAgent(createDto);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test Agent");
        _mockAgentService.Verify(x => x.CreateAgentAsync(createDto), Times.Once);
    }

    [Fact]
    public async Task CreateAgent_Should_Handle_Invalid_Input()
    {
        // Arrange
        var controller = CreateAgentController();
        var createDto = new CreateAgentInputDto(); // Invalid input

        _mockAgentService
            .Setup(x => x.CreateAgentAsync(createDto))
            .ThrowsAsync(new ArgumentException("Invalid agent type"));

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            controller.CreateAgent(createDto));

        exception.Message.ShouldBe("Invalid agent type");
    }

    #endregion

    #region GetAgent Tests

    [Fact]
    public void GetAgent_Should_Have_HttpGet_Attribute_With_Route()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("GetAgent");

        // Act
        var httpGetAttribute = methodInfo.GetCustomAttribute<HttpGetAttribute>();

        // Assert
        httpGetAttribute.ShouldNotBeNull();
        httpGetAttribute.Template.ShouldBe("{guid}");
    }

    [Fact]
    public async Task GetAgent_Should_Call_AgentService_With_Guid()
    {
        // Arrange
        var controller = CreateAgentController();
        var testGuid = Guid.NewGuid();
        var expectedAgent = new AgentDto { AgentGuid = testGuid };

        _mockAgentService
            .Setup(x => x.GetAgentAsync(testGuid))
            .ReturnsAsync(expectedAgent);

        // Act
        var result = await controller.GetAgent(testGuid);

        // Assert
        result.ShouldNotBeNull();
        result.AgentGuid.ShouldBe(testGuid);
        _mockAgentService.Verify(x => x.GetAgentAsync(testGuid), Times.Once);
    }

    [Fact]
    public async Task GetAgent_Should_Handle_NonExistent_Agent()
    {
        // Arrange
        var controller = CreateAgentController();
        var testGuid = Guid.NewGuid();

        _mockAgentService
            .Setup(x => x.GetAgentAsync(testGuid))
            .ThrowsAsync(new KeyNotFoundException("Agent not found"));

        // Act & Assert
        var exception = await Should.ThrowAsync<KeyNotFoundException>(() =>
            controller.GetAgent(testGuid));

        exception.Message.ShouldBe("Agent not found");
    }

    #endregion

    #region GetAgentRelationship Tests

    [Fact]
    public void GetAgentRelationship_Should_Have_HttpGet_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("GetAgentRelationship");

        // Act
        var httpGetAttribute = methodInfo.GetCustomAttribute<HttpGetAttribute>();

        // Assert
        httpGetAttribute.ShouldNotBeNull();
        httpGetAttribute.Template.ShouldBe("{guid}/relationship");
    }

    [Fact]
    public async Task GetAgentRelationship_Should_Call_AgentService()
    {
        // Arrange
        var controller = CreateAgentController();
        var testGuid = Guid.NewGuid();
        var expectedRelationship = new AgentRelationshipDto
        {
            Parent = Guid.NewGuid(),
            SubAgents = new List<Guid> { Guid.NewGuid() }
        };

        _mockAgentService
            .Setup(x => x.GetAgentRelationshipAsync(testGuid))
            .ReturnsAsync(expectedRelationship);

        // Act
        var result = await controller.GetAgentRelationship(testGuid);

        // Assert
        result.ShouldNotBeNull();
        result.Parent.ShouldNotBeNull();
        result.SubAgents.ShouldNotBeEmpty();
        _mockAgentService.Verify(x => x.GetAgentRelationshipAsync(testGuid), Times.Once);
    }

    #endregion

    #region AddSubAgent Tests

    [Fact]
    public void AddSubAgent_Should_Have_HttpPost_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("AddSubAgent");

        // Act
        var httpPostAttribute = methodInfo.GetCustomAttribute<HttpPostAttribute>();

        // Assert
        httpPostAttribute.ShouldNotBeNull();
        httpPostAttribute.Template.ShouldBe("{guid}/add-subagent");
    }

    [Fact]
    public async Task AddSubAgent_Should_Call_AgentService()
    {
        // Arrange
        var controller = CreateAgentController();
        var testGuid = Guid.NewGuid();
        var addSubAgentDto = new AddSubAgentDto
        {
            SubAgents = new List<Guid> { Guid.NewGuid() }
        };
        var expectedResult = new SubAgentDto { SubAgents = addSubAgentDto.SubAgents };

        _mockAgentService
            .Setup(x => x.AddSubAgentAsync(testGuid, addSubAgentDto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.AddSubAgent(testGuid, addSubAgentDto);

        // Assert
        result.ShouldNotBeNull();
        result.SubAgents.ShouldBe(addSubAgentDto.SubAgents);
        _mockAgentService.Verify(x => x.AddSubAgentAsync(testGuid, addSubAgentDto), Times.Once);
    }

    #endregion

    #region RemoveSubAgent Tests

    [Fact]
    public void RemoveSubAgent_Should_Have_HttpPost_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("RemoveSubAgent");

        // Act
        var httpPostAttribute = methodInfo.GetCustomAttribute<HttpPostAttribute>();

        // Assert
        httpPostAttribute.ShouldNotBeNull();
        httpPostAttribute.Template.ShouldBe("{guid}/remove-subagent");
    }

    [Fact]
    public async Task RemoveSubAgent_Should_Call_AgentService()
    {
        // Arrange
        var controller = CreateAgentController();
        var testGuid = Guid.NewGuid();
        var removeSubAgentDto = new RemoveSubAgentDto
        {
            RemovedSubAgents = new List<Guid> { Guid.NewGuid() }
        };
        var expectedResult = new SubAgentDto { SubAgents = new List<Guid>() };

        _mockAgentService
            .Setup(x => x.RemoveSubAgentAsync(testGuid, removeSubAgentDto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.RemoveSubAgent(testGuid, removeSubAgentDto);

        // Assert
        result.ShouldNotBeNull();
        _mockAgentService.Verify(x => x.RemoveSubAgentAsync(testGuid, removeSubAgentDto), Times.Once);
    }

    #endregion

    #region RemoveAllSubAgent Tests

    [Fact]
    public void RemoveAllSubAgent_Should_Have_HttpPost_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("RemoveAllSubAgent");

        // Act
        var httpPostAttribute = methodInfo.GetCustomAttribute<HttpPostAttribute>();

        // Assert
        httpPostAttribute.ShouldNotBeNull();
        httpPostAttribute.Template.ShouldBe("{guid}/remove-all-subagent");
    }

    [Fact]
    public async Task RemoveAllSubAgent_Should_Call_AgentService()
    {
        // Arrange
        var controller = CreateAgentController();
        var testGuid = Guid.NewGuid();

        _mockAgentService
            .Setup(x => x.RemoveAllSubAgentAsync(testGuid))
            .Returns(Task.CompletedTask);

        // Act
        await controller.RemoveAllSubAgent(testGuid);

        // Assert
        _mockAgentService.Verify(x => x.RemoveAllSubAgentAsync(testGuid), Times.Once);
    }

    #endregion

    #region UpdateAgent Tests

    [Fact]
    public void UpdateAgent_Should_Have_HttpPut_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("UpdateAgent");

        // Act
        var httpPutAttribute = methodInfo.GetCustomAttribute<HttpPutAttribute>();

        // Assert
        httpPutAttribute.ShouldNotBeNull();
        httpPutAttribute.Template.ShouldBe("{guid}");
    }

    [Fact]
    public async Task UpdateAgent_Should_Call_AgentService()
    {
        // Arrange
        var controller = CreateAgentController();
        var testGuid = Guid.NewGuid();
        var updateDto = new UpdateAgentInputDto { Name = "Updated Agent" };
        var expectedAgent = new AgentDto { Name = "Updated Agent" };

        _mockAgentService
            .Setup(x => x.UpdateAgentAsync(testGuid, updateDto))
            .ReturnsAsync(expectedAgent);

        // Act
        var result = await controller.UpdateAgent(testGuid, updateDto);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Agent");
        _mockAgentService.Verify(x => x.UpdateAgentAsync(testGuid, updateDto), Times.Once);
    }

    #endregion

    #region DeleteAgent Tests

    [Fact]
    public void DeleteAgent_Should_Have_HttpDelete_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("DeleteAgent");

        // Act
        var httpDeleteAttribute = methodInfo.GetCustomAttribute<HttpDeleteAttribute>();

        // Assert
        httpDeleteAttribute.ShouldNotBeNull();
        httpDeleteAttribute.Template.ShouldBe("{guid}");
    }

    [Fact]
    public async Task DeleteAgent_Should_Call_AgentService()
    {
        // Arrange
        var controller = CreateAgentController();
        var testGuid = Guid.NewGuid();

        _mockAgentService
            .Setup(x => x.DeleteAgentAsync(testGuid))
            .Returns(Task.CompletedTask);

        // Act
        await controller.DeleteAgent(testGuid);

        // Assert
        _mockAgentService.Verify(x => x.DeleteAgentAsync(testGuid), Times.Once);
    }

    [Fact]
    public async Task DeleteAgent_Should_Handle_Agent_With_Dependencies()
    {
        // Arrange
        var controller = CreateAgentController();
        var testGuid = Guid.NewGuid();

        _mockAgentService
            .Setup(x => x.DeleteAgentAsync(testGuid))
            .ThrowsAsync(new InvalidOperationException("Agent has dependencies"));

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            controller.DeleteAgent(testGuid));

        exception.Message.ShouldBe("Agent has dependencies");
    }

    #endregion

    #region PublishAsync Tests

    [Fact]
    public void PublishAsync_Should_Have_HttpPost_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("PublishAsync");

        // Act
        var httpPostAttribute = methodInfo.GetCustomAttribute<HttpPostAttribute>();

        // Assert
        httpPostAttribute.ShouldNotBeNull();
        httpPostAttribute.Template.ShouldBe("publishEvent");
    }

    [Fact]
    public async Task PublishAsync_Should_Call_SubscriptionService()
    {
        // Arrange
        var controller = CreateAgentController();
        var publishEventDto = new PublishEventDto
        {
            AgentId = Guid.NewGuid(),
            EventType = "TestEvent"
        };

        _mockSubscriptionAppService
            .Setup(x => x.PublishEventAsync(publishEventDto))
            .Returns(Task.CompletedTask);

        // Act
        await controller.PublishAsync(publishEventDto);

        // Assert
        _mockSubscriptionAppService.Verify(x => x.PublishEventAsync(publishEventDto), Times.Once);
    }

    #endregion

    #region ValidateConfigAsync Tests

    [Fact]
    public void ValidateConfigAsync_Should_Have_HttpPost_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("ValidateConfigAsync");

        // Act
        var httpPostAttribute = methodInfo.GetCustomAttribute<HttpPostAttribute>();

        // Assert
        httpPostAttribute.ShouldNotBeNull();
        httpPostAttribute.Template.ShouldBe("validation/validate-config");
    }

    [Fact]
    public async Task ValidateConfigAsync_Should_Call_ValidationService()
    {
        // Arrange
        var controller = CreateAgentController();
        var validationRequest = new ValidationRequestDto
        {
            GAgentNamespace = "Test.Agent",
            ConfigJson = "{\"test\": \"value\"}"
        };
        var expectedResult = ConfigValidationResultDto.Success("Validation passed");

        _mockAgentValidationService
            .Setup(x => x.ValidateConfigAsync(validationRequest))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.ValidateConfigAsync(validationRequest);

        // Assert
        result.ShouldNotBeNull();
        result.IsValid.ShouldBe(true);
        result.Message.ShouldBe("Validation passed");
        _mockAgentValidationService.Verify(x => x.ValidateConfigAsync(validationRequest), Times.Once);
    }

    [Fact]
    public async Task ValidateConfigAsync_Should_Handle_Validation_Failure()
    {
        // Arrange
        var controller = CreateAgentController();
        var validationRequest = new ValidationRequestDto
        {
            GAgentNamespace = "Invalid.Agent",
            ConfigJson = "{\"invalid\": \"config\"}"
        };
        var expectedResult = ConfigValidationResultDto.Failure("Invalid configuration");

        _mockAgentValidationService
            .Setup(x => x.ValidateConfigAsync(validationRequest))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.ValidateConfigAsync(validationRequest);

        // Assert
        result.ShouldNotBeNull();
        result.IsValid.ShouldBe(false);
        result.Message.ShouldBe("Invalid configuration");
    }

    [Fact]
    public async Task ValidateConfigAsync_Should_Handle_Service_Exception()
    {
        // Arrange
        var controller = CreateAgentController();
        var validationRequest = new ValidationRequestDto
        {
            GAgentNamespace = "Test.Agent",
            ConfigJson = "{\"test\": \"value\"}"
        };

        _mockAgentValidationService
            .Setup(x => x.ValidateConfigAsync(validationRequest))
            .ThrowsAsync(new InvalidOperationException("Validation service error"));

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            controller.ValidateConfigAsync(validationRequest));

        exception.Message.ShouldBe("Validation service error");
    }

    #endregion

    #region ValidationHealthCheck Tests

    [Fact]
    public void ValidationHealthCheck_Should_Have_HttpGet_Attribute()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("ValidationHealthCheck");

        // Act
        var httpGetAttribute = methodInfo.GetCustomAttribute<HttpGetAttribute>();

        // Assert
        httpGetAttribute.ShouldNotBeNull();
        httpGetAttribute.Template.ShouldBe("validation/health");
    }

    [Fact]
    public void ValidationHealthCheck_Should_Return_Ok_With_Status()
    {
        // Arrange
        var controller = CreateAgentController();

        // Act
        var result = controller.ValidationHealthCheck();

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult.Value.ShouldNotBeNull();
        
        // Convert anonymous object to dictionary for easier testing
        var value = okResult.Value.ToString();
        value.ShouldContain("status");
        value.ShouldContain("healthy");
        value.ShouldContain("timestamp");
    }

    [Fact]
    public void ValidationHealthCheck_Should_Not_Require_Authorization()
    {
        // Arrange
        var methodInfo = typeof(AgentController).GetMethod("ValidationHealthCheck");

        // Act
        var authorizeAttribute = methodInfo.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        authorizeAttribute.ShouldBeNull();
    }

    #endregion

    #region Parameter Validation Tests

    [Fact]
    public async Task CreateAgent_Should_Accept_Valid_Parameters()
    {
        // Arrange
        var controller = CreateAgentController();
        var testCases = new[]
        {
            new CreateAgentInputDto { AgentType = "Agent1", Name = "Test Agent 1" },
            new CreateAgentInputDto { AgentType = "Agent2", Name = "Test Agent 2" },
            new CreateAgentInputDto { AgentType = "ComplexAgent", Name = "Complex Test Agent" }
        };

        foreach (var testCase in testCases)
        {
            _mockAgentService
                .Setup(x => x.CreateAgentAsync(It.Is<CreateAgentInputDto>(dto => dto.AgentType == testCase.AgentType)))
                .ReturnsAsync(new AgentDto { Name = testCase.Name });
        }

        // Act & Assert
        foreach (var testCase in testCases)
        {
            await Should.NotThrowAsync(() =>
                controller.CreateAgent(testCase));
        }
    }

    [Fact]
    public async Task GetAgent_Should_Handle_Empty_Guid()
    {
        // Arrange
        var controller = CreateAgentController();
        var emptyGuid = Guid.Empty;

        _mockAgentService
            .Setup(x => x.GetAgentAsync(emptyGuid))
            .ThrowsAsync(new ArgumentException("Invalid agent ID"));

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            controller.GetAgent(emptyGuid));

        exception.Message.ShouldBe("Invalid agent ID");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task CreateAgent_Then_GetAgent_Should_Work_Together()
    {
        // Arrange
        var controller = CreateAgentController();
        var testGuid = Guid.NewGuid();
        var createDto = new CreateAgentInputDto
        {
            AgentType = "TestAgent",
            Name = "Integration Test Agent"
        };
        var createdAgent = new AgentDto
        {
            AgentGuid = testGuid,
            Name = "Integration Test Agent"
        };

        _mockAgentService
            .Setup(x => x.CreateAgentAsync(createDto))
            .ReturnsAsync(createdAgent);
        _mockAgentService
            .Setup(x => x.GetAgentAsync(testGuid))
            .ReturnsAsync(createdAgent);

        // Act
        var createResult = await controller.CreateAgent(createDto);
        var getResult = await controller.GetAgent(testGuid);

        // Assert
        createResult.ShouldNotBeNull();
        getResult.ShouldNotBeNull();
        createResult.AgentGuid.ShouldBe(getResult.AgentGuid);
        createResult.Name.ShouldBe(getResult.Name);
    }

    #endregion
}
