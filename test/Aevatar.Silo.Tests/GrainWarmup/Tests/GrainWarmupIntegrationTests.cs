using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;
using Aevatar.Silo.GrainWarmup;
using Aevatar.Silo.Tests.GrainWarmup.Fixtures;
using Aevatar.Silo.Tests.GrainWarmup.TestGrains;

namespace Aevatar.Silo.Tests.GrainWarmup.Tests;

/// <summary>
/// Integration tests for grain warmup functionality
/// </summary>
[Collection("GrainWarmup")]
public class GrainWarmupIntegrationTests : IAsyncLifetime
{
    private readonly GrainWarmupTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public GrainWarmupIntegrationTests(GrainWarmupTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ShouldRegisterGrainWarmupServices()
    {
        // Arrange - Get the primary silo's service provider
        var primarySilo = _fixture.Cluster.Primary;
        var services = _fixture.Cluster.GetSiloServiceProvider(primarySilo.SiloAddress);

        // Act & Assert
        var grainWarmupService = services.GetService<IGrainWarmupService>();
        grainWarmupService.ShouldNotBeNull("IGrainWarmupService should be registered");

        var grainWarmupOrchestrator = services.GetService<IGrainWarmupOrchestrator<Guid>>();
        grainWarmupOrchestrator.ShouldNotBeNull("IGrainWarmupOrchestrator<Guid> should be registered");

        var grainDiscoveryService = services.GetRequiredService<IGrainDiscoveryService>();
        grainDiscoveryService.ShouldNotBeNull("IGrainDiscoveryService should be registered");

        _output.WriteLine("All grain warmup services are properly registered");
    }

    [Fact]
    public async Task ShouldDiscoverTestGrainTypes()
    {
        // Arrange - Get the primary silo's service provider
        var primarySilo = _fixture.Cluster.Primary;
        var services = _fixture.Cluster.GetSiloServiceProvider(primarySilo.SiloAddress);
        var grainDiscoveryService = services.GetRequiredService<IGrainDiscoveryService>();

        // Act
        var discoveredTypes = grainDiscoveryService.DiscoverWarmupEligibleGrainTypes().ToList();

        // Assert
        discoveredTypes.ShouldNotBeEmpty("Should discover at least some grain types");
        discoveredTypes.ShouldContain(t => t.Name.Contains("TestWarmupGrain"), 
            "Should discover our test grain type");

        _output.WriteLine($"Discovered {discoveredTypes.Count} grain types:");
        foreach (var grainType in discoveredTypes.Take(10)) // Show first 10
        {
            _output.WriteLine($"  - {grainType.Name}");
        }
    }

    [Fact]
    public async Task ShouldActivateGrainsInBatches()
    {
        // Arrange
        var grainIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();
        var batchSize = 3;
        var activatedGrains = new List<ITestWarmupGrain>();

        // Act
        for (int i = 0; i < grainIds.Count; i += batchSize)
        {
            var batch = grainIds.Skip(i).Take(batchSize);
            var batchTasks = batch.Select(async grainId =>
            {
                var grain = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupGrain>(grainId);
                await grain.PingAsync(); // Activate the grain
                return grain;
            });

            var batchResults = await Task.WhenAll(batchTasks);
            activatedGrains.AddRange(batchResults);

            _output.WriteLine($"Activated batch {(i / batchSize) + 1} with {batchResults.Length} grains");
            
            // Small delay between batches to simulate warmup behavior
            await Task.Delay(100);
        }

        // Assert
        activatedGrains.Count.ShouldBe(grainIds.Count);

        // Verify all grains are actually activated by checking their access count
        foreach (var grain in activatedGrains)
        {
            var accessCount = await grain.GetAccessCountAsync();
            accessCount.ShouldBeGreaterThan(0, "Each grain should have been accessed at least once");
        }

        _output.WriteLine($"Successfully activated {activatedGrains.Count} grains in batches of {batchSize}");
    }

    [Fact]
    public async Task ShouldHandleConcurrentGrainActivation()
    {
        // Arrange
        var grainIds = Enumerable.Range(1, 20).Select(_ => Guid.NewGuid()).ToList();
        var maxConcurrency = 5;

        // Act
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = grainIds.Select(async grainId =>
        {
            await semaphore.WaitAsync();
            try
            {
                var grain = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupGrain>(grainId);
                var result = await grain.PingAsync();
                return new { GrainId = grainId, Result = result, Timestamp = DateTime.UtcNow };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldAllBe(result => result.Result.Contains("Pong"));
        results.Length.ShouldBe(grainIds.Count);

        // Verify timing - all should complete within reasonable time
        var minTime = results.Min(r => r.Timestamp);
        var maxTime = results.Max(r => r.Timestamp);
        var totalDuration = maxTime - minTime;

        totalDuration.ShouldBeLessThan(TimeSpan.FromSeconds(30), 
            "Concurrent activation should complete within reasonable time");

        _output.WriteLine($"Successfully activated {results.Length} grains concurrently " +
                         $"(max concurrency: {maxConcurrency}) in {totalDuration.TotalMilliseconds:F2}ms");
    }

    [Fact]
    public async Task ShouldMeasureGrainActivationPerformance()
    {
        // Arrange
        var grainCount = 50;
        var grainIds = Enumerable.Range(1, grainCount).Select(_ => Guid.NewGuid()).ToList();

        // Act
        var startTime = DateTime.UtcNow;
        
        var tasks = grainIds.Select(async grainId =>
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupGrain>(grainId);
            var activationTime = DateTime.UtcNow;
            await grain.PingAsync();
            return new { GrainId = grainId, ActivationTime = activationTime };
        });

        var results = await Task.WhenAll(tasks);
        var endTime = DateTime.UtcNow;

        // Assert
        var totalDuration = endTime - startTime;
        var averageActivationTime = totalDuration.TotalMilliseconds / grainCount;

        results.Length.ShouldBe(grainCount);
        totalDuration.ShouldBeLessThan(TimeSpan.FromMinutes(1), 
            "Should activate all grains within reasonable time");

        _output.WriteLine($"Performance Results:");
        _output.WriteLine($"  - Total grains activated: {grainCount}");
        _output.WriteLine($"  - Total duration: {totalDuration.TotalMilliseconds:F2}ms");
        _output.WriteLine($"  - Average activation time: {averageActivationTime:F2}ms per grain");
        _output.WriteLine($"  - Throughput: {grainCount / totalDuration.TotalSeconds:F2} grains/second");
    }

    [Fact]
    public async Task ShouldVerifyGrainWarmupServiceIsHostedService()
    {
        // Arrange - Get the primary silo's service provider
        var primarySilo = _fixture.Cluster.Primary;
        var services = _fixture.Cluster.GetSiloServiceProvider(primarySilo.SiloAddress);

        // Act
        var hostedServices = services.GetServices<IHostedService>();
        var grainWarmupHostedService = hostedServices.OfType<IGrainWarmupService>().FirstOrDefault();

        // Assert
        grainWarmupHostedService.ShouldNotBeNull("GrainWarmupService should be registered as IHostedService");
        
        _output.WriteLine("GrainWarmupService is properly registered as a hosted service");
    }
} 