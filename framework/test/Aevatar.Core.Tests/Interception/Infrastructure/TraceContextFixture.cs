using Aevatar.Core.Interception.Configurations;
using Aevatar.Core.Interception.Context;

namespace Aevatar.Core.Tests.Interception.Infrastructure;

/// <summary>
/// Fixture class that provides per-test TraceContext isolation.
/// This ensures each test method starts with a clean TraceContext state.
/// 
/// Usage:
/// 1. Implement IClassFixture<TraceContextFixture> in your test class
/// 2. Call _fixture.ResetTraceContext() in constructor or before each test
/// 3. The fixture will automatically clean up after all tests complete
/// </summary>
public class TraceContextFixture : IDisposable
{
    public TraceContextFixture()
    {
        // Initialize with default enabled configuration
        ResetTraceContext();
    }

    /// <summary>
    /// Resets TraceContext to a clean state with default enabled configuration.
    /// Call this before each test method to ensure isolation.
    /// </summary>
    public void ResetTraceContext()
    {
        // Clear any existing state
        TraceContext.Clear();
        
        // CRITICAL: Set up default enabled configuration since IsTracingEnabled now returns false when config is null
        // Tests expect tracing to be enabled by default for backward compatibility
        TraceContext.UpdateTraceConfig(config =>
        {
            config.Enabled = true;
            config.TrackedIds.Clear();
            // Add a default trace ID so that tracing works even when no specific trace ID is set
            config.AddTrackedId("default-test-trace-id");
        });
        
        // Set a default active trace ID so that tracing works
        TraceContext.ActiveTraceId = "default-test-trace-id";
    }

    /// <summary>
    /// Resets TraceContext to a completely clean state without any default trace IDs.
    /// Use this for tests that need to count exact tracked IDs without defaults.
    /// </summary>
    public void ClearTraceContext()
    {
        // Clear any existing state
        TraceContext.Clear();
        
        // Set up enabled configuration but WITHOUT any default trace IDs
        TraceContext.UpdateTraceConfig(config =>
        {
            config.Enabled = true;
            config.TrackedIds.Clear();
            // NO default trace ID added - completely clean state
        });
        
        // No default active trace ID
        TraceContext.ActiveTraceId = null;
    }

    public void Dispose()
    {
        // Clean up at the end of all tests in the class
        TraceContext.Clear();
        TraceContext.ActiveTraceId = null;
    }
}
