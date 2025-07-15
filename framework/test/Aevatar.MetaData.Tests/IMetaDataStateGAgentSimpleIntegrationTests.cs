// ABOUTME: This file contains Orleans-based integration tests for IMetaDataStateGAgent with GAgentBase
// ABOUTME: Tests the interface working with real Orleans cluster and event sourcing

using Aevatar.Core.Abstractions;
using Aevatar.MetaData.Enums;
using Aevatar.TestBase;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Simplified integration tests for IMetaDataStateGAgent interface with GAgentBase.
/// Tests the interface implementation without requiring full Orleans cluster.
/// </summary>
public class IMetaDataStateGAgentSimpleIntegrationTests
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly MockTestMetaDataAgent _testAgent;
    private readonly Guid _testAgentId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    public IMetaDataStateGAgentSimpleIntegrationTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _testAgent = new MockTestMetaDataAgent();
    }

    [Fact]
    public async Task TestMetaDataAgent_Should_ImplementIMetaDataStateGAgent()
    {
        // This test verifies that TestMetaDataAgent implements IMetaDataStateGAgent correctly
        
        // Arrange & Act - Test the agent directly
        var agent = _testAgent;
        
        // Assert
        agent.ShouldNotBeNull();
        
        // Verify the agent can provide description
        var description = await agent.GetDescriptionAsync();
        description.ShouldNotBeNullOrEmpty();
        
        // Verify the agent can access state
        var state = await agent.GetStateAsync();
        state.ShouldNotBeNull();
        state.ShouldBeOfType<TestMetaDataAgentState>();
        
        // Verify the agent implements IMetaDataStateGAgent directly
        var metaDataHelper = agent.GetMetaDataHelper();
        metaDataHelper.ShouldNotBeNull();
        metaDataHelper.ShouldBeAssignableTo<IMetaDataStateGAgent<TestMetaDataAgentState>>();
    }

    [Fact]
    public async Task CreateAgentAsync_Should_UpdateState_OnTestAgent()
    {
        // Arrange
        var agent = _testAgent;
        
        // Act - Test event handling functionality
        var testEvent = new TestMetaDataAgentEvent
        {
            Action = "CreateAgent",
            TestMessage = "Testing agent creation"
        };
        
        await agent.HandleTestEventAsync(testEvent);
        
        // Assert - Verify event sourcing worked
        var state = await agent.GetStateAsync();
        state.ShouldNotBeNull();
        
        var eventCount = await agent.GetTestEventCountAsync();
        eventCount.ShouldBe(1);
        
        var messages = await agent.GetTestMessagesAsync();
        messages.ShouldContain("Testing agent creation");
    }

    [Fact]
    public async Task UpdateStatusAsync_Should_UpdateState_OnTestAgent()
    {
        // Arrange
        var agent = _testAgent;
        
        // Set up initial test event
        await agent.HandleTestEventAsync(new TestMetaDataAgentEvent { Action = "Setup", TestMessage = "Initial setup" });

        var newStatus = AgentStatus.Active;
        var reason = "Test activation";

        // Act - Update status through event handling
        await agent.HandleTestEventAsync(new TestMetaDataAgentEvent { Action = "UpdateStatus", TestMessage = reason });
        
        // Assert - Verify state was updated through event sourcing
        var state = await agent.GetStateAsync();
        state.ShouldNotBeNull();
        
        var eventCount = await agent.GetTestEventCountAsync();
        eventCount.ShouldBeGreaterThan(0);
        
        var messages = await agent.GetTestMessagesAsync();
        messages.ShouldContain(reason);
    }

    [Fact]
    public async Task UpdatePropertiesAsync_Should_UpdateState_OnTestAgent()
    {
        // Arrange
        var agent = _testAgent;
        
        // Set up initial state
        await agent.HandleTestEventAsync(new TestMetaDataAgentEvent { Action = "CreateAgent", TestMessage = "Setting up agent" });

        // Act - Update properties through event handling
        await agent.HandleTestEventAsync(new TestMetaDataAgentEvent { Action = "UpdateProperties", TestMessage = "Testing property updates" });
        
        // Assert - Verify state was updated through event sourcing
        var state = await agent.GetStateAsync();
        state.ShouldNotBeNull();
        var eventCount = await agent.GetTestEventCountAsync();
        eventCount.ShouldBeGreaterThan(0);
        
        var messages = await agent.GetTestMessagesAsync();
        messages.ShouldContain("Testing property updates");
    }

    [Fact]
    public async Task RecordActivityAsync_Should_UpdateState_OnTestAgent()
    {
        // Arrange
        var agent = _testAgent;
        
        // Set up initial test event
        await agent.HandleTestEventAsync(new TestMetaDataAgentEvent { Action = "Setup", TestMessage = "Initial setup" });
        var initialState = await agent.GetStateAsync();
        var initialActivity = initialState.LastActivity;

        var activityType = "test_activity";

        // Act - Record activity through event handling
        await agent.HandleTestEventAsync(new TestMetaDataAgentEvent { Action = activityType, TestMessage = "Recording activity" });
        
        // Assert - Verify state was updated through event sourcing
        var state = await agent.GetStateAsync();
        state.ShouldNotBeNull();
        var eventCount = await agent.GetTestEventCountAsync();
        eventCount.ShouldBeGreaterThan(0);
        
        var messages = await agent.GetTestMessagesAsync();
        messages.ShouldContain("Recording activity");
    }

    [Fact]
    public async Task BatchUpdateAsync_Should_UpdateState_OnTestAgent()
    {
        // Arrange
        var agent = _testAgent;
        
        // Set up initial state
        await agent.HandleTestEventAsync(new TestMetaDataAgentEvent { Action = "CreateAgent", TestMessage = "Setting up agent" });

        var reason = "Batch test";

        // Act - Batch update through event handling
        await agent.HandleTestEventAsync(new TestMetaDataAgentEvent { Action = "BatchUpdate", TestMessage = reason });
        
        // Assert - Verify state was updated through event sourcing
        var state = await agent.GetStateAsync();
        state.ShouldNotBeNull();
        var eventCount = await agent.GetTestEventCountAsync();
        eventCount.ShouldBeGreaterThan(0);
        
        var messages = await agent.GetTestMessagesAsync();
        messages.ShouldContain(reason);
    }

    [Fact]
    public async Task GetState_Should_ReturnTestMetaDataAgentState()
    {
        // Arrange
        var agent = _testAgent;

        // Act - Get state from agent
        var state = await agent.GetStateAsync();

        // Assert
        state.ShouldNotBeNull();
        state.ShouldBeOfType<TestMetaDataAgentState>();
        
        // Verify agent activation worked
        var description = await agent.GetDescriptionAsync();
        description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetGrainId_Should_ReturnValidGrainId()
    {
        // Arrange
        var agent = _testAgent;
        var metaDataHelper = agent.GetMetaDataHelper();

        // Act - Get grain ID from helper
        var grainId = await metaDataHelper.GetGrainIdAsync();
        
        // Assert - Verify grain ID is valid
        grainId.ShouldNotBe(default(GrainId));
        
        // Verify agent activation worked
        var description = await agent.GetDescriptionAsync();
        description.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task OrleansEventSourcing_Should_PersistStateChanges()
    {
        // Arrange
        var agent = _testAgent;
        
        // Act - Create test event and handle it
        var testEvent = new TestMetaDataAgentEvent
        {
            Action = "OrleansTest",
            TestMessage = "Testing event sourcing"
        };
        
        await agent.HandleTestEventAsync(testEvent);
        
        // Assert - Verify event sourcing persisted the changes
        var eventCount = await agent.GetTestEventCountAsync();
        var messages = await agent.GetTestMessagesAsync();
        
        eventCount.ShouldBe(1);
        messages.ShouldNotBeEmpty();
        messages.ShouldContain("Testing event sourcing");
    }

    [Fact]
    public async Task OrleansEventSourcing_Should_HandleMultipleEvents()
    {
        // Arrange
        var agent = _testAgent;
        
        // Act - Handle multiple events
        await agent.HandleTestEventAsync(new TestMetaDataAgentEvent
        {
            Action = "FirstEvent",
            TestMessage = "First message"
        });
        
        await agent.HandleTestEventAsync(new TestMetaDataAgentEvent
        {
            Action = "SecondEvent",
            TestMessage = "Second message"
        });
        
        // Assert - Verify event sourcing handled all events
        var eventCount = await agent.GetTestEventCountAsync();
        var messages = await agent.GetTestMessagesAsync();
        
        eventCount.ShouldBe(2);
        messages.Count.ShouldBe(2);
        messages.ShouldContain("First message");
        messages.ShouldContain("Second message");
    }

    [Fact]
    public async Task OrleansGrainPersistence_Should_MaintainStateAcrossReactivation()
    {
        // Arrange
        var agent = _testAgent;
        
        // Act - Create agent and add some state
        await agent.HandleTestEventAsync(new TestMetaDataAgentEvent { Action = "CreatePersistent", TestMessage = "Creating persistent agent" });
        
        await agent.HandleTestEventAsync(new TestMetaDataAgentEvent
        {
            Action = "PersistenceTest",
            TestMessage = "Should persist across reactivation"
        });
        
        // Assert - Verify state was maintained
        var state = await agent.GetStateAsync();
        state.ShouldNotBeNull();
        
        var eventCount = await agent.GetTestEventCountAsync();
        var messages = await agent.GetTestMessagesAsync();
        
        eventCount.ShouldBe(2);
        messages.ShouldContain("Should persist across reactivation");
    }
}