using System;
using System.Threading.Tasks;
using Aevatar.Core.Interception.Context;
using Aevatar.Core.Interception.Configurations;
using Aevatar.TestBase;
using Orleans;
using Xunit;
using Aevatar.Core.Interception.E2E.Tests.TestGrains;
using Aevatar.Core.Tests.Interception.Infrastructure;

namespace Aevatar.Core.Interception.E2E.Tests;

/// <summary>
/// E2E tests for Orleans trace filter integration using real Orleans cluster.
/// These tests verify that trace context is properly propagated through actual grain calls.
/// </summary>
[Collection("TraceContextTests")] // Ensure tests run sequentially to prevent static state interference
public class OrleansTraceFilterE2ETests : AevatarTestBase<AevatarCoreInterceptionE2ETestModule>, IClassFixture<TraceContextFixture>, IDisposable
{
    private readonly IClusterClient _clusterClient;
    private readonly IGrainFactory _grainFactory;
    private readonly TraceContextFixture _fixture;

    public OrleansTraceFilterE2ETests(TraceContextFixture fixture)
    {
        _fixture = fixture;
        _clusterClient = GetRequiredService<IClusterClient>();
        _grainFactory = GetRequiredService<IGrainFactory>();
        
        // CRITICAL: Ensure each test starts with a clean TraceContext state
        _fixture.ResetTraceContext();
    }

    public override void Dispose()
    {
        // CRITICAL: Clean up static TraceContext state to ensure test isolation
        // Since TraceContext is static, its state persists across tests and causes interference
        TraceContext.Clear();
        TraceContext.ActiveTraceId = null;
        
        base.Dispose();
    }

    [Fact]
    public async Task GrainCall_ShouldMaintainTraceContext()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var traceId = "e2e-grain-trace-123";
        var config = new TraceConfig { Enabled = true };
        
        // Set trace context before grain call
        TraceContext.ActiveTraceId = traceId;
        TraceContext.UpdateTraceConfig(config =>
        {
            config.Enabled = true;

            config.TrackedIds.Clear();
            config.AddTrackedId("test-trace-id");
        });

        // Act - Create and call a grain
        var grainId = Guid.NewGuid();
        var grain = _grainFactory.GetGrain<ITraceTestGrain>(grainId);
        
        var grainTraceId = await grain.GetTraceIdAsync();
        var grainConfig = await grain.GetTraceConfigAsync();

