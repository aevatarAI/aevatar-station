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
    ITestMetaDataAgent, 
    IMetaDataStateGAgent<TestMetaDataAgentState>
{
    public TestMetaDataAgent()
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Test agent for metadata operations with Orleans integration testing");
    }

    // IMetaDataStateGAgent<TestMetaDataAgentState> implementation
    public TestMetaDataAgentState GetState() => State;
    
    IMetaDataState IMetaDataStateGAgent.GetState() => State;
    
    public GrainId GetGrainId() => GrainId.Create(typeof(ITestMetaDataAgent).Name, this.GetPrimaryKey().ToString());
    
    public void RaiseEvent(MetaDataStateLogEvent @event)
    {
        // For testing purposes, we'll raise a test event to track that the metadata event happened
        // We cannot directly apply metadata events to state as they need proper Orleans event sourcing
        var testEvent = new TestMetaDataAgentEvent
        {
            Action = @event.GetType().Name,
            TestMessage = $"Metadata event: {@event.GetType().Name}"
        };
        
        base.RaiseEvent(testEvent);
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


    // Override state transition method to handle custom events
    protected override void GAgentTransitionState(TestMetaDataAgentState state,
        StateLogEventBase<TestMetaDataAgentEvent> @event)
    {
        // Call base implementation first
        base.GAgentTransitionState(state, @event);
        
        // Handle custom event transitions
        if (@event is TestMetaDataAgentEvent testEvent)
        {
            state.TestEventCount++;
            if (!string.IsNullOrEmpty(testEvent.TestMessage))
            {
                state.TestMessages.Add(testEvent.TestMessage);
            }
            
            // Update last activity time for all events
            state.LastActivity = DateTime.UtcNow;
        }
    }
}