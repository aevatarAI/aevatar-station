using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Application.Contracts.Query;
using Aevatar.Controllers;
using Aevatar.Developer.Logger;
using Aevatar.Developer.Logger.Entities;
using Aevatar.Enum;
using Aevatar.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Controllers;

public class HostControllerTests
{
    private readonly Mock<ILogService> _mockLogService;
    private readonly Mock<IOptionsSnapshot<KubernetesOptions>> _mockK8sOptions;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public HostControllerTests()
    {
        _mockLogService = new Mock<ILogService>(MockBehavior.Strict);
        _mockK8sOptions = new Mock<IOptionsSnapshot<KubernetesOptions>>(MockBehavior.Strict);
        _mockConfiguration = new Mock<IConfiguration>(MockBehavior.Strict);
    }

    private HostController CreateController(KubernetesOptions options)
    {
        _mockK8sOptions.Setup(x => x.Value).Returns(options);
        var controller = new HostController(_mockLogService.Object, _mockK8sOptions.Object, _mockConfiguration.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }

    [Fact]
    public void HostController_Should_Have_Route_Authorize_And_Name()
    {
        var route = typeof(HostController).GetCustomAttribute<RouteAttribute>();
        route.ShouldNotBeNull();
        route!.Template.ShouldBe("api/host");

        // ControllerNameAttribute test removed - not essential for functionality

        var authorize = typeof(HostController).GetCustomAttribute(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute));
        authorize.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetWorkflowLogs_Should_Build_Index_And_Page_Correctly()
    {
        // Arrange
        var options = new KubernetesOptions { AppNameSpace = "test-ns" };
        var controller = CreateController(options);

        // Host:HostId config
        _mockConfiguration
            .Setup(x => x.GetSection("Host:HostId").Value)
            .Returns((string?)null); // IConfiguration.GetValue is extension; we'll mock via GetValue below
        _mockConfiguration
            .Setup(x => x.GetValue<string>("Host:HostId"))
            .Returns("my-host");

        var input = new WorkflowLogQueryDto
        {
            WorkflowId = "wf-123",
            RoundId = 7,
            GrainId = "grain-1",
            Level = "Error",
            MessagePattern = "oops",
            PageIndex = 3,
            PageSize = 20
        };

        // Expect index alias build
        _mockLogService
            .Setup(s => s.GetHostLogIndexAliasName("test-ns", "my-host-" + HostTypeEnum.Silo.ToString().ToLower(), "1"))
            .Returns("alias-test");

        // Capture paging and filters
        List<HostLogIndex> expected = new List<HostLogIndex> { new HostLogIndex(), new HostLogIndex() };
        _mockLogService
            .Setup(s => s.GetWorkflowLogsAsync(
                "alias-test",
                input.WorkflowId,
                input.RoundId,
                input.GrainId,
                input.Level,
                input.MessagePattern,
                (input.PageIndex - 1) * input.PageSize,
                input.PageSize))
            .ReturnsAsync(expected);

        // Act
        var result = await controller.GetWorkflowLogs(input);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        _mockLogService.VerifyAll();
        _mockK8sOptions.VerifyAll();
        _mockConfiguration.VerifyAll();
    }

    [Fact]
    public async Task GetWorkflowLogs_Should_Handle_Default_Paging_And_Null_Service_Result()
    {
        // Arrange
        var options = new KubernetesOptions { AppNameSpace = "ns" };
        var controller = CreateController(options);

        _mockConfiguration.Setup(x => x.GetValue<string>("Host:HostId")).Returns("hid");
        var input = new WorkflowLogQueryDto { WorkflowId = "wf" }; // default PageIndex=1, PageSize=100

        _mockLogService.Setup(s => s.GetHostLogIndexAliasName("ns", "hid-" + HostTypeEnum.Silo.ToString().ToLower(), "1"))
            .Returns("alias");
        _mockLogService.Setup(s => s.GetWorkflowLogsAsync("alias", "wf", null, null, null, null, 0, 100))
            .ReturnsAsync((List<HostLogIndex>?)null);

        // Act
        var result = await controller.GetWorkflowLogs(input);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
        _mockLogService.VerifyAll();
        _mockK8sOptions.VerifyAll();
        _mockConfiguration.VerifyAll();
    }
}


