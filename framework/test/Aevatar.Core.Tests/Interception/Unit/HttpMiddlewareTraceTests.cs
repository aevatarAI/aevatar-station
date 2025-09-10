using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Core.Interception.Context;
using Aevatar.Core.Interception.Configurations;
using Aevatar.Core.Tests.Interception.Infrastructure;
using Xunit;

namespace Aevatar.Core.Tests.Interception.Unit;

/// <summary>
/// Tests for HTTP middleware trace integration and cross-boundary scenarios.
/// </summary>
[Collection("TraceContextTests")] // Ensure tests run sequentially to prevent static state interference
public class HttpMiddlewareTraceTests : IClassFixture<TraceContextFixture>, IDisposable
{
    private readonly TraceContextFixture _fixture;

    public HttpMiddlewareTraceTests(TraceContextFixture fixture)
    {
        _fixture = fixture;
        
        // CRITICAL: Ensure each test starts with a clean TraceContext state
        _fixture.ResetTraceContext();
    }

    public void Dispose()
    {
        
    }

    [Fact]
    public void TraceContextMiddleware_ShouldExtractTraceIdFromHeaders()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var traceId = "http-header-trace-123";
        
        // Simulate HTTP headers (this would normally be done by the middleware)
        // Since we can't access HttpContext in tests, we'll test the underlying logic
        
        // Act - Set trace context as if middleware extracted it from headers
        TraceContext.EnableTracing(traceId); // Enable tracing for this specific ID
        
