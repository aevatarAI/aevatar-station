using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Aevatar.Silo.AgentWarmup;
using Aevatar.Silo.AgentWarmup.Extensions;
using AgentWarmupE2E.Fixtures;
using E2E.Grains;
using AgentWarmupE2E.Utilities;

namespace AgentWarmupE2E.Tests;

/// <summary>
/// Basic functionality tests for agent system (testing against real silo)
/// </summary>
public class BasicFunctionalityTests : IClassFixture<AgentWarmupTestFixture>, IAsyncLifetime
{
    private readonly AgentWarmupTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public BasicFunctionalityTests(AgentWarmupTestFixture fixture, ITestOutputHelper output)
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
        
        // Verify we can access agent factory
        var agentFactory = _fixture.AgentFactory;
        agentFactory.Should().NotBeNull();
        
        _output.WriteLine("Successfully verified silo connection");
    }

    [Fact]
    public async Task ShouldActivateAgent()
    {
        // Arrange
        var agentId = TestDataGenerator.GenerateTestGuid(1);
        var agent = _fixture.GetTestAgent(agentId);

        // Act
        var result = await agent.PingAsync();

        // Assert
        result.Should().Contain("Pong");
        result.Should().Contain(agentId.ToString());
        _output.WriteLine($"Successfully activated agent {agentId} and received: {result}");
    }

    [Fact]
    public async Task ShouldActivateMultipleAgents()
    {
        // Arrange
        var agentIds = AgentWarmupTestFixture.GenerateTestAgentIds(5, "basic");
        
        // Act
        var tasks = agentIds.Select(async agentId =>
        {
            var agent = _fixture.GetTestAgent(agentId);
            return await agent.PingAsync();
        });

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllSatisfy(result => result.Should().Contain("Pong"));
        results.Should().HaveCount(5);
        
        _output.WriteLine($"Successfully activated {results.Length} agents");
    }

    [Fact]
    public async Task ShouldTrackAgentActivationTime()
    {
        // Arrange - Use fresh GUID to ensure new agent activation
        var agentId = Guid.NewGuid();
        var agent = _fixture.GetTestAgent(agentId);

        // Act - First activation and get activation time
        var result = await agent.PingAsync();
        var activationTime = await agent.GetActivationTimeAsync();

        // Assert
        result.Should().Contain("Pong");
        activationTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        _output.WriteLine($"Agent {agentId} activated at: {activationTime}");
    }

    [Fact]
    public async Task ShouldTrackAccessCount()
    {
        // Arrange
        var agentId = TestDataGenerator.GenerateTestGuid(3);
        var agent = _fixture.GetTestAgent(agentId);

        // Act
        await agent.PingAsync(); // First call
        await agent.PingAsync(); // Second call
        var accessCount = await agent.GetAccessCountAsync();

        // Assert
        accessCount.Should().BeGreaterOrEqualTo(2);
        _output.WriteLine($"Agent {agentId} access count: {accessCount}");
    }

    [Fact]
    public async Task ShouldPerformComputation()
    {
        // Arrange
        var agentId = TestDataGenerator.GenerateTestGuid(4);
        var agent = _fixture.GetTestAgent(agentId);
        var input = 42;

        // Act
        var result = await agent.ComputeAsync(input);

        // Assert
        result.Should().BeGreaterThan(input);
        _output.WriteLine($"Computation result for {input}: {result}");
    }

    [Fact]
    public async Task ShouldWaitForAgentActivations()
    {
        // Arrange
        var agentIds = AgentWarmupTestFixture.GenerateTestAgentIds(3, "wait");
        var timeout = TimeSpan.FromSeconds(10);

        // Act
        var startTime = DateTime.UtcNow;
        await _fixture.WaitForAgentActivationsAsync(agentIds, timeout);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        elapsed.Should().BeLessThan(timeout);
        
        // Verify all agents are actually activated by checking their access count
        foreach (var agentId in agentIds)
        {
            var agent = _fixture.GetTestAgent(agentId);
            var accessCount = await agent.GetAccessCountAsync();
            accessCount.Should().BeGreaterThan(0, $"Agent {agentId} should have been accessed");
        }
        
        _output.WriteLine($"Successfully waited for {agentIds.Count} agent activations in {elapsed.TotalMilliseconds:F2}ms");
    }

    [Fact]
    public async Task ShouldSimulateDatabaseOperations()
    {
        // Arrange
        var agentId = TestDataGenerator.GenerateTestGuid(5);
        var agent = _fixture.GetTestAgent(agentId);
        var delayMs = 50;

        // Act
        var startTime = DateTime.UtcNow;
        var result = await agent.SimulateDatabaseOperationAsync(delayMs);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        result.Should().Contain("Database operation completed");
        result.Should().Contain(agentId.ToString());
        elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(delayMs - 10)); // Allow some tolerance
        
        _output.WriteLine($"Database simulation completed in {elapsed.TotalMilliseconds:F2}ms: {result}");
    }

    [Fact]
    public async Task ShouldProvideAgentMetadata()
    {
        // Arrange - Use fresh GUID to ensure new agent activation
        var agentId = Guid.NewGuid();
        var agent = _fixture.GetTestAgent(agentId);

        // Act
        await agent.PingAsync(); // Activate the agent
        var metadata = await agent.GetMetadataAsync();

        // Assert
        metadata.Should().NotBeNull();
        metadata.AgentId.Should().Be(agentId);
        metadata.ActivationTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        metadata.AccessCount.Should().BeGreaterThan(0);
        metadata.SiloAddress.Should().NotBeNullOrEmpty();
        
        _output.WriteLine($"Agent metadata: ID={metadata.AgentId}, ActivationTime={metadata.ActivationTime}, AccessCount={metadata.AccessCount}");
    }

    [Fact]
    public async Task ShouldGenerateUniqueAgentIds()
    {
        // Arrange & Act
        var agentIds1 = AgentWarmupTestFixture.GenerateTestAgentIds(10, "unique1");
        var agentIds2 = AgentWarmupTestFixture.GenerateTestAgentIds(10, "unique2");
        var agentIds3 = AgentWarmupTestFixture.GenerateTestAgentIds(10); // No prefix

        // Assert
        agentIds1.Should().HaveCount(10);
        agentIds2.Should().HaveCount(10);
        agentIds3.Should().HaveCount(10);
        
        agentIds1.Should().OnlyHaveUniqueItems();
        agentIds2.Should().OnlyHaveUniqueItems();
        agentIds3.Should().OnlyHaveUniqueItems();
        
        // Verify different prefixes generate different IDs
        agentIds1.Should().NotIntersectWith(agentIds2);
        agentIds1.Should().NotIntersectWith(agentIds3);
        agentIds2.Should().NotIntersectWith(agentIds3);
        
        _output.WriteLine($"Generated unique agent ID sets: {agentIds1.Count}, {agentIds2.Count}, {agentIds3.Count}");
    }
} 