        // Assert - Grain should have access to the same trace context
        Assert.Equal(traceId, grainTraceId);
        Assert.NotNull(grainConfig);
        Assert.Equal(config.Enabled, grainConfig.Enabled);
    }

    [Fact]
    public async Task GrainToGrainCall_ShouldPropagateTraceContext()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var traceId = "e2e-grain-to-grain-trace";
        var config = new TraceConfig { Enabled = true };
        
        // Set trace context
        TraceContext.ActiveTraceId = traceId;
        TraceContext.UpdateTraceConfig(config =>
        {
            config.Enabled = true;

            config.TrackedIds.Clear();
            config.AddTrackedId("test-trace-id");
        });

        // Act - Create two grains and have one call the other
        var grain1Id = Guid.NewGuid();
        var grain2Id = Guid.NewGuid();
        
        var grain1 = _grainFactory.GetGrain<ITraceTestGrain>(grain1Id);
        var grain2 = _grainFactory.GetGrain<ITraceTestGrain>(grain2Id);

        // Grain1 calls Grain2
        var propagatedTraceId = await grain1.CallOtherGrainAsync(grain2Id);

        // Assert - Trace context should be propagated from Grain1 to Grain2
        Assert.Equal(traceId, propagatedTraceId);
        
        // Verify both grains have the same trace context
        var grain1TraceId = await grain1.GetTraceIdAsync();
        var grain2TraceId = await grain2.GetTraceIdAsync();
        
        Assert.Equal(traceId, grain1TraceId);
        Assert.Equal(traceId, grain2TraceId);
    }

    [Fact]
    public async Task MultipleConcurrentGrainCalls_ShouldMaintainSeparateTraceContexts()
    {
        _fixture.ResetTraceContext();

        // Arrange
        const int grainCount = 5;
        var results = new System.Collections.Concurrent.ConcurrentDictionary<int, string>();
        var tasks = new Task[grainCount];

        // Act - Start multiple concurrent grain calls with different trace contexts
        for (int i = 0; i < grainCount; i++)
        {
            var grainIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                var traceId = $"concurrent-grain-{grainIndex}-trace";
                
                // Set trace context for this task
                TraceContext.ActiveTraceId = traceId;
                
                // Create and call a grain
                var grainId = Guid.NewGuid();
                var grain = _grainFactory.GetGrain<ITraceTestGrain>(grainId);
                
                var grainTraceId = await grain.GetTraceIdAsync();
                results[grainIndex] = grainTraceId;
            });
        }

        // Wait for all grain calls to complete
        await Task.WhenAll(tasks);

        // Assert - Each grain call should have maintained its own trace context
        Assert.Equal(grainCount, results.Count);
        for (int i = 0; i < grainCount; i++)
        {
            Assert.Equal($"concurrent-grain-{i}-trace", results[i]);
        }
    }

    [Fact]
    public async Task GrainCallChain_ShouldPropagateTraceContextThroughMultipleHops()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var traceId = "e2e-chain-trace";
        var config = new TraceConfig { Enabled = true };
        
        // Set initial trace context
        TraceContext.ActiveTraceId = traceId;
        TraceContext.UpdateTraceConfig(config =>
        {
            config.Enabled = true;

            config.TrackedIds.Clear();
            config.AddTrackedId("test-trace-id");
        });

        // Act - Create a chain of grains: Grain1 -> Grain2 -> Grain3
        var grain1Id = Guid.NewGuid();
        var grain2Id = Guid.NewGuid();
        var grain3Id = Guid.NewGuid();
        
        var grain1 = _grainFactory.GetGrain<ITraceTestGrain>(grain1Id);
        var grain2 = _grainFactory.GetGrain<ITraceTestGrain>(grain2Id);
        var grain3 = _grainFactory.GetGrain<ITraceTestGrain>(grain3Id);

        // Chain: Grain1 calls Grain2, which calls Grain3
        var grain2TraceId = await grain1.CallOtherGrainAsync(grain2Id);
        var grain3TraceId = await grain2.CallOtherGrainAsync(grain3Id);

        // Assert - Trace context should be propagated through the entire chain
        Assert.Equal(traceId, grain2TraceId);
        Assert.Equal(traceId, grain3TraceId);
        
        // Verify all grains have the same trace context
        var grain1TraceId = await grain1.GetTraceIdAsync();
        var grain2DirectTraceId = await grain2.GetTraceIdAsync();
        var grain3DirectTraceId = await grain3.GetTraceIdAsync();
        
        Assert.Equal(traceId, grain1TraceId);
        Assert.Equal(traceId, grain2DirectTraceId);
        Assert.Equal(traceId, grain3DirectTraceId);
    }

    [Fact]
    public async Task GrainCall_ShouldHandleMissingTraceContext()
    {
        _fixture.ResetTraceContext();

        // Arrange - No trace context set
        TraceContext.Clear();

        // Act - Create and call a grain without trace context
        var grainId = Guid.NewGuid();
        var grain = _grainFactory.GetGrain<ITraceTestGrain>(grainId);
        
        var grainTraceId = await grain.GetTraceIdAsync();
        var grainConfig = await grain.GetTraceConfigAsync();

        // Assert - Grain should handle missing trace context gracefully
        Assert.Equal("no-trace-id", grainTraceId);
        Assert.NotNull(grainConfig);
        Assert.False(grainConfig.Enabled);
        Assert.Empty(grainConfig.TrackedIds);
    }

    [Fact]
    public async Task GrainCall_ShouldHandleEmptyTraceId()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var emptyTraceId = "";
        var config = new TraceConfig { Enabled = true };
        
        // Set empty trace ID
        TraceContext.ActiveTraceId = emptyTraceId;
        TraceContext.UpdateTraceConfig(config =>
        {
            config.Enabled = true;

            config.TrackedIds.Clear();
            config.AddTrackedId("test-trace-id");
        });

        // Act - Create and call a grain
        var grainId = Guid.NewGuid();
        var grain = _grainFactory.GetGrain<ITraceTestGrain>(grainId);
        
        var grainTraceId = await grain.GetTraceIdAsync();
        var grainConfig = await grain.GetTraceConfigAsync();

        // Assert - Grain should handle empty trace ID gracefully
        // Note: Empty trace ID gets converted to null internally, so grain returns "no-trace-id"
        Assert.Equal("no-trace-id", grainTraceId);
        Assert.NotNull(grainConfig);
        Assert.Equal(config.Enabled, grainConfig.Enabled);
    }
}
