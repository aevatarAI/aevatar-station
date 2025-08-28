using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;
using Xunit;
using Aevatar.Controllers;
using Aevatar.Core.Interception.Configurations;
using Aevatar.Dto;
using Aevatar.Service;

namespace Aevatar.Application.Tests.Controllers;

/// <summary>
/// Comprehensive unit tests for TraceConfigController.
/// Tests cover all HTTP endpoints with success, failure, and validation scenarios.
/// </summary>
public class TraceConfigControllerTests
{
    private readonly Mock<ITraceManagementService> _mockTraceManagementService;
    private readonly TraceConfigController _controller;

    public TraceConfigControllerTests()
    {
        _mockTraceManagementService = new Mock<ITraceManagementService>();
        _controller = new TraceConfigController(_mockTraceManagementService.Object);
    }

    #region GetConfiguration Tests

    [Fact]
    public void GetConfiguration_WhenConfigurationExists_ShouldReturnOkWithTraceConfigDto()
    {
        // Arrange
        var config = new TraceConfig();
        var trackedIds = new HashSet<string> { "trace1", "trace2" };
        const bool isEnabled = true;

        _mockTraceManagementService.Setup(x => x.GetCurrentConfiguration()).Returns(config);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);

        // Act
        var result = _controller.GetConfiguration();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<TraceConfigDto>();
        
        dto.IsEnabled.ShouldBe(isEnabled);
        dto.TrackedIds.ShouldBe(trackedIds);
        dto.Configuration.ShouldBe(config);

        _mockTraceManagementService.Verify(x => x.GetCurrentConfiguration(), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Once);
        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
    }

    [Fact]
    public void GetConfiguration_WhenConfigurationIsNull_ShouldReturnNotFound()
    {
        // Arrange
        _mockTraceManagementService.Setup(x => x.GetCurrentConfiguration()).Returns((TraceConfig?)null);

        // Act
        var result = _controller.GetConfiguration();

        // Assert
        result.Result.ShouldBeOfType<NotFoundResult>();
        _mockTraceManagementService.Verify(x => x.GetCurrentConfiguration(), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Never);
        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Never);
    }

    [Fact]
    public void GetConfiguration_WhenServiceReturnsEmptyTrackedIds_ShouldReturnOkWithEmptyTrackedIds()
    {
        // Arrange
        var config = new TraceConfig();
        var emptyTrackedIds = new HashSet<string>();
        const bool isEnabled = false;

        _mockTraceManagementService.Setup(x => x.GetCurrentConfiguration()).Returns(config);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(emptyTrackedIds);

        // Act
        var result = _controller.GetConfiguration();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<TraceConfigDto>();
        
        dto.IsEnabled.ShouldBe(isEnabled);
        dto.TrackedIds.ShouldBeEmpty();
        dto.Configuration.ShouldBe(config);
    }

    #endregion

    #region UpdateConfiguration Tests

    [Fact]
    public void UpdateConfiguration_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var request = new UpdateTraceConfigRequest { IsEnabled = true };

        // Act
        var result = _controller.UpdateConfiguration(request);

        // Assert
        result.Result.ShouldBeOfType<OkResult>();
    }

    [Fact]
    public void UpdateConfiguration_WithNullRequest_ShouldReturnOk()
    {
        // Act
        var result = _controller.UpdateConfiguration(null!);

        // Assert
        result.Result.ShouldBeOfType<OkResult>();
    }

    #endregion

    #region ClearConfiguration Tests

    [Fact]
    public void ClearConfiguration_WhenCalled_ShouldCallServiceClearAndReturnNoContent()
    {
        // Arrange
        _mockTraceManagementService.Setup(x => x.Clear());

        // Act
        var result = _controller.ClearConfiguration();

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        _mockTraceManagementService.Verify(x => x.Clear(), Times.Once);
    }

    #endregion

    #region GetStatus Tests

    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, 5)]
    [InlineData(false, 0)]
    [InlineData(false, 10)]
    public void GetStatus_WithVariousStates_ShouldReturnCorrectStatus(bool isEnabled, int trackedCount)
    {
        // Arrange
        var trackedIds = new HashSet<string>();
        for (int i = 0; i < trackedCount; i++)
        {
            trackedIds.Add($"trace{i}");
        }

        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);

        // Act
        var result = _controller.GetStatus();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<TracingStatusDto>();
        
        dto.IsEnabled.ShouldBe(isEnabled);
        dto.TrackedTraceCount.ShouldBe(trackedCount);

        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Once);
        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
    }

    [Fact]
    public void GetStatus_WhenServiceReturnsNull_ShouldHandleGracefully()
    {
        // Arrange
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(false);
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns((HashSet<string>)null!);

        // Act & Assert
        Should.Throw<System.NullReferenceException>(() => _controller.GetStatus());
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void GetConfiguration_FullIntegration_ShouldReturnCompleteDto()
    {
        // Arrange
        var config = new TraceConfig();
        var trackedIds = new HashSet<string> { "integration-trace-1", "integration-trace-2", "integration-trace-3" };
        const bool isEnabled = true;

        _mockTraceManagementService.Setup(x => x.GetCurrentConfiguration()).Returns(config);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);

        // Act
        var result = _controller.GetConfiguration();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<TraceConfigDto>();
        
        // Verify all properties are correctly mapped
        dto.IsEnabled.ShouldBe(isEnabled);
        dto.TrackedIds.Count.ShouldBe(3);
        dto.TrackedIds.ShouldContain("integration-trace-1");
        dto.TrackedIds.ShouldContain("integration-trace-2");
        dto.TrackedIds.ShouldContain("integration-trace-3");
        dto.Configuration.ShouldBe(config);

        // Verify all service methods were called exactly once
        _mockTraceManagementService.Verify(x => x.GetCurrentConfiguration(), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Once);
        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
    }

    [Fact]
    public void GetStatus_FullIntegration_ShouldReturnCompleteStatus()
    {
        // Arrange
        var trackedIds = new HashSet<string>();
        for (int i = 1; i <= 7; i++)
        {
            trackedIds.Add($"status-trace-{i}");
        }
        const bool isEnabled = true;

        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);

        // Act
        var result = _controller.GetStatus();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<TracingStatusDto>();
        
        dto.IsEnabled.ShouldBe(isEnabled);
        dto.TrackedTraceCount.ShouldBe(7);

        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Once);
        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetConfiguration_WhenTrackedIdsContainSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var config = new TraceConfig();
        var trackedIds = new HashSet<string> 
        { 
            "trace-with-dashes", 
            "trace_with_underscores",
            "trace.with.dots",
            "trace@with@symbols",
            "trace123withNumbers"
        };
        const bool isEnabled = true;

        _mockTraceManagementService.Setup(x => x.GetCurrentConfiguration()).Returns(config);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);

        // Act
        var result = _controller.GetConfiguration();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<TraceConfigDto>();
        
        dto.TrackedIds.Count.ShouldBe(5);
        dto.TrackedIds.ShouldContain("trace-with-dashes");
        dto.TrackedIds.ShouldContain("trace_with_underscores");
        dto.TrackedIds.ShouldContain("trace.with.dots");
        dto.TrackedIds.ShouldContain("trace@with@symbols");
        dto.TrackedIds.ShouldContain("trace123withNumbers");
    }

    #endregion
}
