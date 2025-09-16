using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Service;
using Aevatar.WorkflowRun;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Controllers;

/// <summary>
/// 工作流控制器单元测试
/// </summary>
public class WorkflowControllerTests
{
    private readonly Mock<IWorkflowOrchestrationService> _mockWorkflowOrchestrationService;
    private readonly Mock<ITextCompletionService> _mockTextCompletionService;
    private readonly Mock<IWorkflowRunService> _mockWorkflowRunService;

    public WorkflowControllerTests()
    {
        _mockWorkflowOrchestrationService = new Mock<IWorkflowOrchestrationService>();
        _mockTextCompletionService = new Mock<ITextCompletionService>();
        _mockWorkflowRunService = new Mock<IWorkflowRunService>();
    }

    private WorkflowController CreateWorkflowController()
    {
        var controller = new WorkflowController(
            _mockWorkflowOrchestrationService.Object,
            _mockTextCompletionService.Object,
            _mockWorkflowRunService.Object
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
    public void WorkflowController_Should_Have_ApiController_Attribute()
    {
        // Arrange & Act
        var apiControllerAttribute = typeof(WorkflowController).GetCustomAttribute<ApiControllerAttribute>();

        // Assert
        apiControllerAttribute.ShouldNotBeNull();
    }

    [Fact]
    public void WorkflowController_Should_Have_Route_Attribute()
    {
        // Arrange & Act
        var routeAttribute = typeof(WorkflowController).GetCustomAttribute<RouteAttribute>();

        // Assert
        routeAttribute.ShouldNotBeNull();
        routeAttribute.Template.ShouldBe("api/workflow");
    }

    [Fact]
    public void WorkflowController_Should_Have_Proper_Controller_Name()
    {
        // Arrange & Act
        var controllerName = typeof(WorkflowController).Name.Replace("Controller", "");

        // Assert
        controllerName.ShouldBe("Workflow");
    }

    [Fact]
    public void WorkflowController_Constructor_Should_Accept_All_Dependencies()
    {
        // Arrange & Act & Assert
        var controller = Should.NotThrow(() => CreateWorkflowController());
        controller.ShouldNotBeNull();
    }

    #endregion

    #region GenerateAsync Tests

    [Fact]
    public void GenerateAsync_Should_Have_HttpPost_Attribute()
    {
        // Arrange
        var methodInfo = typeof(WorkflowController).GetMethod("GenerateAsync");

        // Act
        var httpPostAttribute = methodInfo.GetCustomAttribute<HttpPostAttribute>();

        // Assert
        httpPostAttribute.ShouldNotBeNull();
        httpPostAttribute.Template.ShouldBe("generate");
    }

    [Fact]
    public async Task GenerateAsync_WithValidRequest_ShouldReturnWorkflowConfig()
    {
        // Arrange
        var controller = CreateWorkflowController();
        var request = new Aevatar.Service.GenerateWorkflowRequestDto
        {
            UserGoal = "Create a data processing workflow for customer analytics"
        };

        var expectedResult = new AiWorkflowViewConfigDto
        {
            Name = "Customer Analytics Workflow",
            Properties = new AiWorkflowPropertiesDto
            {
                Name = "Customer Analytics Workflow",
                WorkflowNodeList = new List<AiWorkflowNodeDto>
                {
                    new AiWorkflowNodeDto
                    {
                        NodeId = "node1",
                        AgentType = "DataProcessorAgent",
                        Name = "Data Processor",
                        ExtendedData = new AiWorkflowNodeExtendedDataDto
                        {
                            XPosition = "100",
                            YPosition = "100"
                        }
                    }
                },
                WorkflowNodeUnitList = new List<AiWorkflowNodeUnitDto>()
            }
        };

        _mockWorkflowOrchestrationService.Setup(x => x.GenerateWorkflowAsync(request.UserGoal))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.GenerateAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Customer Analytics Workflow");
        result.Properties.WorkflowNodeList.Count.ShouldBe(1);
        _mockWorkflowOrchestrationService.Verify(x => x.GenerateWorkflowAsync(request.UserGoal), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WithEmptyUserGoal_ShouldCallService()
    {
        // Arrange
        var controller = CreateWorkflowController();
        var request = new Aevatar.Service.GenerateWorkflowRequestDto
        {
            UserGoal = ""
        };

        _mockWorkflowOrchestrationService.Setup(x => x.GenerateWorkflowAsync(It.IsAny<string>()))
            .ReturnsAsync((AiWorkflowViewConfigDto?)null);

        // Act
        var result = await controller.GenerateAsync(request);

        // Assert
        result.ShouldBeNull();
        _mockWorkflowOrchestrationService.Verify(x => x.GenerateWorkflowAsync(request.UserGoal), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_ServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var controller = CreateWorkflowController();
        var request = new Aevatar.Service.GenerateWorkflowRequestDto
        {
            UserGoal = "Test goal"
        };

        _mockWorkflowOrchestrationService.Setup(x => x.GenerateWorkflowAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => controller.GenerateAsync(request));
    }

    #endregion

    #region GenerateTextCompletionAsync Tests

    [Fact]
    public void GenerateTextCompletionAsync_Should_Have_HttpPost_Attribute()
    {
        // Arrange
        var methodInfo = typeof(WorkflowController).GetMethod("GenerateTextCompletionAsync");

        // Act
        var httpPostAttribute = methodInfo.GetCustomAttribute<HttpPostAttribute>();

        // Assert
        httpPostAttribute.ShouldNotBeNull();
        httpPostAttribute.Template.ShouldBe("text-completion/generate");
    }

    [Fact]
    public async Task GenerateTextCompletionAsync_WithValidRequest_ShouldReturnCompletions()
    {
        // Arrange
        var controller = CreateWorkflowController();
        var request = new TextCompletionRequestDto
        {
            UserGoal = "Create automated customer service workflow"
        };

        var expectedResult = new TextCompletionResponseDto
        {
            Completions = new List<string>
            {
                "Create automated customer service workflow with chatbot integration",
                "Create automated customer service workflow for email responses",
                "Create automated customer service workflow using AI agents",
                "Create automated customer service workflow with ticket routing",
                "Create automated customer service workflow for FAQ handling"
            }
        };

        _mockTextCompletionService.Setup(x => x.GenerateCompletionsAsync(request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.GenerateTextCompletionAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Completions.Count.ShouldBe(5);
        result.Completions[0].ShouldBe("Create automated customer service workflow with chatbot integration");
        _mockTextCompletionService.Verify(x => x.GenerateCompletionsAsync(request), Times.Once);
    }

    [Fact]
    public async Task GenerateTextCompletionAsync_WithShortUserGoal_ShouldStillCallService()
    {
        // Arrange
        var controller = CreateWorkflowController();
        var request = new TextCompletionRequestDto
        {
            UserGoal = "Short goal"
        };

        var expectedResult = new TextCompletionResponseDto
        {
            Completions = new List<string>()
        };

        _mockTextCompletionService.Setup(x => x.GenerateCompletionsAsync(request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.GenerateTextCompletionAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Completions.Count.ShouldBe(0);
        _mockTextCompletionService.Verify(x => x.GenerateCompletionsAsync(request), Times.Once);
    }

    [Fact]
    public async Task GenerateTextCompletionAsync_ServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var controller = CreateWorkflowController();
        var request = new TextCompletionRequestDto
        {
            UserGoal = "Test goal for completion"
        };

        _mockTextCompletionService.Setup(x => x.GenerateCompletionsAsync(It.IsAny<TextCompletionRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("Text completion service error"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => controller.GenerateTextCompletionAsync(request));
    }

    #endregion

    #region RunWorkflowAsync Tests

    [Fact]
    public void RunWorkflowAsync_Should_Have_HttpPost_Attribute()
    {
        // Arrange
        var methodInfo = typeof(WorkflowController).GetMethod("RunWorkflowAsync");

        // Act
        var httpPostAttribute = methodInfo.GetCustomAttribute<HttpPostAttribute>();

        // Assert
        httpPostAttribute.ShouldNotBeNull();
        httpPostAttribute.Template.ShouldBe("run");
    }

    [Fact]
    public async Task RunWorkflowAsync_WithValidRequest_ShouldReturnSuccessResult()
    {
        // Arrange
        var controller = CreateWorkflowController();
        var viewAgentId = Guid.NewGuid();
        var workflowId = Guid.NewGuid();

        var request = new WorkflowRunRequestDto
        {
            ViewAgentId = viewAgentId,
            EventProperties = new Dictionary<string, object>
            {
                {"property1", "value1"},
                {"property2", 123}
            }
        };

        var expectedResult = new WorkflowRunResultDto
        {
            IsSuccess = true,
            Message = "Workflow executed successfully",
            WorkflowId = workflowId
        };

        _mockWorkflowRunService.Setup(x => x.RunWorkflowAsync(request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.RunWorkflowAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Message.ShouldBe("Workflow executed successfully");
        result.WorkflowId.ShouldBe(workflowId);
        _mockWorkflowRunService.Verify(x => x.RunWorkflowAsync(request), Times.Once);
    }

    [Fact]
    public async Task RunWorkflowAsync_WithFailedExecution_ShouldReturnFailureResult()
    {
        // Arrange
        var controller = CreateWorkflowController();
        var viewAgentId = Guid.NewGuid();

        var request = new WorkflowRunRequestDto
        {
            ViewAgentId = viewAgentId,
            EventProperties = new Dictionary<string, object>()
        };

        var expectedResult = new WorkflowRunResultDto
        {
            IsSuccess = false,
            Message = "Workflow execution failed",
            WorkflowId = Guid.Empty
        };

        _mockWorkflowRunService.Setup(x => x.RunWorkflowAsync(request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.RunWorkflowAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.Message.ShouldBe("Workflow execution failed");
        result.WorkflowId.ShouldBe(Guid.Empty);
        _mockWorkflowRunService.Verify(x => x.RunWorkflowAsync(request), Times.Once);
    }

    [Fact]
    public async Task RunWorkflowAsync_WithEmptyGuid_ShouldStillCallService()
    {
        // Arrange
        var controller = CreateWorkflowController();

        var request = new WorkflowRunRequestDto
        {
            ViewAgentId = Guid.Empty,
            EventProperties = new Dictionary<string, object>()
        };

        var expectedResult = new WorkflowRunResultDto
        {
            IsSuccess = false,
            Message = "Invalid ViewAgentId",
            WorkflowId = Guid.Empty
        };

        _mockWorkflowRunService.Setup(x => x.RunWorkflowAsync(request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await controller.RunWorkflowAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        _mockWorkflowRunService.Verify(x => x.RunWorkflowAsync(request), Times.Once);
    }

    [Fact]
    public async Task RunWorkflowAsync_ServiceThrowsException_ShouldPropagateException()
    {
        // Arrange
        var controller = CreateWorkflowController();
        var request = new WorkflowRunRequestDto
        {
            ViewAgentId = Guid.NewGuid(),
            EventProperties = new Dictionary<string, object>()
        };

        _mockWorkflowRunService.Setup(x => x.RunWorkflowAsync(It.IsAny<WorkflowRunRequestDto>()))
            .ThrowsAsync(new InvalidOperationException("Workflow run service error"));

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            () => controller.RunWorkflowAsync(request));
    }

    #endregion

    #region GenerateWorkflowRequestDto Tests

    [Fact]
    public void GenerateWorkflowRequestDto_Should_Have_Default_Values()
    {
        // Arrange & Act
        var dto = new Aevatar.Service.GenerateWorkflowRequestDto();

        // Assert
        dto.UserGoal.ShouldBe(string.Empty);
    }

    [Fact]
    public void GenerateWorkflowRequestDto_Should_Accept_UserGoal()
    {
        // Arrange
        var userGoal = "Create an automated sales funnel workflow";

        // Act
        var dto = new Aevatar.Service.GenerateWorkflowRequestDto
        {
            UserGoal = userGoal
        };

        // Assert
        dto.UserGoal.ShouldBe(userGoal);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task WorkflowController_EndToEnd_ShouldWorkCorrectly()
    {
        // Arrange
        var controller = CreateWorkflowController();
        var userGoal = "Create complete customer onboarding workflow";

        // Setup GenerateAsync
        var workflowConfig = new AiWorkflowViewConfigDto
        {
            Name = "Customer Onboarding",
            Properties = new AiWorkflowPropertiesDto
            {
                Name = "Customer Onboarding",
                WorkflowNodeList = new List<AiWorkflowNodeDto>
                {
                    new AiWorkflowNodeDto
                    {
                        NodeId = "onboarding1",
                        AgentType = "OnboardingAgent",
                        Name = "Onboarding Step 1",
                        ExtendedData = new AiWorkflowNodeExtendedDataDto()
                    }
                },
                WorkflowNodeUnitList = new List<AiWorkflowNodeUnitDto>()
            }
        };

        _mockWorkflowOrchestrationService.Setup(x => x.GenerateWorkflowAsync(userGoal))
            .ReturnsAsync(workflowConfig);

        // Setup RunWorkflowAsync  
        var runRequest = new WorkflowRunRequestDto
        {
            ViewAgentId = Guid.NewGuid(),
            EventProperties = new Dictionary<string, object>()
        };

        var runResult = new WorkflowRunResultDto
        {
            IsSuccess = true,
            Message = "Workflow completed successfully",
            WorkflowId = Guid.NewGuid()
        };

        _mockWorkflowRunService.Setup(x => x.RunWorkflowAsync(runRequest))
            .ReturnsAsync(runResult);

        // Act - Generate workflow
        var generateRequest = new Aevatar.Service.GenerateWorkflowRequestDto { UserGoal = userGoal };
        var generatedWorkflow = await controller.GenerateAsync(generateRequest);

        // Act - Run workflow
        var workflowRunResult = await controller.RunWorkflowAsync(runRequest);

        // Assert
        generatedWorkflow.ShouldNotBeNull();
        generatedWorkflow.Name.ShouldBe("Customer Onboarding");
        
        workflowRunResult.ShouldNotBeNull();
        workflowRunResult.IsSuccess.ShouldBeTrue();

        _mockWorkflowOrchestrationService.Verify(x => x.GenerateWorkflowAsync(userGoal), Times.Once);
        _mockWorkflowRunService.Verify(x => x.RunWorkflowAsync(runRequest), Times.Once);
    }

    #endregion
}
