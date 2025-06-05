using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Aevatar.Silo.AgentWarmup;
using Aevatar.Silo.Tests.AgentWarmup.Fixtures;
using Aevatar.Silo.Tests.AgentWarmup.TestAgents;

namespace Aevatar.Silo.Tests.AgentWarmup.Tests;

/// <summary>
/// Basic functionality tests for agent warmup system integrated with Aevatar.Silo.Tests infrastructure
/// </summary>
[Collection("AgentWarmup")]
public class BasicFunctionalityTests : IAsyncLifetime
{
    private readonly AgentWarmupTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public BasicFunctionalityTests(AgentWarmupTestFixture fixture, ITestOutputHelper output)
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
        
        // Verify we can access agent factory
        var agentFactory = _fixture.Cluster.GrainFactory;
        agentFactory.ShouldNotBeNull();
        
        _output.WriteLine("Successfully verified silo connection");
    }

    [Fact]
    public async Task ShouldActivateAgent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupAgent>(agentId);

        // Act
        var result = await agent.PingAsync();

        // Assert
        result.ShouldContain("Pong");
        result.ShouldContain(agentId.ToString());
        _output.WriteLine($"Successfully activated agent {agentId} and received: {result}");
    }

    [Fact]
    public async Task ShouldActivateMultipleAgents()
    {
        // Arrange
        var agentIds = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToList();
        
        // Act
        var tasks = agentIds.Select(async agentId =>
        {
            var agent = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupAgent>(agentId);
            return await agent.PingAsync();
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldAllBe(result => result.Contains("Pong"));
        results.Length.ShouldBe(5);
        
        _output.WriteLine($"Successfully activated {results.Length} agents");
    }

    [Fact]
    public async Task ShouldTrackAgentActivationTime()
    {
        // Arrange - Use fresh GUID to ensure new agent activation
        var agentId = Guid.NewGuid();
        var agent = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupAgent>(agentId);

        // Act - First activation and get activation time
        var result = await agent.PingAsync();
        var activationTime = await agent.GetActivationTimeAsync();

        // Assert
        result.ShouldContain("Pong");
        activationTime.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        
        _output.WriteLine($"Agent {agentId} activated at: {activationTime}");
    }

    [Fact]
    public async Task ShouldTrackAccessCount()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupAgent>(agentId);

        // Act
        await agent.PingAsync(); // First call
        await agent.PingAsync(); // Second call
        var accessCount = await agent.GetAccessCountAsync();

        // Assert
        accessCount.ShouldBeGreaterThanOrEqualTo(2);
        _output.WriteLine($"Agent {agentId} access count: {accessCount}");
    }

    [Fact]
    public async Task ShouldPerformComputation()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupAgent>(agentId);
        var input = 42;

        // Act
        var result = await agent.ComputeAsync(input);

        // Assert
        result.ShouldBeGreaterThan(input);
        _output.WriteLine($"Computation result for {input}: {result}");
    }

    [Fact]
    public async Task ShouldSimulateDatabaseOperations()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupAgent>(agentId);
        var delayMs = 50;

        // Act
        var startTime = DateTime.UtcNow;
        var result = await agent.SimulateDatabaseOperationAsync(delayMs);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        result.ShouldContain("Database operation completed");
        result.ShouldContain(agentId.ToString());
        elapsed.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(delayMs - 10)); // Allow some tolerance
        
        _output.WriteLine($"Database simulation completed in {elapsed.TotalMilliseconds:F2}ms: {result}");
    }

    [Fact]
    public async Task ShouldProvideAgentMetadata()
    {
        // Arrange - Use fresh GUID to ensure new agent activation
        var agentId = Guid.NewGuid();
        var agent = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupAgent>(agentId);

        // Act
        await agent.PingAsync(); // Activate the agent
        var metadata = await agent.GetMetadataAsync();

        // Assert
        metadata.ShouldNotBeNull();
        metadata.AgentId.ShouldBe(agentId);
        metadata.ActivationTime.ShouldBeInRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
        metadata.AccessCount.ShouldBeGreaterThan(0);
        metadata.SiloAddress.ShouldNotBeNullOrEmpty();
        
        _output.WriteLine($"Agent metadata: Id={metadata.AgentId}, " +
                         $"ActivationTime={metadata.ActivationTime}, " +
                         $"AccessCount={metadata.AccessCount}, " +
                         $"SiloAddress={metadata.SiloAddress}");
    }

    [Fact]
    public async Task ShouldVerifyAgentWarmupServiceIsRegistered()
    {
        // Arrange - Get the primary silo's service provider
        var primarySilo = _fixture.Cluster.Primary;
        var services = _fixture.Cluster.GetSiloServiceProvider(primarySilo.SiloAddress);

        // Act & Assert
        var agentWarmupService = services.GetService<IAgentWarmupService>();
        agentWarmupService.ShouldNotBeNull("Agent warmup service should be registered");
        
        var agentWarmupOrchestrator = services.GetService<IAgentWarmupOrchestrator<Guid>>();
        agentWarmupOrchestrator.ShouldNotBeNull("Agent warmup orchestrator should be registered");
        
        _output.WriteLine("Successfully verified agent warmup services are registered");
    }
} 