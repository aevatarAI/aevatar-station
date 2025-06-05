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
using Aevatar.Silo.AgentWarmup;
using Aevatar.Silo.Tests.AgentWarmup.Fixtures;
using Aevatar.Silo.Tests.AgentWarmup.TestAgents;

namespace Aevatar.Silo.Tests.AgentWarmup.Tests;

/// <summary>
/// Integration tests for agent warmup functionality
/// </summary>
[Collection("AgentWarmup")]
public class AgentWarmupIntegrationTests : IAsyncLifetime
{
    private readonly AgentWarmupTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public AgentWarmupIntegrationTests(AgentWarmupTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ShouldRegisterAgentWarmupServices()
    {
        // Arrange - Get the primary silo's service provider
        var primarySilo = _fixture.Cluster.Primary;
        var services = _fixture.Cluster.GetSiloServiceProvider(primarySilo.SiloAddress);

        // Act & Assert
        var agentWarmupService = services.GetService<IAgentWarmupService>();
        agentWarmupService.ShouldNotBeNull("IAgentWarmupService should be registered");

        var agentWarmupOrchestrator = services.GetService<IAgentWarmupOrchestrator<Guid>>();
        agentWarmupOrchestrator.ShouldNotBeNull("IAgentWarmupOrchestrator<Guid> should be registered");

        var agentDiscoveryService = services.GetRequiredService<IAgentDiscoveryService>();
        agentDiscoveryService.ShouldNotBeNull("IAgentDiscoveryService should be registered");

        _output.WriteLine("All agent warmup services are properly registered");
    }

    [Fact]
    public async Task ShouldDiscoverTestAgentTypes()
    {
        // Arrange - Get the primary silo's service provider
        var primarySilo = _fixture.Cluster.Primary;
        var services = _fixture.Cluster.GetSiloServiceProvider(primarySilo.SiloAddress);
        var agentDiscoveryService = services.GetRequiredService<IAgentDiscoveryService>();

        // Act
        var discoveredTypes = agentDiscoveryService.DiscoverWarmupEligibleAgentTypes().ToList();

        // Assert
        discoveredTypes.ShouldNotBeEmpty("Should discover at least some agent types");
        discoveredTypes.ShouldContain(t => t.Name.Contains("TestWarmupAgent"), 
            "Should discover our test agent type");

        _output.WriteLine($"Discovered {discoveredTypes.Count} agent types:");
        foreach (var agentType in discoveredTypes.Take(10)) // Show first 10
        {
            _output.WriteLine($"  - {agentType.Name}");
        }
    }

    [Fact]
    public async Task ShouldActivateAgentsInBatches()
    {
        // Arrange
        var agentIds = Enumerable.Range(1, 10).Select(_ => Guid.NewGuid()).ToList();
        var batchSize = 3;
        var activatedAgents = new List<ITestWarmupAgent>();

        // Act
        for (int i = 0; i < agentIds.Count; i += batchSize)
        {
            var batch = agentIds.Skip(i).Take(batchSize);
            var batchTasks = batch.Select(async agentId =>
            {
                var agent = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupAgent>(agentId);
                await agent.PingAsync(); // Activate the agent
                return agent;
            });

            var batchResults = await Task.WhenAll(batchTasks);
            activatedAgents.AddRange(batchResults);

            _output.WriteLine($"Activated batch {(i / batchSize) + 1} with {batchResults.Length} agents");
            
            // Small delay between batches to simulate warmup behavior
            await Task.Delay(100);
        }

        // Assert
        activatedAgents.Count.ShouldBe(agentIds.Count);

        // Verify all agents are actually activated by checking their access count
        foreach (var agent in activatedAgents)
        {
            var accessCount = await agent.GetAccessCountAsync();
            accessCount.ShouldBeGreaterThan(0, "Each agent should have been accessed at least once");
        }

        _output.WriteLine($"Successfully activated {activatedAgents.Count} agents in batches of {batchSize}");
    }

    [Fact]
    public async Task ShouldHandleConcurrentAgentActivation()
    {
        // Arrange
        var agentIds = Enumerable.Range(1, 20).Select(_ => Guid.NewGuid()).ToList();
        var maxConcurrency = 5;

        // Act
        var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = agentIds.Select(async agentId =>
        {
            await semaphore.WaitAsync();
            try
            {
                var agent = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupAgent>(agentId);
                var result = await agent.PingAsync();
                return new { AgentId = agentId, Result = result, Timestamp = DateTime.UtcNow };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldAllBe(result => result.Result.Contains("Pong"));
        results.Length.ShouldBe(agentIds.Count);

        // Verify timing - all should complete within reasonable time
        var minTime = results.Min(r => r.Timestamp);
        var maxTime = results.Max(r => r.Timestamp);
        var totalDuration = maxTime - minTime;

        totalDuration.ShouldBeLessThan(TimeSpan.FromSeconds(30), 
            "Concurrent activation should complete within reasonable time");

        _output.WriteLine($"Successfully activated {results.Length} agents concurrently " +
                         $"(max concurrency: {maxConcurrency}) in {totalDuration.TotalMilliseconds:F2}ms");
    }

    [Fact]
    public async Task ShouldMeasureAgentActivationPerformance()
    {
        // Arrange
        var agentCount = 50;
        var agentIds = Enumerable.Range(1, agentCount).Select(_ => Guid.NewGuid()).ToList();

        // Act
        var startTime = DateTime.UtcNow;
        
        var tasks = agentIds.Select(async agentId =>
        {
            var agent = _fixture.Cluster.GrainFactory.GetGrain<ITestWarmupAgent>(agentId);
            var activationTime = DateTime.UtcNow;
            await agent.PingAsync();
            return new { AgentId = agentId, ActivationTime = activationTime };
        });

        var results = await Task.WhenAll(tasks);
        var endTime = DateTime.UtcNow;

        // Assert
        var totalDuration = endTime - startTime;
        var averageActivationTime = totalDuration.TotalMilliseconds / agentCount;

        results.Length.ShouldBe(agentCount);
        totalDuration.ShouldBeLessThan(TimeSpan.FromMinutes(1), 
            "Should activate all agents within reasonable time");

        _output.WriteLine($"Performance Results:");
        _output.WriteLine($"  - Total agents activated: {agentCount}");
        _output.WriteLine($"  - Total duration: {totalDuration.TotalMilliseconds:F2}ms");
        _output.WriteLine($"  - Average activation time: {averageActivationTime:F2}ms per agent");
        _output.WriteLine($"  - Throughput: {agentCount / totalDuration.TotalSeconds:F2} agents/second");
    }

    [Fact]
    public async Task ShouldVerifyAgentWarmupServiceIsHostedService()
    {
        // Arrange - Get the primary silo's service provider
        var primarySilo = _fixture.Cluster.Primary;
        var services = _fixture.Cluster.GetSiloServiceProvider(primarySilo.SiloAddress);

        // Act
        var hostedServices = services.GetServices<IHostedService>();
        var agentWarmupHostedService = hostedServices.OfType<IAgentWarmupService>().FirstOrDefault();

        // Assert
        agentWarmupHostedService.ShouldNotBeNull("AgentWarmupService should be registered as IHostedService");
        
        _output.WriteLine("AgentWarmupService is properly registered as a hosted service");
    }
} 