        // Assert - Trace context should be set correctly
        Assert.Equal(traceId, TraceContext.ActiveTraceId);
        Assert.True(TraceContext.IsTracingEnabled);
    }

    [Fact]
    public void TraceContextMiddleware_ShouldHandleMissingTraceId()
    {
        // Arrange - No trace ID in headers (use clear instead of reset for empty state)
        TraceContext.Clear();
        
        // Act - Verify default behavior
        var currentTraceId = TraceContext.ActiveTraceId;
        var isTracingEnabled = TraceContext.IsTracingEnabled;
        
        // Assert - Should handle missing trace ID gracefully
        Assert.Null(currentTraceId);
        Assert.False(isTracingEnabled); // With no config, tracing should be disabled
    }

    [Fact]
    public void TraceContextMiddleware_ShouldHandleEmptyTraceId()
    {
        // Arrange - Start with clear state and set up config
        TraceContext.Clear();
        TraceContext.UpdateTraceConfig(config => config.Enabled = true);
        
        var emptyTraceId = "";
        
        // Act - Try to set empty trace ID
        TraceContext.ActiveTraceId = emptyTraceId;
        
        // Assert - Should handle empty trace ID gracefully
        // Note: Empty strings may be converted to null by the implementation
        var actualTraceId = TraceContext.ActiveTraceId;
        Assert.True(string.IsNullOrEmpty(actualTraceId), "Should handle empty trace ID gracefully");
        Assert.False(TraceContext.IsTracingEnabled); // Empty trace ID should not be traceable
    }

    [Fact]
    public async Task HttpToOrleans_ShouldPropagateTraceContext()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var httpTraceId = "http-to-orleans-trace";
        var httpConfig = new TraceConfig { Enabled = true };
        
        // Simulate HTTP middleware setting trace context
        TraceContext.ActiveTraceId = httpTraceId;
        TraceContext.UpdateTraceConfig(config => 
        {
            config.Enabled = httpConfig.Enabled;
        });
        
        // Verify HTTP context is set
        Assert.Equal(httpTraceId, TraceContext.ActiveTraceId);
        Assert.Equal(httpConfig.Enabled, TraceContext.GetTraceConfig()?.Enabled);

        // Act - Simulate Orleans grain call from HTTP context
        await SimulateOrleansCallFromHttp();
        
        // Assert - Trace context should be preserved
        Assert.Equal(httpTraceId, TraceContext.ActiveTraceId);
        var finalConfig = TraceContext.GetTraceConfig();
        Assert.NotNull(finalConfig);
        Assert.Equal(httpConfig.Enabled, finalConfig.Enabled);
    }

    [Fact]
    public async Task OrleansToHttp_ShouldPropagateTraceContext()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var orleansTraceId = "orleans-to-http-trace";
        var orleansConfig = new TraceConfig { Enabled = true };
        
        // Simulate Orleans context setting trace context
        TraceContext.ActiveTraceId = orleansTraceId;
        TraceContext.UpdateTraceConfig(config => 
        {
            config.Enabled = orleansConfig.Enabled;
        });
        
        // Verify Orleans context is set
        Assert.Equal(orleansTraceId, TraceContext.ActiveTraceId);
        Assert.Equal(orleansConfig.Enabled, TraceContext.GetTraceConfig()?.Enabled);

        // Act - Simulate HTTP request from Orleans context
        await SimulateHttpRequestFromOrleans();
        
        // Assert - Trace context should be preserved
        Assert.Equal(orleansTraceId, TraceContext.ActiveTraceId);
        var finalConfig = TraceContext.GetTraceConfig();
        Assert.NotNull(finalConfig);
        Assert.Equal(orleansConfig.Enabled, finalConfig.Enabled);
    }

    [Fact]
    public async Task MixedHttpAndOrleansContexts_ShouldMaintainIsolation()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var httpTraceId = "mixed-http-trace";
        var orleansTraceId = "mixed-orleans-trace";
        
        // Start with HTTP context
        TraceContext.ActiveTraceId = httpTraceId;
        Assert.Equal(httpTraceId, TraceContext.ActiveTraceId);

        // Act - Simulate Orleans call that changes context
        await Task.Run(async () =>
        {
            // Simulate Orleans context
            TraceContext.ActiveTraceId = orleansTraceId;
            Assert.Equal(orleansTraceId, TraceContext.ActiveTraceId);
            
            // Simulate nested Orleans call
            await Task.Run(async () =>
            {
                // Verify Orleans trace context is preserved
                Assert.Equal(orleansTraceId, TraceContext.ActiveTraceId);
                
                await Task.Delay(10);
                
                // Verify trace context is still preserved
                Assert.Equal(orleansTraceId, TraceContext.ActiveTraceId);
            });
            
            // Verify Orleans trace context is preserved after nested call
            Assert.Equal(orleansTraceId, TraceContext.ActiveTraceId);
        });

        // Assert - Original HTTP context should be restored
        Assert.Equal(httpTraceId, TraceContext.ActiveTraceId);
    }

    [Fact]
    public async Task HttpRequestPipeline_ShouldMaintainTraceContext()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var traceId = "http-pipeline-trace";
        var config = new TraceConfig { Enabled = true };
        
        // Simulate HTTP request starting
        TraceContext.ActiveTraceId = traceId;
        TraceContext.UpdateTraceConfig(config => 
        {
            config.Enabled = config.Enabled;
        });
        
        // Verify initial state
        Assert.Equal(traceId, TraceContext.ActiveTraceId);
        Assert.Equal(config.Enabled, TraceContext.GetTraceConfig()?.Enabled);

        // Act - Simulate HTTP pipeline processing
        await SimulateHttpPipelineProcessing();
        
        // Assert - Trace context should be preserved through the pipeline
        Assert.Equal(traceId, TraceContext.ActiveTraceId);
        var finalConfig = TraceContext.GetTraceConfig();
        Assert.NotNull(finalConfig);
        Assert.Equal(config.Enabled, finalConfig.Enabled);
    }

    [Fact]
    public async Task ConcurrentHttpRequests_ShouldMaintainSeparateTraceContexts()
    {
        _fixture.ResetTraceContext();

        // Arrange
        const int requestCount = 6;
        var results = new System.Collections.Concurrent.ConcurrentDictionary<int, string?>();
        var tasks = new Task[requestCount];

        // Act - Start multiple concurrent HTTP requests
        for (int i = 0; i < requestCount; i++)
        {
            var requestId = i;
            tasks[i] = Task.Run(async () =>
            {
                var traceId = $"concurrent-http-{requestId:D2}";
                
                // Simulate HTTP middleware setting trace context
                TraceContext.ActiveTraceId = traceId;
                
                // Verify it's set correctly
                Assert.Equal(traceId, TraceContext.ActiveTraceId);
                
                // Simulate HTTP request processing
                await Task.Delay(20);
                
                // Verify trace context is preserved
                var preservedTraceId = TraceContext.ActiveTraceId;
                results[requestId] = preservedTraceId;
                
                // Simulate more processing
                await Task.Delay(10);
                
                // Verify trace context is still preserved
                Assert.Equal(traceId, TraceContext.ActiveTraceId);
            });
        }

        // Wait for all requests to complete
        await Task.WhenAll(tasks);

        // Assert - Each request should have maintained its own trace context
        Assert.Equal(requestCount, results.Count);
        for (int i = 0; i < requestCount; i++)
        {
            Assert.Equal($"concurrent-http-{i:D2}", results[i]);
        }
    }

    [Fact]
    public async Task HttpMiddlewareChain_ShouldPreserveTraceContext()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var traceId = "middleware-chain-trace";
        
        // Simulate first middleware setting trace context
        TraceContext.ActiveTraceId = traceId;
        Assert.Equal(traceId, TraceContext.ActiveTraceId);

        // Act - Simulate middleware chain
        await SimulateMiddlewareChain();
        
        // Assert - Trace context should be preserved through the entire middleware chain
        Assert.Equal(traceId, TraceContext.ActiveTraceId);
    }

    [Fact]
    public async Task CrossBoundaryTracePropagation_ShouldWorkCorrectly()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var initialTraceId = "cross-boundary-trace";
        
        // Set initial trace context
        TraceContext.ActiveTraceId = initialTraceId;
        Assert.Equal(initialTraceId, TraceContext.ActiveTraceId);

        // Act - Simulate complex cross-boundary scenario
        await SimulateCrossBoundaryScenario();
        
        // Assert - Trace context should be preserved across all boundaries
        Assert.Equal(initialTraceId, TraceContext.ActiveTraceId);
    }

    private async Task SimulateOrleansCallFromHttp()
    {
        // Simulate Orleans grain call from HTTP context
        await Task.Run(async () =>
        {
            // Verify trace context flows to Orleans context
            Assert.NotNull(TraceContext.ActiveTraceId);
            Assert.NotNull(TraceContext.GetTraceConfig());
            
            // Simulate grain work
            await Task.Delay(15);
            
            // Verify trace context is preserved
            Assert.NotNull(TraceContext.ActiveTraceId);
            Assert.NotNull(TraceContext.GetTraceConfig());
        });
    }

    private async Task SimulateHttpRequestFromOrleans()
    {
        // Simulate HTTP request from Orleans context
        await Task.Run(async () =>
        {
            // Verify trace context flows to HTTP context
            Assert.NotNull(TraceContext.ActiveTraceId);
            Assert.NotNull(TraceContext.GetTraceConfig());
            
            // Simulate HTTP processing
            await Task.Delay(15);
            
            // Verify trace context is preserved
            Assert.NotNull(TraceContext.ActiveTraceId);
            Assert.NotNull(TraceContext.GetTraceConfig());
        });
    }

    private async Task SimulateHttpPipelineProcessing()
    {
        // Simulate HTTP request pipeline with multiple middleware
        await Task.Run(async () =>
        {
            // Simulate authentication middleware
            await Task.Delay(5);
            Assert.NotNull(TraceContext.ActiveTraceId);
            
            // Simulate routing middleware
            await Task.Delay(5);
            Assert.NotNull(TraceContext.ActiveTraceId);
            
            // Simulate endpoint execution
            await Task.Delay(5);
            Assert.NotNull(TraceContext.ActiveTraceId);
            
            // Simulate response middleware
            await Task.Delay(5);
            Assert.NotNull(TraceContext.ActiveTraceId);
        });
    }

    private async Task SimulateMiddlewareChain()
    {
        // Simulate middleware chain execution
        await Task.Run(async () =>
        {
            // Simulate first middleware
            await Task.Delay(5);
            Assert.NotNull(TraceContext.ActiveTraceId);
            
            // Simulate second middleware
            await Task.Delay(5);
            Assert.NotNull(TraceContext.ActiveTraceId);
            
            // Simulate third middleware
            await Task.Delay(5);
            Assert.NotNull(TraceContext.ActiveTraceId);
        });
    }

    private async Task SimulateCrossBoundaryScenario()
    {
        // Simulate complex cross-boundary scenario
        await Task.Run(async () =>
        {
            // Simulate Orleans grain call
            await Task.Run(async () =>
            {
                // Verify trace context flows to grain
                Assert.NotNull(TraceContext.ActiveTraceId);
                
                // Simulate nested HTTP call from grain
                await Task.Run(async () =>
                {
                    // Verify trace context flows to HTTP
                    Assert.NotNull(TraceContext.ActiveTraceId);
                    
                    await Task.Delay(5);
                    
                    // Verify trace context is preserved
                    Assert.NotNull(TraceContext.ActiveTraceId);
                });
                
                // Verify trace context is preserved in grain
                Assert.NotNull(TraceContext.ActiveTraceId);
            });
            
            // Verify trace context is preserved in original context
            Assert.NotNull(TraceContext.ActiveTraceId);
        });
    }
}
