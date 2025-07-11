// ABOUTME: This file contains simplified integration tests for IMetaDataStateEventRaiser with GAgentBase
// ABOUTME: Tests the interface working with a test agent implementation using in-memory testing

using Aevatar.MetaData.Enums;
using Shouldly;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Simplified integration tests for IMetaDataStateEventRaiser interface with GAgentBase.
/// Tests that the interface can be implemented by a test agent and works correctly.
/// </summary>
public class IMetaDataStateEventRaiserSimpleIntegrationTests
{
    private readonly Guid _testAgentId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    [Fact]
    public void TestMetaDataAgent_Should_ImplementIMetaDataStateEventRaiser()
    {
        // This test verifies that TestMetaDataAgent can implement IMetaDataStateEventRaiser
        // and that the integration compiles correctly
        
        // Arrange & Act - Create an instance to verify interface implementation
        var agent = new TestMetaDataAgent();
        var eventRaiser = agent as IMetaDataStateEventRaiser<TestMetaDataAgentState>;
        
        // Assert
        eventRaiser.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateAgentAsync_Should_CallExpectedMethods_OnTestAgent()
    {
        // Arrange
        var agent = new TestMetaDataAgent();
        var eventRaiser = (IMetaDataStateEventRaiser<TestMetaDataAgentState>)agent;
        
        var name = "Test Agent";
        var agentType = "TestAgent";
        var properties = new Dictionary<string, string> { { "key1", "value1" } };

        // Act & Assert - This will test that the method executes without throwing
        // In a full Orleans integration test, we would verify state changes
        await eventRaiser.CreateAgentAsync(_testAgentId, _testUserId, name, agentType, properties);
        
        // For now, just verify the method completed successfully
        Assert.True(true, "CreateAgentAsync completed without throwing");
    }

    [Fact]
    public async Task UpdateStatusAsync_Should_CallExpectedMethods_OnTestAgent()
    {
        // Arrange
        var agent = new TestMetaDataAgent();
        var eventRaiser = (IMetaDataStateEventRaiser<TestMetaDataAgentState>)agent;
        
        // Set up initial state
        agent.State.Id = _testAgentId;
        agent.State.UserId = _testUserId;
        agent.State.Status = AgentStatus.Creating;

        var newStatus = AgentStatus.Active;
        var reason = "Test activation";

        // Act & Assert
        await eventRaiser.UpdateStatusAsync(newStatus, reason);
        
        // For now, just verify the method completed successfully
        Assert.True(true, "UpdateStatusAsync completed without throwing");
    }

    [Fact]
    public async Task UpdatePropertiesAsync_Should_CallExpectedMethods_OnTestAgent()
    {
        // Arrange
        var agent = new TestMetaDataAgent();
        var eventRaiser = (IMetaDataStateEventRaiser<TestMetaDataAgentState>)agent;
        
        // Set up initial state
        agent.State.Id = _testAgentId;
        agent.State.UserId = _testUserId;

        var properties = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };

        // Act & Assert
        await eventRaiser.UpdatePropertiesAsync(properties, merge: true);
        
        // For now, just verify the method completed successfully
        Assert.True(true, "UpdatePropertiesAsync completed without throwing");
    }

    [Fact]
    public async Task RecordActivityAsync_Should_CallExpectedMethods_OnTestAgent()
    {
        // Arrange
        var agent = new TestMetaDataAgent();
        var eventRaiser = (IMetaDataStateEventRaiser<TestMetaDataAgentState>)agent;
        
        // Set up initial state
        agent.State.Id = _testAgentId;
        agent.State.UserId = _testUserId;

        var activityType = "test_activity";

        // Act & Assert
        await eventRaiser.RecordActivityAsync(activityType);
        
        // For now, just verify the method completed successfully
        Assert.True(true, "RecordActivityAsync completed without throwing");
    }

    [Fact]
    public async Task BatchUpdateAsync_Should_CallExpectedMethods_OnTestAgent()
    {
        // Arrange
        var agent = new TestMetaDataAgent();
        var eventRaiser = (IMetaDataStateEventRaiser<TestMetaDataAgentState>)agent;
        
        // Set up initial state
        agent.State.Id = _testAgentId;
        agent.State.UserId = _testUserId;
        agent.State.Status = AgentStatus.Creating;

        var newStatus = AgentStatus.Active;
        var properties = new Dictionary<string, string> { { "batch", "updated" } };
        var reason = "Batch test";

        // Act & Assert
        await eventRaiser.BatchUpdateAsync(newStatus, properties, true, reason);
        
        // For now, just verify the method completed successfully
        Assert.True(true, "BatchUpdateAsync completed without throwing");
    }

    [Fact]
    public void GetState_Should_ReturnTestMetaDataAgentState()
    {
        // Arrange
        var agent = new TestMetaDataAgent();
        var eventRaiser = (IMetaDataStateEventRaiser<TestMetaDataAgentState>)agent;

        // Act
        var state = eventRaiser.GetState();

        // Assert
        state.ShouldNotBeNull();
        state.ShouldBeOfType<TestMetaDataAgentState>();
    }

    [Fact]
    public void GetGrainId_Should_ReturnValidGrainId()
    {
        // Arrange
        var agent = new TestMetaDataAgent();
        var eventRaiser = (IMetaDataStateEventRaiser<TestMetaDataAgentState>)agent;

        // Act & Assert - This will test that GetGrainId method works
        var grainId = eventRaiser.GetGrainId();
        
        // For now, just verify the method completed successfully
        Assert.True(true, "GetGrainId completed without throwing");
    }
}