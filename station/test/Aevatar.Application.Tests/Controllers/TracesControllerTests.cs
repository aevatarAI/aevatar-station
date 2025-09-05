using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;
using Xunit;
using Aevatar.Controllers;
using Aevatar.Dto;
using Aevatar.Service;

namespace Aevatar.Application.Tests.Controllers;

/// <summary>
/// Comprehensive unit tests for TracesController.
/// Tests cover all HTTP endpoints with success, failure, and validation scenarios.
/// </summary>
public class TracesControllerTests
{
    private readonly Mock<ITraceManagementService> _mockTraceManagementService;
    private readonly TracesController _controller;

    public TracesControllerTests()
    {
        _mockTraceManagementService = new Mock<ITraceManagementService>();
        _controller = new TracesController(_mockTraceManagementService.Object);
    }

    #region GetTraces Tests

    [Fact]
    public void GetTraces_WhenTrackedIdsExist_ShouldReturnOkWithTraceList()
    {
        // Arrange
        var trackedIds = new HashSet<string> { "trace1", "trace2", "trace3" };
        const bool isEnabled = true;

        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);

        // Act
        var result = _controller.GetTraces();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var traceList = okResult.Value.ShouldBeOfType<List<TraceDto>>();
        
        traceList.Count.ShouldBe(3);
        traceList.All(t => t.IsEnabled == isEnabled).ShouldBeTrue();
        traceList.Select(t => t.TraceId).ShouldContain("trace1");
        traceList.Select(t => t.TraceId).ShouldContain("trace2");
        traceList.Select(t => t.TraceId).ShouldContain("trace3");

        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Exactly(3));
    }

    [Fact]
    public void GetTraces_WhenNoTrackedIds_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        var emptyTrackedIds = new HashSet<string>();
        const bool isEnabled = false;

        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(emptyTrackedIds);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);

        // Act
        var result = _controller.GetTraces();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var traceList = okResult.Value.ShouldBeOfType<List<TraceDto>>();
        
        traceList.ShouldBeEmpty();
        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Never);
    }

    [Fact]
    public void GetTraces_WhenTracingDisabled_ShouldReturnTracesWithDisabledStatus()
    {
        // Arrange
        var trackedIds = new HashSet<string> { "trace1", "trace2" };
        const bool isEnabled = false;

        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);

        // Act
        var result = _controller.GetTraces();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var traceList = okResult.Value.ShouldBeOfType<List<TraceDto>>();
        
        traceList.Count.ShouldBe(2);
        traceList.All(t => t.IsEnabled == false).ShouldBeTrue();
        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Exactly(2));
    }

    #endregion

    #region GetTrace Tests

    [Fact]
    public void GetTrace_WithValidExistingTraceId_ShouldReturnOkWithTraceDto()
    {
        // Arrange
        const string traceId = "existing-trace";
        var trackedIds = new HashSet<string> { traceId, "other-trace" };
        const bool isEnabled = true;

        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);

        // Act
        var result = _controller.GetTrace(traceId);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var trace = okResult.Value.ShouldBeOfType<TraceDto>();
        
        trace.TraceId.ShouldBe(traceId);
        trace.IsEnabled.ShouldBe(isEnabled);

        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Once);
    }

    [Fact]
    public void GetTrace_WithNonExistingTraceId_ShouldReturnNotFound()
    {
        // Arrange
        const string traceId = "non-existing-trace";
        var trackedIds = new HashSet<string> { "other-trace1", "other-trace2" };

        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);

        // Act
        var result = _controller.GetTrace(traceId);

        // Assert
        var notFoundResult = result.Result.ShouldBeOfType<NotFoundObjectResult>();
        notFoundResult.Value.ShouldBe("Trace ID 'non-existing-trace' not found in tracked traces.");

        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetTrace_WithInvalidTraceId_ShouldReturnBadRequest(string traceId)
    {
        // Act
        var result = _controller.GetTrace(traceId);

        // Assert
        var badRequestResult = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.Value.ShouldBe("Trace ID is required.");

        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Never);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Never);
    }

    #endregion

    #region UpdateTrace Tests

    [Fact]
    public void UpdateTrace_EnableTracing_WhenSuccessful_ShouldReturnOkWithUpdatedTrace()
    {
        // Arrange
        const string traceId = "test-trace";
        var request = new UpdateTraceRequest { IsEnabled = true };
        
        _mockTraceManagementService.Setup(x => x.EnableTracing(traceId, true)).Returns(true);

        // Act
        var result = _controller.UpdateTrace(traceId, request);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var updatedTrace = okResult.Value.ShouldBeOfType<TraceDto>();
        
        updatedTrace.TraceId.ShouldBe(traceId);
        updatedTrace.IsEnabled.ShouldBeTrue();

        _mockTraceManagementService.Verify(x => x.EnableTracing(traceId, true), Times.Once);
        _mockTraceManagementService.Verify(x => x.DisableTracing(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void UpdateTrace_DisableTracing_WhenSuccessful_ShouldReturnOkWithUpdatedTrace()
    {
        // Arrange
        const string traceId = "test-trace";
        var request = new UpdateTraceRequest { IsEnabled = false };
        
        _mockTraceManagementService.Setup(x => x.DisableTracing(traceId)).Returns(true);

        // Act
        var result = _controller.UpdateTrace(traceId, request);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var updatedTrace = okResult.Value.ShouldBeOfType<TraceDto>();
        
        updatedTrace.TraceId.ShouldBe(traceId);
        updatedTrace.IsEnabled.ShouldBeFalse();

        _mockTraceManagementService.Verify(x => x.DisableTracing(traceId), Times.Once);
        _mockTraceManagementService.Verify(x => x.EnableTracing(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void UpdateTrace_WhenServiceReturnsFalse_ShouldReturnBadRequest()
    {
        // Arrange
        const string traceId = "test-trace";
        var request = new UpdateTraceRequest { IsEnabled = true };
        
        _mockTraceManagementService.Setup(x => x.EnableTracing(traceId, true)).Returns(false);

        // Act
        var result = _controller.UpdateTrace(traceId, request);

        // Assert
        var badRequestResult = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.Value.ShouldBe("Failed to update trace 'test-trace'.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateTrace_WithInvalidTraceId_ShouldReturnBadRequest(string traceId)
    {
        // Arrange
        var request = new UpdateTraceRequest { IsEnabled = true };

        // Act
        var result = _controller.UpdateTrace(traceId, request);

        // Assert
        var badRequestResult = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.Value.ShouldBe("Trace ID is required.");

        _mockTraceManagementService.Verify(x => x.EnableTracing(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        _mockTraceManagementService.Verify(x => x.DisableTracing(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region DeleteTrace Tests

    [Fact]
    public void DeleteTrace_WhenSuccessful_ShouldReturnNoContent()
    {
        // Arrange
        const string traceId = "test-trace";
        _mockTraceManagementService.Setup(x => x.RemoveTrackedId(traceId)).Returns(true);

        // Act
        var result = _controller.DeleteTrace(traceId);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        _mockTraceManagementService.Verify(x => x.RemoveTrackedId(traceId), Times.Once);
    }

    [Fact]
    public void DeleteTrace_WhenServiceReturnsFalse_ShouldReturnNotFound()
    {
        // Arrange
        const string traceId = "non-existing-trace";
        _mockTraceManagementService.Setup(x => x.RemoveTrackedId(traceId)).Returns(false);

        // Act
        var result = _controller.DeleteTrace(traceId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        notFoundResult.Value.ShouldBe("Trace ID 'non-existing-trace' not found or could not be removed.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeleteTrace_WithInvalidTraceId_ShouldReturnBadRequest(string traceId)
    {
        // Act
        var result = _controller.DeleteTrace(traceId);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.Value.ShouldBe("Trace ID is required.");

        _mockTraceManagementService.Verify(x => x.RemoveTrackedId(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void GetTraces_FullIntegration_ShouldReturnCompleteTraceList()
    {
        // Arrange
        var trackedIds = new HashSet<string> 
        { 
            "integration-trace-1", 
            "integration-trace-2", 
            "integration-trace-3",
            "integration-trace-4"
        };
        const bool isEnabled = true;

        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);

        // Act
        var result = _controller.GetTraces();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var traceList = okResult.Value.ShouldBeOfType<List<TraceDto>>();
        
        traceList.Count.ShouldBe(4);
        
        // Verify each trace has correct properties
        foreach (var trace in traceList)
        {
            trace.TraceId.ShouldStartWith("integration-trace-");
            trace.IsEnabled.ShouldBe(isEnabled);
        }

        // Verify all original trace IDs are present
        var resultTraceIds = traceList.Select(t => t.TraceId).ToHashSet();
        resultTraceIds.ShouldBe(trackedIds);
        
        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Exactly(4));
    }

    [Fact]
    public void UpdateTrace_FullIntegration_EnableToDisable_ShouldHandleCorrectly()
    {
        // Arrange
        const string traceId = "integration-trace";
        var enableRequest = new UpdateTraceRequest { IsEnabled = true };
        var disableRequest = new UpdateTraceRequest { IsEnabled = false };
        
        _mockTraceManagementService.Setup(x => x.EnableTracing(traceId, true)).Returns(true);
        _mockTraceManagementService.Setup(x => x.DisableTracing(traceId)).Returns(true);

        // Act - Enable first
        var enableResult = _controller.UpdateTrace(traceId, enableRequest);
        
        // Act - Then disable
        var disableResult = _controller.UpdateTrace(traceId, disableRequest);

        // Assert
        var enableOkResult = enableResult.Result.ShouldBeOfType<OkObjectResult>();
        var enabledTrace = enableOkResult.Value.ShouldBeOfType<TraceDto>();
        enabledTrace.IsEnabled.ShouldBeTrue();

        var disableOkResult = disableResult.Result.ShouldBeOfType<OkObjectResult>();
        var disabledTrace = disableOkResult.Value.ShouldBeOfType<TraceDto>();
        disabledTrace.IsEnabled.ShouldBeFalse();

        _mockTraceManagementService.Verify(x => x.EnableTracing(traceId, true), Times.Once);
        _mockTraceManagementService.Verify(x => x.DisableTracing(traceId), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetTraces_WithSpecialCharacterTraceIds_ShouldHandleCorrectly()
    {
        // Arrange
        var trackedIds = new HashSet<string> 
        { 
            "trace-with-dashes",
            "trace_with_underscores",
            "trace.with.dots",
            "trace123withNumbers",
            "TRACE_WITH_CAPS",
            "trace@with#symbols!"
        };
        const bool isEnabled = true;

        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);

        // Act
        var result = _controller.GetTraces();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var traceList = okResult.Value.ShouldBeOfType<List<TraceDto>>();
        
        traceList.Count.ShouldBe(6);
        
        var resultTraceIds = traceList.Select(t => t.TraceId).ToHashSet();
        resultTraceIds.ShouldBe(trackedIds);
        
        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Exactly(6));
    }

    [Fact]
    public void GetTrace_WithVeryLongTraceId_ShouldHandleCorrectly()
    {
        // Arrange
        var longTraceId = new string('a', 1000); // 1000 character trace ID
        var trackedIds = new HashSet<string> { longTraceId };
        const bool isEnabled = true;

        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);

        // Act
        var result = _controller.GetTrace(longTraceId);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var trace = okResult.Value.ShouldBeOfType<TraceDto>();
        
        trace.TraceId.ShouldBe(longTraceId);
        trace.TraceId.Length.ShouldBe(1000);
    }

    #endregion
}
