using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Aevatar.Silo.GrainWarmup;
using Aevatar.Silo.Tests.GrainWarmup.Fixtures;
using Aevatar.Silo.Tests.GrainWarmup.TestGrains;

namespace Aevatar.Silo.Tests.GrainWarmup.Tests;

/// <summary>
/// Basic functionality tests for grain warmup system integrated with Aevatar.Silo.Tests infrastructure
/// </summary>
[Collection("GrainWarmup")]
public class BasicFunctionalityTests : IAsyncLifetime
{
    private readonly GrainWarmupTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public BasicFunctionalityTests(GrainWarmupTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ShouldConnectToSilo()
    {
        // Act & Assert
        var client = _fixture.Cluster.Client;
        client.ShouldNotBeNull();
        
        // Verify we can access grain factory
        var grainFactory = _fixture.Cluster.GrainFactory;
        grainFactory.ShouldNotBeNull();
        
        _output.WriteLine("Successfully verified silo connection");
    }

    [Fact]
    public async Task ShouldActivateGrain()
    {
        // Arrange
        var grainId = Guid.NewGuid();
        var grain = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupGrain>(grainId);

        // Act
        var result = await grain.PingAsync();

        // Assert
        result.ShouldContain("Pong");
        result.ShouldContain(grainId.ToString());
        _output.WriteLine($"Successfully activated grain {grainId} and received: {result}");
    }

    [Fact]
    public async Task ShouldActivateMultipleGrains()
    {
        // Arrange
        var grainIds = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToList();
        
        // Act
        var tasks = grainIds.Select(async grainId =>
        {
            var grain = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupGrain>(grainId);
            return await grain.PingAsync();
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldAllBe(result => result.Contains("Pong"));
        results.Length.ShouldBe(5);
        
        _output.WriteLine($"Successfully activated {results.Length} grains");
    }

    [Fact]
    public async Task ShouldTrackGrainActivationTime()
    {
        // Arrange - Use fresh GUID to ensure new grain activation
        var grainId = Guid.NewGuid();
        var grain = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupGrain>(grainId);

        // Act - First activation and get activation time
        var result = await grain.PingAsync();
        var activationTime = await grain.GetActivationTimeAsync();

        // Assert
        result.ShouldContain("Pong");
        activationTime.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        
        _output.WriteLine($"Grain {grainId} activated at: {activationTime}");
    }

    [Fact]
    public async Task ShouldTrackAccessCount()
    {
        // Arrange
        var grainId = Guid.NewGuid();
        var grain = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupGrain>(grainId);

        // Act
        await grain.PingAsync(); // First call
        await grain.PingAsync(); // Second call
        var accessCount = await grain.GetAccessCountAsync();

        // Assert
        accessCount.ShouldBeGreaterThanOrEqualTo(2);
        _output.WriteLine($"Grain {grainId} access count: {accessCount}");
    }

    [Fact]
    public async Task ShouldPerformComputation()
    {
        // Arrange
        var grainId = Guid.NewGuid();
        var grain = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupGrain>(grainId);
        var input = 42;

        // Act
        var result = await grain.ComputeAsync(input);

        // Assert
        result.ShouldBeGreaterThan(input);
        _output.WriteLine($"Computation result for {input}: {result}");
    }

    [Fact]
    public async Task ShouldSimulateDatabaseOperations()
    {
        // Arrange
        var grainId = Guid.NewGuid();
        var grain = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupGrain>(grainId);
        var delayMs = 50;

        // Act
        var startTime = DateTime.UtcNow;
        var result = await grain.SimulateDatabaseOperationAsync(delayMs);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        result.ShouldContain("Database operation completed");
        result.ShouldContain(grainId.ToString());
        elapsed.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(delayMs - 10)); // Allow some tolerance
        
        _output.WriteLine($"Database simulation completed in {elapsed.TotalMilliseconds:F2}ms: {result}");
    }

    [Fact]
    public async Task ShouldProvideGrainMetadata()
    {
        // Arrange - Use fresh GUID to ensure new grain activation
        var grainId = Guid.NewGuid();
        var grain = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupGrain>(grainId);

        // Act
        await grain.PingAsync(); // Activate the grain
        var metadata = await grain.GetMetadataAsync();

        // Assert
        metadata.ShouldNotBeNull();
        metadata.GrainId.ShouldBe(grainId);
        metadata.ActivationTime.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        metadata.AccessCount.ShouldBeGreaterThan(0);
        metadata.SiloAddress.ShouldNotBeNullOrEmpty();
        
        _output.WriteLine($"Grain metadata: Id={metadata.GrainId}, " +
                         $"ActivationTime={metadata.ActivationTime}, " +
                         $"AccessCount={metadata.AccessCount}, " +
                         $"SiloAddress={metadata.SiloAddress}");
    }

    [Fact]
    public async Task ShouldVerifyGrainWarmupServiceIsRegistered()
    {
        // Arrange - Get the primary silo's service provider
        var primarySilo = _fixture.Cluster.Primary;
        var services = _fixture.Cluster.GetSiloServiceProvider(primarySilo.SiloAddress);

        // Act & Assert
        var grainWarmupService = services.GetService<IGrainWarmupService>();
        grainWarmupService.ShouldNotBeNull("Grain warmup service should be registered");
        
        var grainWarmupOrchestrator = services.GetService<IGrainWarmupOrchestrator<Guid>>();
        grainWarmupOrchestrator.ShouldNotBeNull("Grain warmup orchestrator should be registered");
        
        _output.WriteLine("Successfully verified grain warmup services are registered");
    }
} 