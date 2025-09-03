using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Interception.Context;
using Aevatar.Core.Interception.Configurations;
using Aevatar.Core.Tests.Interception.Infrastructure;
using Xunit;

namespace Aevatar.Core.Tests.Interception.Unit;

/// <summary>
/// Tests for concurrent HTTP request scenarios and AsyncLocal isolation in TraceContext.
/// </summary>
[Collection("TraceContextTests")] // Ensure tests run sequentially to prevent static state interference
public class ConcurrentTraceContextTests : IClassFixture<TraceContextFixture>, IDisposable
{
    private readonly TraceContextFixture _fixture;

    public ConcurrentTraceContextTests(TraceContextFixture fixture)
    {
        _fixture = fixture;
        
        // CRITICAL: Ensure each test starts with a clean TraceContext state
        _fixture.ResetTraceContext();
    }

    public void Dispose()
    {
        
    }

    [Fact]
    public async Task ConcurrentHttpRequests_ShouldMaintainSeparateTraceContexts()
    {
        _fixture.ResetTraceContext();

        // Arrange
        const int requestCount = 10;
        var results = new ConcurrentDictionary<int, string?>();
        var tasks = new Task[requestCount];

        // Act - Start multiple concurrent "HTTP requests"
        for (int i = 0; i < requestCount; i++)
        {
            var requestId = i;
            tasks[i] = Task.Run(async () =>
            {
                var traceId = $"trace-{requestId:D3}";
                
                // Simulate HTTP request processing
                TraceContext.ActiveTraceId = traceId;
                
                // Simulate some async work
                await Task.Delay(10);
                
                // Verify trace context is preserved
                var preservedTraceId = TraceContext.ActiveTraceId;
                results[requestId] = preservedTraceId;
                
                // Simulate more async work
                await Task.Delay(10);
                
                // Verify trace context is still preserved
                var finalTraceId = TraceContext.ActiveTraceId;
                Assert.Equal(traceId, finalTraceId);
            });
        }

        // Wait for all requests to complete
        await Task.WhenAll(tasks);

        // Assert - Each request should have maintained its own trace context
        Assert.Equal(requestCount, results.Count);
        for (int i = 0; i < requestCount; i++)
        {
            Assert.Equal($"trace-{i:D3}", results[i]);
        }
    }

    [Fact]
    public async Task AsyncLocal_ShouldIsolateTraceContextsAcrossTasks()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var results = new ConcurrentDictionary<int, string?>();
        var tasks = new Task[5];

        // Act - Start multiple tasks with different trace contexts
        for (int i = 0; i < 5; i++)
        {
            var taskId = i;
            tasks[i] = Task.Run(async () =>
            {
                var traceId = $"async-trace-{taskId}";
                
                // Set trace context in this task
                TraceContext.ActiveTraceId = traceId;
                
                // Verify it's set correctly
                Assert.Equal(traceId, TraceContext.ActiveTraceId);
                
                // Simulate async work
                await Task.Delay(20);
                
                // Verify trace context is preserved across async boundaries
                var preservedTraceId = TraceContext.ActiveTraceId;
                results[taskId] = preservedTraceId;
                
                // Start a nested task
                await Task.Run(async () =>
                {
                    // Verify trace context flows to nested task
                    Assert.Equal(traceId, TraceContext.ActiveTraceId);
                    
                    await Task.Delay(10);
                    
                    // Verify trace context is still preserved
                    Assert.Equal(traceId, TraceContext.ActiveTraceId);
                });
            });
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert - Each task should have maintained its own trace context
        Assert.Equal(5, results.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal($"async-trace-{i}", results[i]);
        }
    }

    [Fact]
    public async Task ExecutionContext_ShouldFlowWithAsyncOperations()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var traceId = "execution-context-trace";
        var results = new ConcurrentQueue<string?>();

        // Act - Set trace context and perform nested async operations
        TraceContext.ActiveTraceId = traceId;

        // Verify initial context
        Assert.Equal(traceId, TraceContext.ActiveTraceId);

