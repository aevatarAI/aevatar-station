using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Developer.Logger;
using Aevatar.Developer.Logger.Entities;
using Aevatar.Enum;
using Aevatar.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Controllers;

public class HostControllerTests
{
    private static IOptionsSnapshot<KubernetesOptions> CreateK8sOptions(string @namespace)
    {
        var mock = new Mock<IOptionsSnapshot<KubernetesOptions>>();
        mock.Setup(m => m.Value).Returns(new KubernetesOptions { AppNameSpace = @namespace });
        return mock.Object;
    }

    [Fact]
    public void HostController_Should_Have_Controller_Metadata()
    {
        // Ensure Controller has expected attributes
        typeof(HostController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Length.ShouldBeGreaterThan(0);
        var route = (RouteAttribute?)Attribute.GetCustomAttribute(typeof(HostController), typeof(RouteAttribute));
        route.ShouldNotBeNull();
        route!.Template.ShouldBe("api/host");
    }

    [Fact]
    public async Task GetLatestRealTimeLogs_Should_Use_Computed_Index_And_Return_Logs()
    {
        var logService = new Mock<ILogService>(MockBehavior.Strict);
        var ns = "ns";
        var appId = "app";
        var hostType = HostTypeEnum.Silo;
        var offset = 50;
        var expectedIndex = $"{ns}-{appId}-{hostType.ToString().ToLower()}-log-index";

        var logs = new List<HostLogIndex> { new HostLogIndex { AppLogId = Guid.NewGuid().ToString() } };

        logService.Setup(s => s.GetHostLogIndexAliasName(ns, $"{appId}-{hostType.ToString().ToLower()}", "1"))
                  .Returns(expectedIndex);
        logService.Setup(s => s.GetHostLatestLogAsync(expectedIndex, offset)).ReturnsAsync(logs);

        var controller = new HostController(logService.Object, CreateK8sOptions(ns));

        var result = await controller.GetLatestRealTimeLogs(appId, hostType, offset);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);

        logService.VerifyAll();
    }

    [Fact]
    public async Task GetWorkflowLogs_Should_Use_Computed_Silo_Index_And_Forward_Filters()
    {
        var logService = new Mock<ILogService>(MockBehavior.Strict);
        var ns = "prod";
        var appId = "myapp";
        var workflowId = Guid.NewGuid().ToString();
        var grainId = Guid.NewGuid().ToString();
        var level = "Information";
        var pattern = "started";
        const int pageSize = 10;
        var expectedIndex = $"{ns}-{appId}-silo-log-index";

        var logs = new List<HostLogIndex> { new HostLogIndex { AppLogId = Guid.NewGuid().ToString() } };

        logService.Setup(s => s.GetHostLogIndexAliasName(ns, $"{appId}-silo", "1")).Returns(expectedIndex);
        logService.Setup(s => s.GetWorkflowLogsAsync(expectedIndex, workflowId, grainId, level, pattern, pageSize))
                  .ReturnsAsync(logs);

        var controller = new HostController(logService.Object, CreateK8sOptions(ns));

        var result = await controller.GetWorkflowLogs(appId, workflowId, grainId, level, pattern, pageSize);

        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        logService.VerifyAll();
    }
}


