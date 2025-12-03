using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using Aevatar.Core.Interception.Configurations;
using Aevatar.Core.Interception.Services;
using Aevatar.Service;

namespace Aevatar.Application.Tests.Service;

/// <summary>
/// Comprehensive unit tests for TraceManagementService.
/// Tests cover all service methods with success, failure, and exception scenarios.
/// </summary>
public class TraceManagementServiceTests
{
    private readonly Mock<ITraceManager> _mockTraceManager;
    private readonly Mock<ILogger<TraceManagementService>> _mockLogger;
    private readonly TraceManagementService _service;

    public TraceManagementServiceTests()
    {
        _mockTraceManager = new Mock<ITraceManager>();
        _mockLogger = new Mock<ILogger<TraceManagementService>>();
        _service = new TraceManagementService(_mockTraceManager.Object, _mockLogger.Object);
    }

    #region GetCurrentConfiguration Tests

    [Fact]
    public void GetCurrentConfiguration_WhenTraceManagerReturnsConfig_ShouldReturnConfig()
    {
        // Arrange
        var expectedConfig = new TraceConfig();
        _mockTraceManager.Setup(x => x.GetCurrentConfiguration()).Returns(expectedConfig);

        // Act
        var result = _service.GetCurrentConfiguration();

        // Assert
        result.ShouldBe(expectedConfig);
        _mockTraceManager.Verify(x => x.GetCurrentConfiguration(), Times.Once);
        VerifyDebugLog("Retrieved current trace configuration: {Config}");
    }

    [Fact]
    public void GetCurrentConfiguration_WhenTraceManagerReturnsNull_ShouldReturnNull()
    {
        // Arrange
        _mockTraceManager.Setup(x => x.GetCurrentConfiguration()).Returns((TraceConfig?)null);

        // Act
        var result = _service.GetCurrentConfiguration();

        // Assert
        result.ShouldBeNull();
        _mockTraceManager.Verify(x => x.GetCurrentConfiguration(), Times.Once);
    }

    [Fact]
    public void GetCurrentConfiguration_WhenExceptionThrown_ShouldReturnNullAndLogError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _mockTraceManager.Setup(x => x.GetCurrentConfiguration()).Throws(exception);

        // Act
        var result = _service.GetCurrentConfiguration();

