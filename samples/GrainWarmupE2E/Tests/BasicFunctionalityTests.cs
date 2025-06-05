using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Aevatar.Silo.GrainWarmup;
using Aevatar.Silo.GrainWarmup.Extensions;
using GrainWarmupE2E.Fixtures;
using E2E.Grains;
using GrainWarmupE2E.Utilities;

namespace GrainWarmupE2E.Tests;

/// <summary>
/// Basic functionality tests for grain system (testing against real silo)
/// </summary>
public class BasicFunctionalityTests : IClassFixture<GrainWarmupTestFixture>, IAsyncLifetime
{
    private readonly GrainWarmupTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public BasicFunctionalityTests(GrainWarmupTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _output.WriteLine("Initializing test fixture...");
        await _fixture.InitializeAsync();
        _output.WriteLine("Test fixture initialized successfully");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ShouldConnectToSilo()
    {
        // Act & Assert
        var client = _fixture.Client;
        client.Should().NotBeNull();
        
        // Verify we can access grain factory
        var grainFactory = _fixture.GrainFactory;
        grainFactory.Should().NotBeNull();
        
        _output.WriteLine("Successfully verified silo connection");
    }

    [Fact]
    public async Task ShouldActivateGrain()
    {
        // Arrange
        var grainId = TestDataGenerator.GenerateTestGuid(1);
        var grain = _fixture.GetTestGrain(grainId);

        // Act
        var result = await grain.PingAsync();

        // Assert
        result.Should().Contain("Pong");
        result.Should().Contain(grainId.ToString());
        _output.WriteLine($"Successfully activated grain {grainId} and received: {result}");
    }

    [Fact]
    public async Task ShouldActivateMultipleGrains()
    {
        // Arrange
        var grainIds = GrainWarmupTestFixture.GenerateTestGrainIds(5, "basic");
        
        // Act
        var tasks = grainIds.Select(async grainId =>
        {
            var grain = _fixture.GetTestGrain(grainId);
            return await grain.PingAsync();
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(result => result.Should().Contain("Pong"));
        results.Should().HaveCount(5);
        
        _output.WriteLine($"Successfully activated {results.Length} grains");
    }

    [Fact]
    public async Task ShouldTrackGrainActivationTime()
    {
        // Arrange - Use fresh GUID to ensure new grain activation
        var grainId = Guid.NewGuid();
        var grain = _fixture.GetTestGrain(grainId);

        // Act - First activation and get activation time
        var result = await grain.PingAsync();
        var activationTime = await grain.GetActivationTimeAsync();

        // Assert
        result.Should().Contain("Pong");
        activationTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        _output.WriteLine($"Grain {grainId} activated at: {activationTime}");
    }

    [Fact]
    public async Task ShouldTrackAccessCount()
    {
        // Arrange
        var grainId = TestDataGenerator.GenerateTestGuid(3);
        var grain = _fixture.GetTestGrain(grainId);

        // Act
        await grain.PingAsync(); // First call
        await grain.PingAsync(); // Second call
        var accessCount = await grain.GetAccessCountAsync();

        // Assert
        accessCount.Should().BeGreaterOrEqualTo(2);
        _output.WriteLine($"Grain {grainId} access count: {accessCount}");
    }

    [Fact]
    public async Task ShouldPerformComputation()
    {
        // Arrange
        var grainId = TestDataGenerator.GenerateTestGuid(4);
        var grain = _fixture.GetTestGrain(grainId);
        var input = 42;

        // Act
        var result = await grain.ComputeAsync(input);

        // Assert
        result.Should().BeGreaterThan(input);
        _output.WriteLine($"Computation result for {input}: {result}");
    }

    [Fact]
    public async Task ShouldWaitForGrainActivations()
    {
        // Arrange
        var grainIds = GrainWarmupTestFixture.GenerateTestGrainIds(3, "wait");
        var timeout = TimeSpan.FromSeconds(10);

        // Act
        var startTime = DateTime.UtcNow;
        await _fixture.WaitForGrainActivationsAsync(grainIds, timeout);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        elapsed.Should().BeLessThan(timeout);
        
        // Verify all grains are actually activated by checking their access count
        foreach (var grainId in grainIds)
        {
            var grain = _fixture.GetTestGrain(grainId);
            var accessCount = await grain.GetAccessCountAsync();
            accessCount.Should().BeGreaterThan(0, $"Grain {grainId} should have been accessed");
        }
        
        _output.WriteLine($"Successfully waited for {grainIds.Count} grain activations in {elapsed.TotalMilliseconds:F2}ms");
    }

    [Fact]
    public async Task ShouldSimulateDatabaseOperations()
    {
        // Arrange
        var grainId = TestDataGenerator.GenerateTestGuid(5);
        var grain = _fixture.GetTestGrain(grainId);
        var delayMs = 50;

        // Act
        var startTime = DateTime.UtcNow;
        var result = await grain.SimulateDatabaseOperationAsync(delayMs);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        result.Should().Contain("Database operation completed");
        result.Should().Contain(grainId.ToString());
        elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(delayMs - 10)); // Allow some tolerance
        
        _output.WriteLine($"Database simulation completed in {elapsed.TotalMilliseconds:F2}ms: {result}");
    }

    [Fact]
    public async Task ShouldProvideGrainMetadata()
    {
        // Arrange - Use fresh GUID to ensure new grain activation
        var grainId = Guid.NewGuid();
        var grain = _fixture.GetTestGrain(grainId);

        // Act
        await grain.PingAsync(); // Activate the grain
        var metadata = await grain.GetMetadataAsync();

        // Assert
        metadata.Should().NotBeNull();
        metadata.GrainId.Should().Be(grainId);
        metadata.ActivationTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        metadata.AccessCount.Should().BeGreaterThan(0);
        metadata.SiloAddress.Should().NotBeNullOrEmpty();
        
        _output.WriteLine($"Grain metadata: ID={metadata.GrainId}, ActivationTime={metadata.ActivationTime}, AccessCount={metadata.AccessCount}");
    }

    [Fact]
    public async Task ShouldGenerateUniqueGrainIds()
    {
        // Arrange & Act
        var grainIds1 = GrainWarmupTestFixture.GenerateTestGrainIds(10, "unique1");
        var grainIds2 = GrainWarmupTestFixture.GenerateTestGrainIds(10, "unique2");
        var grainIds3 = GrainWarmupTestFixture.GenerateTestGrainIds(10); // No prefix

        // Assert
        grainIds1.Should().HaveCount(10);
        grainIds2.Should().HaveCount(10);
        grainIds3.Should().HaveCount(10);
        
        grainIds1.Should().OnlyHaveUniqueItems();
        grainIds2.Should().OnlyHaveUniqueItems();
        grainIds3.Should().OnlyHaveUniqueItems();
        
        // Verify different prefixes generate different IDs
        grainIds1.Should().NotIntersectWith(grainIds2);
        grainIds1.Should().NotIntersectWith(grainIds3);
        grainIds2.Should().NotIntersectWith(grainIds3);
        
        _output.WriteLine($"Generated unique grain ID sets: {grainIds1.Count}, {grainIds2.Count}, {grainIds3.Count}");
    }
} 