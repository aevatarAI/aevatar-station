using System;
using System.Threading.Tasks;
using Aevatar.Core.Interception.Services;
using Aevatar.Core.Interception.Configurations;
using Aevatar.Core.Tests.Interception.Infrastructure;
using Xunit;

namespace Aevatar.Core.Tests.Interception.Unit;

/// <summary>
/// Tests for runtime trace control functionality.
/// </summary>
[Collection("TraceContextTests")] // Ensure tests run sequentially to prevent static state interference
public class RuntimeTraceControlTests : IClassFixture<TraceContextFixture>, IDisposable
{
    private readonly ITraceManager _traceManager;
    private readonly TraceContextFixture _fixture;

    public RuntimeTraceControlTests(TraceContextFixture fixture)
    {
        _fixture = fixture;
        _traceManager = new TraceManager();
        
        // CRITICAL: Ensure each test starts with a clean TraceContext state
        _fixture.ResetTraceContext();
    }

    public void Dispose()
    {
        // No cleanup needed
    }

    [Fact]
    public void EnableTracing_WithValidTraceId_ShouldSucceed()
    {
        _fixture.ResetTraceContext();

        // Act
        var result = _traceManager.EnableTracing("test-123");

        // Assert
        Assert.True(result);
        Assert.True(_traceManager.IsTracingEnabled());
        
        var trackedIds = _traceManager.GetTrackedIds();
        Assert.Contains("test-123", trackedIds);
    }

    [Fact]
    public void EnableTracing_WithEmptyTraceId_ShouldFail()
    {
        _fixture.ResetTraceContext();

        // Act
        var result = _traceManager.EnableTracing("");

        // Assert
        Assert.False(result);
        // Should not change the tracing state - should remain enabled for backward compatibility
        Assert.True(_traceManager.IsTracingEnabled());
    }

    [Fact]
    public void EnableTracing_WithNullTraceId_ShouldFail()
    {
        _fixture.ResetTraceContext();

        // Act
        var result = _traceManager.EnableTracing(null!);

        // Assert
        Assert.False(result);
        // Should not change the tracing state - should remain enabled for backward compatibility
        Assert.True(_traceManager.IsTracingEnabled());
    }

    [Fact]
    public void DisableTracing_WithExistingTraceId_ShouldSucceed()
    {
        // Start with clean state for this test
        _traceManager.Clear();

        // Arrange
        _traceManager.EnableTracing("test-123");

        // Act
        var result = _traceManager.DisableTracing("test-123");

        // Assert
        Assert.True(result);
        // Note: IsTracingEnabled will return false when no trace IDs are tracked and no active trace ID
        Assert.False(_traceManager.IsTracingEnabled());
        
        var trackedIds = _traceManager.GetTrackedIds();
        Assert.DoesNotContain("test-123", trackedIds);
    }

    [Fact]
    public void DisableTracing_WithNonExistentTraceId_ShouldFail()
    {
        _fixture.ResetTraceContext();

        // Act
        var result = _traceManager.DisableTracing("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AddTrackedId_WithValidTraceId_ShouldSucceed()
    {
        _fixture.ResetTraceContext();

        // Act
        var result = _traceManager.AddTrackedId("test-456");

        // Assert
        Assert.True(result);
        var trackedIds = _traceManager.GetTrackedIds();
        Assert.Contains("test-456", trackedIds);
    }

    [Fact]
    public void AddTrackedId_WithExistingTraceId_ShouldFail()
    {
        _fixture.ResetTraceContext();

        // Arrange
        _traceManager.AddTrackedId("test-456");

        // Act
        var result = _traceManager.AddTrackedId("test-456");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveTrackedId_WithExistingTraceId_ShouldSucceed()
    {
        _fixture.ResetTraceContext();

        // Arrange
        _traceManager.AddTrackedId("test-456");

        // Act
        var result = _traceManager.RemoveTrackedId("test-456");

        // Assert
        Assert.True(result);
        var trackedIds = _traceManager.GetTrackedIds();
        Assert.DoesNotContain("test-456", trackedIds);
    }

    [Fact]
    public void RemoveTrackedId_WithNonExistentTraceId_ShouldFail()
    {
        _fixture.ResetTraceContext();

        // Act
        var result = _traceManager.RemoveTrackedId("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Clear_ShouldResetAllState()
    {
        _fixture.ResetTraceContext();

        // Arrange
        _traceManager.EnableTracing("test-123");
        _traceManager.AddTrackedId("test-456");

        // Act
        _traceManager.Clear();

        // Assert
        Assert.False(_traceManager.IsTracingEnabled());
        var trackedIds = _traceManager.GetTrackedIds();
        Assert.Empty(trackedIds);
    }

    [Fact]
    public void GetCurrentConfiguration_ShouldReturnConfiguration()
    {
        _fixture.ResetTraceContext();

        // Arrange
        _traceManager.EnableTracing("test-123");

        // Act
        var config = _traceManager.GetCurrentConfiguration();

        // Assert
        Assert.NotNull(config);
        Assert.True(config.Enabled);
        Assert.Contains("test-123", config.TrackedIds);
    }

    [Fact]
    public void MultipleTraceIds_ShouldBeTrackedIndependently()
    {
        // Start with clean state for this test
        _traceManager.Clear();

        // Arrange & Act
        _traceManager.EnableTracing("trace-1");
        _traceManager.AddTrackedId("trace-2");
        _traceManager.AddTrackedId("trace-3");

        // Assert
        var trackedIds = _traceManager.GetTrackedIds();
        Assert.Contains("trace-1", trackedIds);
        Assert.Contains("trace-2", trackedIds);
        Assert.Contains("trace-3", trackedIds);
        Assert.Equal(3, trackedIds.Count);
    }

    [Fact]
    public void DisableTracing_ShouldRemoveFromTrackedIds()
    {
        _fixture.ResetTraceContext();

        // Arrange
        _traceManager.EnableTracing("trace-1");
        _traceManager.AddTrackedId("trace-2");

        // Act
        _traceManager.DisableTracing("trace-1");

        // Assert
        var trackedIds = _traceManager.GetTrackedIds();
        Assert.DoesNotContain("trace-1", trackedIds);
        Assert.Contains("trace-2", trackedIds);
    }

    [Fact]
    public void IsTracingEnabled_WithoutConfiguration_ShouldReturnTrue()
    {
        _fixture.ResetTraceContext();

        // Act & Assert
        // When no configuration is set, tracing should be enabled by default for backward compatibility
        Assert.True(_traceManager.IsTracingEnabled());
    }

    [Fact]
    public void IsTracingEnabled_WithDisabledConfiguration_ShouldReturnFalse()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var config = new TraceConfig { Enabled = false };
        _traceManager.SetTraceConfig(config);

        // Act & Assert
        Assert.False(_traceManager.IsTracingEnabled());
    }

    [Fact]
    public void IsTracingEnabled_WithEnabledConfiguration_ShouldReturnTrue()
    {
        // Start with clean state for this test
        _traceManager.Clear();

        // Arrange - Set up enabled config with a trace ID to track
        var config = new TraceConfig { Enabled = true };
        config.AddTrackedId("test-trace");
        _traceManager.SetTraceConfig(config);
        
        // Set an active trace ID that matches the tracked ID
        _traceManager.EnableTracing("test-trace");

        // Act & Assert
        Assert.True(_traceManager.IsTracingEnabled());
    }
}
