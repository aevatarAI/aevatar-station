// ABOUTME: This file contains isolated unit tests for UserController CopyDeploymentWithPattern API endpoint
// ABOUTME: Tests validate API controller behavior and parameter handling

using System;
using System.Threading.Tasks;
using Aevatar.Admin.Controllers;
using Aevatar.Service;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Aevatar.Permissions;

namespace Aevatar.Application.Tests.Controllers;

public class UserControllerUnitTests
{
    private readonly Mock<IUserAppService> _mockUserAppService;
    private readonly Mock<IDeveloperService> _mockDeveloperService;
    private readonly Mock<ILogger<UserController>> _mockLogger;

    public UserControllerUnitTests()
    {
        _mockUserAppService = new Mock<IUserAppService>();
        _mockDeveloperService = new Mock<IDeveloperService>();
        _mockLogger = new Mock<ILogger<UserController>>();
    }

    private UserController CreateUserController()
    {
        var controller = new UserController(
            _mockUserAppService.Object,
            _mockDeveloperService.Object,
            _mockLogger.Object
        );

        // Setup HttpContext for testing
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    [Fact]
    public void UserController_Should_Have_CopyDeploymentWithPattern_Method()
    {
        // Arrange & Act
        var methodInfo = typeof(UserController).GetMethod("CopyDeploymentWithPattern");

        // Assert
        methodInfo.ShouldNotBeNull();
        methodInfo.ReturnType.ShouldBe(typeof(Task));
        
        var parameters = methodInfo.GetParameters();
        parameters.Length.ShouldBe(4);
        parameters[0].Name.ShouldBe("clientId");
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("sourceVersion");
        parameters[1].ParameterType.ShouldBe(typeof(string));
        parameters[2].Name.ShouldBe("targetVersion");
        parameters[2].ParameterType.ShouldBe(typeof(string));
        parameters[3].Name.ShouldBe("siloNamePattern");
        parameters[3].ParameterType.ShouldBe(typeof(string));
    }

    [Fact]
    public void CopyDeploymentWithPattern_Should_Have_HttpPost_Attribute()
    {
        // Arrange
        var methodInfo = typeof(UserController).GetMethod("CopyDeploymentWithPattern");

        // Act
        var httpPostAttribute = methodInfo.GetCustomAttribute<HttpPostAttribute>();

        // Assert
        httpPostAttribute.ShouldNotBeNull();
        httpPostAttribute.Template.ShouldBe("CopyDeploymentWithPattern");
    }

    [Fact]
    public void CopyDeploymentWithPattern_Should_Have_Authorize_Attribute()
    {
        // Arrange
        var methodInfo = typeof(UserController).GetMethod("CopyDeploymentWithPattern");

        // Act
        var authorizeAttribute = methodInfo.GetCustomAttribute<AuthorizeAttribute>();

        // Assert
        authorizeAttribute.ShouldNotBeNull();
        authorizeAttribute.Policy.ShouldBe(AevatarPermissions.AdminPolicy);
    }

    [Fact]
    public async Task CopyDeploymentWithPattern_Should_Call_DeveloperService()
    {
        // Arrange
        var controller = CreateUserController();
        var clientId = "testclient";
        var sourceVersion = "1";
        var targetVersion = "2";
        var siloNamePattern = "User";

        _mockDeveloperService
            .Setup(x => x.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern))
            .Returns(Task.CompletedTask);

        // Act
        await controller.CopyDeploymentWithPattern(clientId, sourceVersion, targetVersion, siloNamePattern);

        // Assert
        _mockDeveloperService.Verify(
            x => x.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern),
            Times.Once);
    }

    [Fact]
    public async Task CopyDeploymentWithPattern_Should_Handle_Service_Exception()
    {
        // Arrange
        var controller = CreateUserController();
        var clientId = "testclient";
        var sourceVersion = "1";
        var targetVersion = "2";
        var siloNamePattern = "User";

        _mockDeveloperService
            .Setup(x => x.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern))
            .ThrowsAsync(new InvalidOperationException("Source deployment not found"));

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            controller.CopyDeploymentWithPattern(clientId, sourceVersion, targetVersion, siloNamePattern));

        exception.Message.ShouldBe("Source deployment not found");
        _mockDeveloperService.Verify(
            x => x.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern),
            Times.Once);
    }

    [Fact]
    public async Task CopyDeploymentWithPattern_Should_Accept_Valid_Parameters()
    {
        // Arrange
        var controller = CreateUserController();
        var testCases = new[]
        {
            new { ClientId = "client1", SourceVersion = "1", TargetVersion = "2", SiloNamePattern = "User" },
            new { ClientId = "test-client", SourceVersion = "v1.0", TargetVersion = "v2.0", SiloNamePattern = "Scheduler" },
            new { ClientId = "demo", SourceVersion = "1.0.0", TargetVersion = "1.1.0", SiloNamePattern = "Projector" }
        };

        _mockDeveloperService
            .Setup(x => x.CopyDeploymentWithPatternAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act & Assert
        foreach (var testCase in testCases)
        {
            await Should.NotThrowAsync(() =>
                controller.CopyDeploymentWithPattern(testCase.ClientId, testCase.SourceVersion, testCase.TargetVersion, testCase.SiloNamePattern));
        }

        // Verify all calls were made
        _mockDeveloperService.Verify(
            x => x.CopyDeploymentWithPatternAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(testCases.Length));
    }

    [Fact]
    public void UserController_Should_Have_Route_Attribute()
    {
        // Arrange & Act
        var routeAttribute = typeof(UserController).GetCustomAttribute<RouteAttribute>();

        // Assert
        routeAttribute.ShouldNotBeNull();
        routeAttribute.Template.ShouldBe("api/users");
    }

    [Fact]
    public void UserController_Should_Have_Proper_Controller_Name()
    {
        // Arrange & Act
        var controllerName = typeof(UserController).Name.Replace("Controller", "");

        // Assert
        controllerName.ShouldBe("User");
    }

    [Fact]
    public async Task CopyDeploymentWithPattern_Should_Complete_Without_Return_Value()
    {
        // Arrange
        var controller = CreateUserController();
        var clientId = "testclient";
        var sourceVersion = "1";
        var targetVersion = "2";
        var siloNamePattern = "User";

        _mockDeveloperService
            .Setup(x => x.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern))
            .Returns(Task.CompletedTask);

        // Act
        await controller.CopyDeploymentWithPattern(clientId, sourceVersion, targetVersion, siloNamePattern);

        // Assert - Method should complete without returning a value (void Task)
        // No assertion needed for void Task method
        _mockDeveloperService.Verify(
            x => x.CopyDeploymentWithPatternAsync(clientId, sourceVersion, targetVersion, siloNamePattern),
            Times.Once);
    }
}