        // Perform nested async operations
        await Task.Run(async () =>
        {
            // Verify trace context flows to this task
            Assert.Equal(traceId, TraceContext.ActiveTraceId);
            results.Enqueue(TraceContext.ActiveTraceId);

            await Task.Run(async () =>
            {
                // Verify trace context flows to nested task
                Assert.Equal(traceId, TraceContext.ActiveTraceId);
                results.Enqueue(TraceContext.ActiveTraceId);

                await Task.Delay(10);

                // Verify trace context is still preserved
                Assert.Equal(traceId, TraceContext.ActiveTraceId);
                results.Enqueue(TraceContext.ActiveTraceId);
            });

            // Verify trace context is preserved after nested task
            Assert.Equal(traceId, TraceContext.ActiveTraceId);
            results.Enqueue(TraceContext.ActiveTraceId);
        });

        // Verify trace context is preserved in original context
        Assert.Equal(traceId, TraceContext.ActiveTraceId);
        results.Enqueue(TraceContext.ActiveTraceId);

        // Assert - All async operations should have preserved the trace context
        Assert.Equal(5, results.Count);
        foreach (var result in results)
        {
            Assert.Equal(traceId, result);
        }
    }

    [Fact]
    public async Task ConcurrentTraceConfig_ShouldShareGlobalState()
    {
        _fixture.ResetTraceContext();

        // Arrange
        const int taskCount = 5;
        var configUpdates = new ConcurrentQueue<bool>();
        var finalConfigs = new ConcurrentQueue<bool>();
        var tasks = new Task[taskCount];

        // Act - Start multiple tasks that concurrently update trace configuration
        for (int i = 0; i < taskCount; i++)
        {
            var taskId = i;
            tasks[i] = Task.Run(async () =>
            {
                // Each task tries to set config to enabled/disabled alternately
                var shouldEnable = taskId % 2 == 0;
                
                TraceContext.UpdateTraceConfig(config => 
                {
                    config.Enabled = shouldEnable;
                });
                
                configUpdates.Enqueue(shouldEnable);
                
                // Simulate some work
                await Task.Delay(10);
                
                // Check final config (will be whatever the last task set)
                var finalConfig = TraceContext.GetTraceConfig();
                finalConfigs.Enqueue(finalConfig?.Enabled ?? false);
            });
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert - All tasks should have completed and shared the same final state
        Assert.Equal(taskCount, configUpdates.Count);
        Assert.Equal(taskCount, finalConfigs.Count);
        
        // The final configuration should be consistent across all tasks
        var finalConfigStates = finalConfigs.ToArray();
        var lastConfigValue = finalConfigStates[0];
        
        // All tasks should see the same final config state (since it's shared)
        Assert.All(finalConfigStates, config => Assert.Equal(lastConfigValue, config));
        
        // Verify that when tracing is disabled, it affects all operations
        TraceContext.UpdateTraceConfig(config => config.Enabled = false);
        Assert.False(TraceContext.IsTracingEnabled);
        
        // Add a tracked ID and explicitly enable tracing
        TraceContext.AddTrackedId("test-id");
        TraceContext.EnableTracing("test-id"); // Explicitly enable tracing
        Assert.True(TraceContext.IsTracingEnabled);
    }

    [Fact]
    public async Task ConcurrentTrackedIds_ShouldAccumulateCorrectly()
    {
        _fixture.ClearTraceContext(); // Use clean reset without default trace IDs
        
        // Arrange
        const int taskCount = 10;
        var trackedIdsToAdd = new List<string>();
        var addResults = new ConcurrentBag<bool>();
        var tasks = new Task[taskCount];

        // Generate unique trace IDs for each task
        for (int i = 0; i < taskCount; i++)
        {
            trackedIdsToAdd.Add($"concurrent-trace-{i:D2}");
        }

        // Act - Start multiple tasks that concurrently add tracked IDs
        for (int i = 0; i < taskCount; i++)
        {
            var taskId = i;
            var traceId = trackedIdsToAdd[i];
            
            tasks[i] = Task.Run(async () =>
            {
                // Each task adds a unique traced ID
                var addSuccess = TraceContext.AddTrackedId(traceId);
                addResults.Add(addSuccess);
                
                // Simulate some work
                await Task.Delay(5);
            });
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert - All traced IDs should be accumulated correctly
        var finalTrackedIds = TraceContext.GetTrackedIds();
        
        // All add operations should have succeeded (no duplicates)
        Assert.Equal(taskCount, addResults.Count);
        Assert.All(addResults, result => Assert.True(result));
        
        // Final tracked IDs should contain all added IDs
        Assert.Equal(taskCount, finalTrackedIds.Count);
        
        // Verify each trace ID was added
        foreach (var expectedId in trackedIdsToAdd)
        {
            Assert.Contains(expectedId, finalTrackedIds);
        }
    }

    [Fact]
    public async Task ConcurrentTracingDisable_CanBeReEnabledByTrackedIds()
    {
        _fixture.ClearTraceContext(); // Use clean reset for controlled ID management
        
        // Arrange
        const int taskCount = 5;
        var tracingStates = new ConcurrentBag<bool>();
        var tasks = new Task[taskCount + 1]; // +1 for the disabling task

        // Enable tracing initially and add some tracked IDs
        TraceContext.UpdateTraceConfig(config => config.Enabled = true);
        TraceContext.AddTrackedId("initial-trace-1");
        TraceContext.AddTrackedId("initial-trace-2");
        
        // Act - Start multiple tasks that check tracing state
        for (int i = 0; i < taskCount; i++)
        {
            var taskId = i;
            tasks[i] = Task.Run(async () =>
            {
                // Add a trace ID
                TraceContext.AddTrackedId($"task-trace-{taskId}");
                
                // Simulate some work
                await Task.Delay(20);
                
                // Check if tracing is still enabled (might be disabled by concurrent task)
                var isEnabled = TraceContext.IsTracingEnabled;
                tracingStates.Add(isEnabled);
            });
        }

        // Start a task that disables tracing concurrently
        tasks[taskCount] = Task.Run(async () =>
        {
            await Task.Delay(10); // Let other tasks start first
            
            // Disable tracing - this should affect all other operations
            TraceContext.UpdateTraceConfig(config => config.Enabled = false);
        });

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert - Verify final state
        var finalTrackedIds = TraceContext.GetTrackedIds();
        var finalTracingEnabled = TraceContext.IsTracingEnabled;
        
        // Tracing should be disabled at the end
        Assert.False(finalTracingEnabled);
        
        // All tracked IDs should still be present (disabling doesn't remove them)
        Assert.True(finalTrackedIds.Count >= 2); // At least the initial 2 IDs
        Assert.Contains("initial-trace-1", finalTrackedIds);
        Assert.Contains("initial-trace-2", finalTrackedIds);
        
        // Even with tracked IDs present, tracing should be disabled (disabled by config)
        Assert.False(TraceContext.IsTracingEnabled);
        
        // Verify that enabling tracing explicitly works after disable
        TraceContext.AddTrackedId("post-disable-trace");
        TraceContext.EnableTracing("post-disable-trace"); // Explicitly enable tracing
        Assert.True(TraceContext.IsTracingEnabled);
    }

    [Fact]
    public void AddTrackedId_ShouldOnlyAddToTracking()
    {
        _fixture.ResetTraceContext();
        
        // Arrange - Start with tracing disabled
        TraceContext.UpdateTraceConfig(config => config.Enabled = false);
        Assert.False(TraceContext.IsTracingEnabled);
        
        // Act - Add a tracked ID (should not enable tracing)
        var result = TraceContext.AddTrackedId("tracking-test");
        TraceContext.ActiveTraceId = "tracking-test"; // Set active trace ID to match tracked ID
        
        // Assert - AddTrackedId should succeed but not enable tracing
        Assert.True(result); // AddTrackedId should succeed
        Assert.False(TraceContext.IsTracingEnabled); // Tracing should still be disabled
        
        // Verify the tracked ID is present
        var trackedIds = TraceContext.GetTrackedIds();
        Assert.Contains("tracking-test", trackedIds);
        
        // Now explicitly enable tracing and verify it works
        TraceContext.EnableTracing("tracking-test");
        Assert.True(TraceContext.IsTracingEnabled); // Now tracing should be enabled
        
        // Add another ID while tracing is enabled
        TraceContext.AddTrackedId("another-test");
        TraceContext.ActiveTraceId = "another-test"; // Set active trace ID to match tracked ID
        Assert.True(TraceContext.IsTracingEnabled); // Should still be enabled
    }

    [Fact]
    public async Task ThreadPoolReuse_ShouldNotAffectTraceContext()
    {
        _fixture.ResetTraceContext();

        // Arrange
        var results = new ConcurrentDictionary<int, string?>();
        var tasks = new Task[8];

        // Act - Start tasks that will likely reuse threads from the pool
        for (int i = 0; i < 8; i++)
        {
            var taskId = i;
            tasks[i] = Task.Run(async () =>
            {
                var traceId = $"threadpool-trace-{taskId}";
                
                // Set trace context
                TraceContext.ActiveTraceId = traceId;
                
                // Verify it's set correctly
                Assert.Equal(traceId, TraceContext.ActiveTraceId);
                
                // Simulate work that might cause thread pool reuse
                await Task.Delay(5);
                
                // Verify trace context is preserved
                var preservedTraceId = TraceContext.ActiveTraceId;
                results[taskId] = preservedTraceId;
            });
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert - Each task should have maintained its own trace context
        Assert.Equal(8, results.Count);
        for (int i = 0; i < 8; i++)
        {
            Assert.Equal($"threadpool-trace-{i}", results[i]);
        }
    }

    [Fact]
    public async Task GlobalTracingDisable_CanBeReEnabledByAddingTrackedIds()
    {
        _fixture.ClearTraceContext(); // Use clean reset for exact counting
        
        // Arrange
        const int taskCount = 8;
        var tracingResults = new ConcurrentBag<(int taskId, bool wasTracingEnabled, int trackedIdCount)>();
        var tasks = new Task[taskCount];

        // Initially enable tracing and add some base tracked IDs
        TraceContext.UpdateTraceConfig(config => config.Enabled = true);
        TraceContext.AddTrackedId("base-trace-1");
        TraceContext.AddTrackedId("base-trace-2");

        // Act - Start multiple tasks that add tracked IDs and check tracing state
        for (int i = 0; i < taskCount; i++)
        {
            var taskId = i;
            tasks[i] = Task.Run(async () =>
            {
                // Each task adds multiple trace IDs
                TraceContext.AddTrackedId($"task-{taskId}-trace-A");
                TraceContext.AddTrackedId($"task-{taskId}-trace-B");
                
                // Task 4 will disable tracing halfway through
                if (taskId == 4)
                {
                    await Task.Delay(15); // Let other tasks add their IDs first
                    TraceContext.UpdateTraceConfig(config => config.Enabled = false);
                }
                else
                {
                    await Task.Delay(30); // Other tasks check state after task 4 disables
                }
                
                // Record the final state each task observes
                var isEnabled = TraceContext.IsTracingEnabled;
                var trackedIds = TraceContext.GetTrackedIds();
                tracingResults.Add((taskId, isEnabled, trackedIds.Count));
            });
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);

        // Assert - Verify the global disable behavior
        var finalResults = tracingResults.ToArray();
        var finalTrackedIds = TraceContext.GetTrackedIds();
        var finalTracingEnabled = TraceContext.IsTracingEnabled;

        // All tasks should have completed
        Assert.Equal(taskCount, finalResults.Length);
        
        // Final tracing should be disabled globally
        Assert.False(finalTracingEnabled);
        
        // All tracked IDs should be preserved (disabling doesn't remove them)
        var expectedMinimumCount = 2 + (taskCount * 2); // base IDs + task IDs
        Assert.True(finalTrackedIds.Count >= expectedMinimumCount);
        
        // Verify base IDs are still present
        Assert.Contains("base-trace-1", finalTrackedIds);
        Assert.Contains("base-trace-2", finalTrackedIds);
        
        // Verify task IDs are present
        for (int i = 0; i < taskCount; i++)
        {
            Assert.Contains($"task-{i}-trace-A", finalTrackedIds);
            Assert.Contains($"task-{i}-trace-B", finalTrackedIds);
        }
        
        // After task 4 disables tracing, it should be disabled initially
        Assert.False(TraceContext.IsTracingEnabled);
        Assert.True(finalTrackedIds.Count > 0); // IDs exist
        
        // Verify that setting active trace ID doesn't automatically enable tracing when disabled
        var originalActiveTrace = TraceContext.ActiveTraceId;
        TraceContext.ActiveTraceId = "manual-active-trace";
        // Tracing state depends on config and tracked IDs, not just active trace
        
        // Adding tracked IDs and explicitly enabling should re-enable tracing
        var addResult = TraceContext.AddTrackedId("post-disable-should-enable");
        Assert.True(addResult); // Adding succeeds
        TraceContext.EnableTracing("post-disable-should-enable"); // Explicitly enable tracing
        Assert.True(TraceContext.IsTracingEnabled); // And tracing is re-enabled
    }
}