        // Assert
        result.ShouldBeNull();
        VerifyErrorLog("Failed to retrieve current trace configuration", exception);
    }

    #endregion

    #region GetTrackedIds Tests

    [Fact]
    public void GetTrackedIds_WhenTraceManagerReturnsIds_ShouldReturnIds()
    {
        // Arrange
        var expectedIds = new HashSet<string> { "trace1", "trace2", "trace3" };
        _mockTraceManager.Setup(x => x.GetTrackedIds()).Returns(expectedIds);

        // Act
        var result = _service.GetTrackedIds();

        // Assert
        result.ShouldBe(expectedIds);
        _mockTraceManager.Verify(x => x.GetTrackedIds(), Times.Once);
        VerifyDebugLog("Retrieved tracked trace IDs: {Count} IDs");
    }

    [Fact]
    public void GetTrackedIds_WhenTraceManagerReturnsEmptySet_ShouldReturnEmptySet()
    {
        // Arrange
        var expectedIds = new HashSet<string>();
        _mockTraceManager.Setup(x => x.GetTrackedIds()).Returns(expectedIds);

        // Act
        var result = _service.GetTrackedIds();

        // Assert
        result.ShouldBeEmpty();
        _mockTraceManager.Verify(x => x.GetTrackedIds(), Times.Once);
    }

    [Fact]
    public void GetTrackedIds_WhenExceptionThrown_ShouldReturnEmptySetAndLogError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _mockTraceManager.Setup(x => x.GetTrackedIds()).Throws(exception);

        // Act
        var result = _service.GetTrackedIds();

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
        VerifyErrorLog("Failed to retrieve tracked trace IDs", exception);
    }

    #endregion

    #region IsTracingEnabled Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IsTracingEnabled_WhenTraceManagerReturnsValue_ShouldReturnSameValue(bool enabled)
    {
        // Arrange
        _mockTraceManager.Setup(x => x.IsTracingEnabled()).Returns(enabled);

        // Act
        var result = _service.IsTracingEnabled();

        // Assert
        result.ShouldBe(enabled);
        _mockTraceManager.Verify(x => x.IsTracingEnabled(), Times.Once);
        VerifyDebugLog("Tracing enabled status: {IsEnabled}");
    }

    [Fact]
    public void IsTracingEnabled_WhenExceptionThrown_ShouldReturnFalseAndLogError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _mockTraceManager.Setup(x => x.IsTracingEnabled()).Throws(exception);

        // Act
        var result = _service.IsTracingEnabled();

        // Assert
        result.ShouldBeFalse();
        VerifyErrorLog("Failed to check if tracing is enabled", exception);
    }

    #endregion

    #region EnableTracing Tests

    [Fact]
    public void EnableTracing_WithValidTraceIdAndEnabled_ShouldCallEnableTracingAndReturnTrue()
    {
        // Arrange
        const string traceId = "test-trace-id";
        _mockTraceManager.Setup(x => x.EnableTracing(traceId)).Returns(true);

        // Act
        var result = _service.EnableTracing(traceId, true);

        // Assert
        result.ShouldBeTrue();
        _mockTraceManager.Verify(x => x.EnableTracing(traceId), Times.Once);
        _mockTraceManager.Verify(x => x.DisableTracing(It.IsAny<string>()), Times.Never);
        VerifyInformationLog("Successfully enabled tracing for trace ID: test-trace-id");
    }

    [Fact]
    public void EnableTracing_WithValidTraceIdAndDisabled_ShouldCallDisableTracingAndReturnTrue()
    {
        // Arrange
        const string traceId = "test-trace-id";
        _mockTraceManager.Setup(x => x.DisableTracing(traceId)).Returns(true);

        // Act
        var result = _service.EnableTracing(traceId, false);

        // Assert
        result.ShouldBeTrue();
        _mockTraceManager.Verify(x => x.DisableTracing(traceId), Times.Once);
        _mockTraceManager.Verify(x => x.EnableTracing(It.IsAny<string>()), Times.Never);
        VerifyInformationLog("Successfully disabled tracing for trace ID: test-trace-id");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EnableTracing_WithInvalidTraceId_ShouldReturnFalseAndLogWarning(string traceId)
    {
        // Act
        var result = _service.EnableTracing(traceId, true);

        // Assert
        result.ShouldBeFalse();
        _mockTraceManager.Verify(x => x.EnableTracing(It.IsAny<string>()), Times.Never);
        _mockTraceManager.Verify(x => x.DisableTracing(It.IsAny<string>()), Times.Never);
        VerifyWarningLog("Attempted to enable tracing with null or empty trace ID");
    }

    [Fact]
    public void EnableTracing_WhenTraceManagerReturnsFalse_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        const string traceId = "test-trace-id";
        _mockTraceManager.Setup(x => x.EnableTracing(traceId)).Returns(false);

        // Act
        var result = _service.EnableTracing(traceId, true);

        // Assert
        result.ShouldBeFalse();
        VerifyWarningLog("Failed to enable tracing for trace ID: test-trace-id");
    }

    [Fact]
    public void EnableTracing_WhenExceptionThrown_ShouldReturnFalseAndLogError()
    {
        // Arrange
        const string traceId = "test-trace-id";
        var exception = new InvalidOperationException("Test exception");
        _mockTraceManager.Setup(x => x.EnableTracing(traceId)).Throws(exception);

        // Act
        var result = _service.EnableTracing(traceId, true);

        // Assert
        result.ShouldBeFalse();
        VerifyErrorLog("Exception occurred while enabling tracing for trace ID: test-trace-id", exception);
    }

    #endregion

    #region DisableTracing Tests

    [Fact]
    public void DisableTracing_WithValidTraceId_ShouldCallEnableTracingWithFalse()
    {
        // Arrange
        const string traceId = "test-trace-id";
        _mockTraceManager.Setup(x => x.DisableTracing(traceId)).Returns(true);

        // Act
        var result = _service.DisableTracing(traceId);

        // Assert
        result.ShouldBeTrue();
        _mockTraceManager.Verify(x => x.DisableTracing(traceId), Times.Once);
    }

    #endregion

    #region AddTrackedId Tests

    [Fact]
    public void AddTrackedId_WithValidTraceIdAndEnabled_ShouldAddAndEnableTracing()
    {
        // Arrange
        const string traceId = "test-trace-id";
        _mockTraceManager.Setup(x => x.AddTrackedId(traceId)).Returns(true);
        _mockTraceManager.Setup(x => x.EnableTracing(traceId)).Returns(true);

        // Act
        var result = _service.AddTrackedId(traceId, true);

        // Assert
        result.ShouldBeTrue();
        _mockTraceManager.Verify(x => x.AddTrackedId(traceId), Times.Once);
        _mockTraceManager.Verify(x => x.EnableTracing(traceId), Times.Once);
        VerifyInformationLog("Successfully added trace ID to tracking: test-trace-id (enabled: True)");
    }

    [Fact]
    public void AddTrackedId_WithValidTraceIdAndDisabled_ShouldAddButNotEnableTracing()
    {
        // Arrange
        const string traceId = "test-trace-id";
        _mockTraceManager.Setup(x => x.AddTrackedId(traceId)).Returns(true);

        // Act
        var result = _service.AddTrackedId(traceId, false);

        // Assert
        result.ShouldBeTrue();
        _mockTraceManager.Verify(x => x.AddTrackedId(traceId), Times.Once);
        _mockTraceManager.Verify(x => x.EnableTracing(It.IsAny<string>()), Times.Never);
        VerifyInformationLog("Successfully added trace ID to tracking: test-trace-id (enabled: False)");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddTrackedId_WithInvalidTraceId_ShouldReturnFalseAndLogWarning(string traceId)
    {
        // Act
        var result = _service.AddTrackedId(traceId, true);

        // Assert
        result.ShouldBeFalse();
        _mockTraceManager.Verify(x => x.AddTrackedId(It.IsAny<string>()), Times.Never);
        VerifyWarningLog("Attempted to add tracked ID with null or empty trace ID");
    }

    [Fact]
    public void AddTrackedId_WhenTraceManagerReturnsFalse_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        const string traceId = "test-trace-id";
        _mockTraceManager.Setup(x => x.AddTrackedId(traceId)).Returns(false);

        // Act
        var result = _service.AddTrackedId(traceId, true);

        // Assert
        result.ShouldBeFalse();
        VerifyWarningLog("Failed to add trace ID to tracking: test-trace-id");
    }

    [Fact]
    public void AddTrackedId_WhenExceptionThrown_ShouldReturnFalseAndLogError()
    {
        // Arrange
        const string traceId = "test-trace-id";
        var exception = new InvalidOperationException("Test exception");
        _mockTraceManager.Setup(x => x.AddTrackedId(traceId)).Throws(exception);

        // Act
        var result = _service.AddTrackedId(traceId, true);

        // Assert
        result.ShouldBeFalse();
        VerifyErrorLog("Exception occurred while adding trace ID to tracking: test-trace-id", exception);
    }

    #endregion

    #region RemoveTrackedId Tests

    [Fact]
    public void RemoveTrackedId_WithValidTraceId_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        const string traceId = "test-trace-id";
        _mockTraceManager.Setup(x => x.RemoveTrackedId(traceId)).Returns(true);

        // Act
        var result = _service.RemoveTrackedId(traceId);

        // Assert
        result.ShouldBeTrue();
        _mockTraceManager.Verify(x => x.RemoveTrackedId(traceId), Times.Once);
        VerifyInformationLog("Successfully removed trace ID from tracking: test-trace-id");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RemoveTrackedId_WithInvalidTraceId_ShouldReturnFalseAndLogWarning(string traceId)
    {
        // Act
        var result = _service.RemoveTrackedId(traceId);

        // Assert
        result.ShouldBeFalse();
        _mockTraceManager.Verify(x => x.RemoveTrackedId(It.IsAny<string>()), Times.Never);
        VerifyWarningLog("Attempted to remove tracked ID with null or empty trace ID");
    }

    [Fact]
    public void RemoveTrackedId_WhenTraceManagerReturnsFalse_ShouldReturnFalseAndLogWarning()
    {
        // Arrange
        const string traceId = "test-trace-id";
        _mockTraceManager.Setup(x => x.RemoveTrackedId(traceId)).Returns(false);

        // Act
        var result = _service.RemoveTrackedId(traceId);

        // Assert
        result.ShouldBeFalse();
        VerifyWarningLog("Failed to remove trace ID from tracking: test-trace-id");
    }

    [Fact]
    public void RemoveTrackedId_WhenExceptionThrown_ShouldReturnFalseAndLogError()
    {
        // Arrange
        const string traceId = "test-trace-id";
        var exception = new InvalidOperationException("Test exception");
        _mockTraceManager.Setup(x => x.RemoveTrackedId(traceId)).Throws(exception);

        // Act
        var result = _service.RemoveTrackedId(traceId);

        // Assert
        result.ShouldBeFalse();
        VerifyErrorLog("Exception occurred while removing trace ID from tracking: test-trace-id", exception);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_WhenTraceManagerSucceeds_ShouldCallClearAndLogInformation()
    {
        // Arrange
        _mockTraceManager.Setup(x => x.Clear());

        // Act
        _service.Clear();

        // Assert
        _mockTraceManager.Verify(x => x.Clear(), Times.Once);
        VerifyInformationLog("Successfully cleared trace context");
    }

    [Fact]
    public void Clear_WhenExceptionThrown_ShouldLogError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _mockTraceManager.Setup(x => x.Clear()).Throws(exception);

        // Act
        _service.Clear();

        // Assert
        VerifyErrorLog("Exception occurred while clearing trace context", exception);
    }

    #endregion

    #region Helper Methods

    private void VerifyDebugLog(string messageTemplate)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyInformationLog(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyWarningLog(string expectedMessage)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private void VerifyErrorLog(string expectedMessage, Exception? expectedException = null)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                expectedException ?? It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
