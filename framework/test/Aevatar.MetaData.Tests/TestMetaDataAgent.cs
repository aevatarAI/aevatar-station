// ABOUTME: This file defines a test agent that implements both GAgentBase and IMetaDataStateGAgent
// ABOUTME: Used for integration testing to verify Orleans event sourcing works with metadata events

using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.MetaData;
using Aevatar.MetaData.Enums;
using Aevatar.MetaData.Events;
using Orleans.EventSourcing;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Test agent that implements both GAgentBase and IMetaDataStateGAgent for integration testing.
/// Demonstrates how to use the metadata state interface with Orleans event sourcing.
/// </summary>
[GAgent]
public class TestMetaDataAgent : GAgentBase<TestMetaDataAgentState, TestMetaDataAgentEvent>, 
    ITestMetaDataAgent
{
    private readonly TestMetaDataAgentHelper _metaDataHelper;

    public TestMetaDataAgent()
    {
        _metaDataHelper = new TestMetaDataAgentHelper(this);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Test agent for metadata operations with Orleans integration testing");
    }

    /// <summary>
    /// Gets the metadata helper for testing IMetaDataStateGAgent interface (internal use only)
    /// </summary>
    internal IMetaDataStateGAgent<TestMetaDataAgentState> GetMetaDataHelper()
    {
        return _metaDataHelper;
    }

    /// <summary>
    /// Internal method to provide state access to the helper
    /// </summary>
    internal TestMetaDataAgentState GetInternalState()
    {
        return State;
    }

    /// <summary>
    /// Internal method to provide event raising to the helper
    /// </summary>
    internal void RaiseInternalEvent(TestMetaDataAgentEvent @event)
    {
        RaiseEvent(@event);
    }

    // Orleans-compatible async method for interface
    public new Task<TestMetaDataAgentState> GetStateAsync()
    {
        return Task.FromResult(State);
    }

    // Test-specific methods for Orleans integration testing
    public async Task HandleTestEventAsync(TestMetaDataAgentEvent @event)
    {
        RaiseEvent(@event);
        await ConfirmEvents();
    }

    public Task<int> GetTestEventCountAsync()
    {
        return Task.FromResult(State.TestEventCount);
    }

    public Task<List<string>> GetTestMessagesAsync()
    {
        return Task.FromResult(new List<string>(State.TestMessages));
    }

    public async Task ClearTestDataAsync()
    {
        var clearEvent = new TestMetaDataAgentEvent
        {
            Action = "ClearTestData",
            TestMessage = "Test data cleared"
        };
        RaiseEvent(clearEvent);
        State.TestEventCount = 0;
        State.TestMessages.Clear();
        await ConfirmEvents();
    }

    // Event handler for custom events
    [EventHandler]
    public async Task HandleTestEventInternalAsync(TestMetaDataAgentEvent @event)
    {
        State.TestEventCount++;
        State.TestMessages.Add(@event.TestMessage);
        await ConfirmEvents();
    }

    // Override state transition method to handle custom events
    protected override void GAgentTransitionState(TestMetaDataAgentState state,
        StateLogEventBase<TestMetaDataAgentEvent> @event)
    {
        // Handle custom event transitions
        if (@event is TestMetaDataAgentEvent testEvent)
        {
            state.TestEventCount++;
            if (!string.IsNullOrEmpty(testEvent.TestMessage))
            {
                state.TestMessages.Add(testEvent.TestMessage);
            }
        }
    }
}

/// <summary>
/// Helper class that implements IMetaDataStateGAgent for composition within TestMetaDataAgent.
/// This allows the grain to be Orleans-compatible while still testing the IMetaDataStateGAgent interface.
/// </summary>
internal class TestMetaDataAgentHelper : IMetaDataStateGAgent<TestMetaDataAgentState>
{
    private readonly TestMetaDataAgent _agent;

    public TestMetaDataAgentHelper(TestMetaDataAgent agent)
    {
        _agent = agent;
    }

    public void RaiseEvent(MetaDataStateLogEvent @event)
    {
        // For testing purposes, we'll just raise a test event to track that the metadata event happened
        // We cannot directly apply metadata events to state as they need proper Orleans event sourcing
        var testEvent = new TestMetaDataAgentEvent
        {
            Action = @event.GetType().Name,
            TestMessage = $"Metadata event: {@event.GetType().Name}"
        };
        
        _agent.RaiseInternalEvent(testEvent);
    }

    public Task ConfirmEvents()
    {
        return _agent.ConfirmEvents();
    }

    public TestMetaDataAgentState GetState()
    {
        return _agent.GetInternalState();
    }
    
    IMetaDataState IMetaDataStateGAgent.GetState() => _agent.GetInternalState();

    public GrainId GetGrainId()
    {
        return GrainId.Create(typeof(ITestMetaDataAgent).Name, _agent.GetPrimaryKey().ToString());
    }
}