using System;
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
/// Comprehensive unit tests for TrackedTracesController.
/// Tests cover all HTTP endpoints with success, failure, and validation scenarios.
/// </summary>
public class TrackedTracesControllerTests
{
    private readonly Mock<ITraceManagementService> _mockTraceManagementService;
    private readonly TrackedTracesController _controller;

    public TrackedTracesControllerTests()
    {
        _mockTraceManagementService = new Mock<ITraceManagementService>();
        _controller = new TrackedTracesController(_mockTraceManagementService.Object);
    }

    #region GetTrackedTraces Tests

    [Fact]
    public void GetTrackedTraces_WhenTrackedIdsExist_ShouldReturnOkWithTrackedTracesDto()
    {
        // Arrange
        var trackedIds = new HashSet<string> { "trace1", "trace2", "trace3", "trace4" };
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);

        // Act
        var result = _controller.GetTrackedTraces();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<TrackedTracesDto>();
        
        dto.Count.ShouldBe(4);
        dto.TraceIds.Count.ShouldBe(4);
        dto.TraceIds.ShouldContain("trace1");
        dto.TraceIds.ShouldContain("trace2");
        dto.TraceIds.ShouldContain("trace3");
        dto.TraceIds.ShouldContain("trace4");

        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
    }

    [Fact]
    public void GetTrackedTraces_WhenNoTrackedIds_ShouldReturnOkWithEmptyDto()
    {
        // Arrange
        var emptyTrackedIds = new HashSet<string>();
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(emptyTrackedIds);

        // Act
        var result = _controller.GetTrackedTraces();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<TrackedTracesDto>();
        
        dto.Count.ShouldBe(0);
        dto.TraceIds.ShouldBeEmpty();

        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
    }

    #endregion

    #region AddTrackedTrace Tests

    [Fact]
    public void AddTrackedTrace_WithValidNewTraceId_ShouldReturnCreatedWithTrackedTraceDto()
    {
        // Arrange
        var request = new AddTrackedTraceRequest { TraceId = "new-trace", IsEnabled = true };
        var existingTrackedIds = new HashSet<string> { "existing-trace1", "existing-trace2" };
        
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(existingTrackedIds);
        _mockTraceManagementService.Setup(x => x.AddTrackedId("new-trace", true)).Returns(true);

        // Act
        var result = _controller.AddTrackedTrace(request);

        // Assert
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        var dto = createdResult.Value.ShouldBeOfType<TrackedTraceDto>();
        
        dto.TraceId.ShouldBe("new-trace");
        dto.IsEnabled.ShouldBeTrue();
        dto.AddedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
        dto.AddedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);

        createdResult.ActionName.ShouldBe(nameof(TrackedTracesController.GetTrackedTrace));
        createdResult.RouteValues!["traceId"].ShouldBe("new-trace");

        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
        _mockTraceManagementService.Verify(x => x.AddTrackedId("new-trace", true), Times.Once);
    }

    [Fact]
    public void AddTrackedTrace_WithValidNewTraceIdDisabled_ShouldReturnCreatedWithDisabledTrace()
    {
        // Arrange
        var request = new AddTrackedTraceRequest { TraceId = "new-trace", IsEnabled = false };
        var existingTrackedIds = new HashSet<string> { "existing-trace" };
        
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(existingTrackedIds);
        _mockTraceManagementService.Setup(x => x.AddTrackedId("new-trace", false)).Returns(true);

        // Act
        var result = _controller.AddTrackedTrace(request);

        // Assert
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        var dto = createdResult.Value.ShouldBeOfType<TrackedTraceDto>();
        
        dto.TraceId.ShouldBe("new-trace");
        dto.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void AddTrackedTrace_WithExistingTraceId_ShouldReturnConflict()
    {
        // Arrange
        var request = new AddTrackedTraceRequest { TraceId = "existing-trace", IsEnabled = true };
        var existingTrackedIds = new HashSet<string> { "existing-trace", "other-trace" };
        
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(existingTrackedIds);

        // Act
        var result = _controller.AddTrackedTrace(request);

        // Assert
        var conflictResult = result.Result.ShouldBeOfType<ConflictObjectResult>();
        conflictResult.Value.ShouldBe("Trace ID 'existing-trace' is already being tracked.");

        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
        _mockTraceManagementService.Verify(x => x.AddTrackedId(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddTrackedTrace_WithInvalidTraceId_ShouldReturnBadRequest(string traceId)
    {
        // Arrange
        var request = new AddTrackedTraceRequest { TraceId = traceId, IsEnabled = true };

        // Act
        var result = _controller.AddTrackedTrace(request);

        // Assert
        var badRequestResult = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.Value.ShouldBe("Trace ID is required.");

        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Never);
        _mockTraceManagementService.Verify(x => x.AddTrackedId(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void AddTrackedTrace_WhenServiceReturnsFalse_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new AddTrackedTraceRequest { TraceId = "new-trace", IsEnabled = true };
        var existingTrackedIds = new HashSet<string> { "other-trace" };
        
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(existingTrackedIds);
        _mockTraceManagementService.Setup(x => x.AddTrackedId("new-trace", true)).Returns(false);

        // Act
        var result = _controller.AddTrackedTrace(request);

        // Assert
        var badRequestResult = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.Value.ShouldBe("Failed to add trace ID 'new-trace' to tracking.");
    }

    #endregion

    #region GetTrackedTrace Tests

    [Fact]
    public void GetTrackedTrace_WithValidExistingTraceId_ShouldReturnOkWithTrackedTraceDto()
    {
        // Arrange
        const string traceId = "existing-trace";
        var trackedIds = new HashSet<string> { traceId, "other-trace" };
        const bool isEnabled = true;

        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(isEnabled);

        // Act
        var result = _controller.GetTrackedTrace(traceId);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<TrackedTraceDto>();
        
        dto.TraceId.ShouldBe(traceId);
        dto.IsEnabled.ShouldBe(isEnabled);
        dto.AddedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
        dto.AddedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);

        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Once);
    }

    [Fact]
    public void GetTrackedTrace_WithNonExistingTraceId_ShouldReturnNotFound()
    {
        // Arrange
        const string traceId = "non-existing-trace";
        var trackedIds = new HashSet<string> { "other-trace1", "other-trace2" };

        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);

        // Act
        var result = _controller.GetTrackedTrace(traceId);

        // Assert
        var notFoundResult = result.Result.ShouldBeOfType<NotFoundObjectResult>();
        notFoundResult.Value.ShouldBe("Trace ID 'non-existing-trace' is not being tracked.");

        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetTrackedTrace_WithInvalidTraceId_ShouldReturnBadRequest(string traceId)
    {
        // Act
        var result = _controller.GetTrackedTrace(traceId);

        // Assert
        var badRequestResult = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.Value.ShouldBe("Trace ID is required.");

        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Never);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Never);
    }

    #endregion

    #region RemoveTrackedTrace Tests

    [Fact]
    public void RemoveTrackedTrace_WhenSuccessful_ShouldReturnNoContent()
    {
        // Arrange
        const string traceId = "trace-to-remove";
        _mockTraceManagementService.Setup(x => x.RemoveTrackedId(traceId)).Returns(true);

        // Act
        var result = _controller.RemoveTrackedTrace(traceId);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        _mockTraceManagementService.Verify(x => x.RemoveTrackedId(traceId), Times.Once);
    }

    [Fact]
    public void RemoveTrackedTrace_WhenServiceReturnsFalse_ShouldReturnNotFound()
    {
        // Arrange
        const string traceId = "non-existing-trace";
        _mockTraceManagementService.Setup(x => x.RemoveTrackedId(traceId)).Returns(false);

        // Act
        var result = _controller.RemoveTrackedTrace(traceId);

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        notFoundResult.Value.ShouldBe("Trace ID 'non-existing-trace' not found in tracked traces or could not be removed.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RemoveTrackedTrace_WithInvalidTraceId_ShouldReturnBadRequest(string traceId)
    {
        // Act
        var result = _controller.RemoveTrackedTrace(traceId);

        // Assert
        var badRequestResult = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.Value.ShouldBe("Trace ID is required.");

        _mockTraceManagementService.Verify(x => x.RemoveTrackedId(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ClearTrackedTraces Tests

    [Fact]
    public void ClearTrackedTraces_WhenCalled_ShouldCallServiceClearAndReturnNoContent()
    {
        // Arrange
        _mockTraceManagementService.Setup(x => x.Clear());

        // Act
        var result = _controller.ClearTrackedTraces();

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        _mockTraceManagementService.Verify(x => x.Clear(), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AddTrackedTrace_FullWorkflow_ShouldWorkEndToEnd()
    {
        // Arrange
        var request = new AddTrackedTraceRequest { TraceId = "integration-trace", IsEnabled = true };
        var initialTrackedIds = new HashSet<string> { "existing-trace" };
        var updatedTrackedIds = new HashSet<string> { "existing-trace", "integration-trace" };
        
        // Setup for AddTrackedTrace
        _mockTraceManagementService.SetupSequence(x => x.GetTrackedIds())
            .Returns(initialTrackedIds)  // For AddTrackedTrace check
            .Returns(updatedTrackedIds); // For GetTrackedTrace verification
        
        _mockTraceManagementService.Setup(x => x.AddTrackedId("integration-trace", true)).Returns(true);
        _mockTraceManagementService.Setup(x => x.IsTracingEnabled()).Returns(true);

        // Act - Add tracked trace
        var addResult = _controller.AddTrackedTrace(request);
        
        // Verify add result
        var createdResult = addResult.Result.ShouldBeOfType<CreatedAtActionResult>();
        var addedDto = createdResult.Value.ShouldBeOfType<TrackedTraceDto>();
        addedDto.TraceId.ShouldBe("integration-trace");
        addedDto.IsEnabled.ShouldBeTrue();

        // Act - Get tracked trace to verify it was added
        var getResult = _controller.GetTrackedTrace("integration-trace");
        
        // Verify get result
        var okResult = getResult.Result.ShouldBeOfType<OkObjectResult>();
        var retrievedDto = okResult.Value.ShouldBeOfType<TrackedTraceDto>();
        retrievedDto.TraceId.ShouldBe("integration-trace");

        // Verify service calls
        _mockTraceManagementService.Verify(x => x.GetTrackedIds(), Times.Exactly(2));
        _mockTraceManagementService.Verify(x => x.AddTrackedId("integration-trace", true), Times.Once);
        _mockTraceManagementService.Verify(x => x.IsTracingEnabled(), Times.Once);
    }

    [Fact]
    public void GetTrackedTraces_FullIntegration_ShouldReturnCompleteData()
    {
        // Arrange
        var trackedIds = new HashSet<string> 
        { 
            "integration-trace-1", 
            "integration-trace-2", 
            "integration-trace-3",
            "integration-trace-4",
            "integration-trace-5"
        };
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);

        // Act
        var result = _controller.GetTrackedTraces();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<TrackedTracesDto>();
        
        dto.Count.ShouldBe(5);
        dto.TraceIds.Count.ShouldBe(5);
        
        // Verify all trace IDs are present
        foreach (var traceId in trackedIds)
        {
            dto.TraceIds.ShouldContain(traceId);
        }

        // Verify the List contains exactly the same items as the HashSet
        var dtoTraceIds = dto.TraceIds.ToHashSet();
        dtoTraceIds.ShouldBe(trackedIds);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AddTrackedTrace_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var request = new AddTrackedTraceRequest 
        { 
            TraceId = "trace-with-special@chars#123!_", 
            IsEnabled = true 
        };
        var existingTrackedIds = new HashSet<string>();
        
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(existingTrackedIds);
        _mockTraceManagementService.Setup(x => x.AddTrackedId("trace-with-special@chars#123!_", true)).Returns(true);

        // Act
        var result = _controller.AddTrackedTrace(request);

        // Assert
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        var dto = createdResult.Value.ShouldBeOfType<TrackedTraceDto>();
        
        dto.TraceId.ShouldBe("trace-with-special@chars#123!_");
    }

    [Fact]
    public void GetTrackedTraces_WithLargeNumberOfTraces_ShouldHandleCorrectly()
    {
        // Arrange
        var trackedIds = new HashSet<string>();
        for (int i = 1; i <= 1000; i++)
        {
            trackedIds.Add($"trace-{i:D4}");
        }
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(trackedIds);

        // Act
        var result = _controller.GetTrackedTraces();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var dto = okResult.Value.ShouldBeOfType<TrackedTracesDto>();
        
        dto.Count.ShouldBe(1000);
        dto.TraceIds.Count.ShouldBe(1000);
        
        // Spot check a few trace IDs
        dto.TraceIds.ShouldContain("trace-0001");
        dto.TraceIds.ShouldContain("trace-0500");
        dto.TraceIds.ShouldContain("trace-1000");
    }

    [Fact]
    public void AddTrackedTrace_WithVeryLongTraceId_ShouldHandleCorrectly()
    {
        // Arrange
        var longTraceId = new string('a', 2000); // 2000 character trace ID
        var request = new AddTrackedTraceRequest { TraceId = longTraceId, IsEnabled = true };
        var existingTrackedIds = new HashSet<string>();
        
        _mockTraceManagementService.Setup(x => x.GetTrackedIds()).Returns(existingTrackedIds);
        _mockTraceManagementService.Setup(x => x.AddTrackedId(longTraceId, true)).Returns(true);

        // Act
        var result = _controller.AddTrackedTrace(request);

        // Assert
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        var dto = createdResult.Value.ShouldBeOfType<TrackedTraceDto>();
        
        dto.TraceId.ShouldBe(longTraceId);
        dto.TraceId.Length.ShouldBe(2000);
    }

    #endregion
}
