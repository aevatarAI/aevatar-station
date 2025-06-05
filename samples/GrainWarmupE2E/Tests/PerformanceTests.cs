using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using GrainWarmupE2E.Fixtures;
using E2E.Grains;
using GrainWarmupE2E.Utilities;
using System.Diagnostics;

namespace GrainWarmupE2E.Tests;

/// <summary>
/// Basic performance tests for grain system (testing against real silo)
/// </summary>
public class PerformanceTests : IClassFixture<GrainWarmupTestFixture>, IAsyncLifetime
{
    private readonly GrainWarmupTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public PerformanceTests(GrainWarmupTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _output.WriteLine("Initializing performance test fixture...");
        await _fixture.InitializeAsync();
        _output.WriteLine("Performance test fixture initialized successfully");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ActivationLatencyBaseline_ShouldMeasureGrainActivationPerformance()
    {
        // Arrange
        var grainCount = 20;
        var grainIds = GrainWarmupTestFixture.GenerateTestGrainIds(grainCount, "perf-baseline");
        var latencies = new List<double>();

        // Act - Measure activation latencies
        foreach (var grainId in grainIds)
        {
            var grain = _fixture.GetTestGrain(grainId);
            var sw = Stopwatch.StartNew();
            var result = await grain.PingAsync();
            sw.Stop();
            
            latencies.Add(sw.Elapsed.TotalMilliseconds);
            result.Should().Contain("Pong");
        }

        // Assert
        var avgLatency = latencies.Average();
        var p95Latency = latencies.OrderBy(x => x).Skip((int)(latencies.Count * 0.95)).FirstOrDefault();
        
        avgLatency.Should().BeLessThan(1000); // Less than 1 second average
        p95Latency.Should().BeLessThan(2000); // Less than 2 seconds P95
        
        _output.WriteLine($"Grain Activation Performance:");
        _output.WriteLine($"  Count: {grainCount}");
        _output.WriteLine($"  Average Latency: {avgLatency:F2}ms");
        _output.WriteLine($"  P95 Latency: {p95Latency:F2}ms");
        _output.WriteLine($"  Min Latency: {latencies.Min():F2}ms");
        _output.WriteLine($"  Max Latency: {latencies.Max():F2}ms");
    }

    [Fact]
    public async Task ConcurrentAccessPerformance_ShouldHandleParallelRequests()
    {
        // Arrange
        var grainCount = 10;
        var requestsPerGrain = 5;
        var grainIds = GrainWarmupTestFixture.GenerateTestGrainIds(grainCount, "perf-concurrent");
        
        // Act - Perform concurrent requests
        var sw = Stopwatch.StartNew();
        var tasks = grainIds.SelectMany(grainId =>
            Enumerable.Range(0, requestsPerGrain).Select(async _ =>
            {
                var grain = _fixture.GetTestGrain(grainId);
                return await grain.PingAsync();
            })).ToArray();

        var results = await Task.WhenAll(tasks);
        sw.Stop();

        // Assert
        var totalOperations = grainCount * requestsPerGrain;
        var throughput = totalOperations / sw.Elapsed.TotalSeconds;
        
        results.Should().HaveCount(totalOperations);
        results.Should().AllSatisfy(result => result.Should().Contain("Pong"));
        throughput.Should().BeGreaterThan(1); // At least 1 operation per second
        
        _output.WriteLine($"Concurrent Access Performance:");
        _output.WriteLine($"  Total Operations: {totalOperations}");
        _output.WriteLine($"  Total Duration: {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"  Throughput: {throughput:F2} ops/sec");
        _output.WriteLine($"  Average per Operation: {sw.Elapsed.TotalMilliseconds / totalOperations:F2}ms");
    }

    [Fact]
    public async Task ComputePerformance_ShouldMeasureGrainComputationLatency()
    {
        // Arrange
        var grainCount = 10;
        var computeInput = 1000; // Larger computation
        var grainIds = GrainWarmupTestFixture.GenerateTestGrainIds(grainCount, "perf-compute");
        var latencies = new List<double>();

        // Act - Measure computation latencies
        foreach (var grainId in grainIds)
        {
            var grain = _fixture.GetTestGrain(grainId);
            var sw = Stopwatch.StartNew();
            var result = await grain.ComputeAsync(computeInput);
            sw.Stop();
            
            latencies.Add(sw.Elapsed.TotalMilliseconds);
            result.Should().BeGreaterThan(computeInput);
        }

        // Assert
        var avgLatency = latencies.Average();
        var maxLatency = latencies.Max();
        
        avgLatency.Should().BeLessThan(500); // Less than 500ms average
        maxLatency.Should().BeLessThan(1000); // Less than 1 second max
        
        _output.WriteLine($"Compute Performance:");
        _output.WriteLine($"  Grains: {grainCount}, Input: {computeInput}");
        _output.WriteLine($"  Average Latency: {avgLatency:F2}ms");
        _output.WriteLine($"  Max Latency: {maxLatency:F2}ms");
        _output.WriteLine($"  Min Latency: {latencies.Min():F2}ms");
    }

    [Fact]
    public async Task DatabaseSimulationPerformance_ShouldMeasureIOLatency()
    {
        // Arrange
        var grainCount = 5;
        var delayMs = 100; // Simulate DB delay
        var grainIds = GrainWarmupTestFixture.GenerateTestGrainIds(grainCount, "perf-db");
        var latencies = new List<double>();

        // Act - Measure database simulation latencies
        foreach (var grainId in grainIds)
        {
            var grain = _fixture.GetTestGrain(grainId);
            var sw = Stopwatch.StartNew();
            var result = await grain.SimulateDatabaseOperationAsync(delayMs);
            sw.Stop();
            
            latencies.Add(sw.Elapsed.TotalMilliseconds);
            result.Should().Contain("Database operation completed");
        }

        // Assert
        var avgLatency = latencies.Average();
        var overhead = avgLatency - delayMs;
        
        // Should be close to the simulated delay plus some overhead
        avgLatency.Should().BeGreaterThan(delayMs);
        avgLatency.Should().BeLessThan(delayMs + 200); // Max 200ms overhead
        overhead.Should().BeLessThan(100); // Max 100ms framework overhead
        
        _output.WriteLine($"Database Simulation Performance:");
        _output.WriteLine($"  Grains: {grainCount}, Simulated Delay: {delayMs}ms");
        _output.WriteLine($"  Average Total Latency: {avgLatency:F2}ms");
        _output.WriteLine($"  Average Overhead: {overhead:F2}ms");
        _output.WriteLine($"  Overhead Percentage: {(overhead / delayMs):P1}");
    }

    [Fact]
    public async Task AccessCountTracking_ShouldMeasureStateUpdatePerformance()
    {
        // Arrange
        var grainId = TestDataGenerator.GenerateTestGuid(1);
        var grain = _fixture.GetTestGrain(grainId);
        var accessCount = 10;
        var latencies = new List<double>();

        // Act - Measure repeated access performance
        for (int i = 0; i < accessCount; i++)
        {
            var sw = Stopwatch.StartNew();
            await grain.PingAsync();
            var count = await grain.GetAccessCountAsync();
            sw.Stop();
            
            latencies.Add(sw.Elapsed.TotalMilliseconds);
            count.Should().BeGreaterThan(i);
        }

        // Assert
        var avgLatency = latencies.Average();
        var finalCount = await grain.GetAccessCountAsync();
        
        avgLatency.Should().BeLessThan(100); // Less than 100ms average for state updates
        finalCount.Should().BeGreaterOrEqualTo(accessCount);
        
        _output.WriteLine($"Access Count Tracking Performance:");
        _output.WriteLine($"  Access Count: {accessCount}");
        _output.WriteLine($"  Final Count: {finalCount}");
        _output.WriteLine($"  Average Latency per Access: {avgLatency:F2}ms");
        _output.WriteLine($"  Total Operations: {accessCount * 2}"); // Ping + GetAccessCount
    }
} 