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
public class TestMetaDataAgent : GAgentBase<TestMetaDataAgentState, MetaDataStateLogEvent>, 
    ITestMetaDataAgent
{
    public TestMetaDataAgent()
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Test agent for metadata operations with Orleans integration testing");
    }
    
    Task<IMetaDataState> IMetaDataStateGAgent.GetState() => Task.FromResult<IMetaDataState>(State);
    
    public Task<GrainId> GetGrainIdAsync() => Task.FromResult(GrainId.Create(typeof(ITestMetaDataAgent).Name, this.GetPrimaryKey().ToString()));

    public Task RaiseEvent(MetaDataStateLogEvent @event)
    {
        // Apply the metadata event directly to the state using the state's Apply method
        State.Apply(@event);
        
        // For testing purposes, also raise the metadata event through the base class
        // This will trigger the GAgentTransitionState method
        base.RaiseEvent(@event);
        return Task.CompletedTask;
    }

    // Test-specific methods for Orleans integration testing
    public async Task HandleTestEventAsync(TestMetaDataAgentEvent @event)
    {
        // Raise the test event through the base class
        base.RaiseEvent(@event);
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
        StateLogEventBase<MetaDataStateLogEvent> @event)
    {
        // Call base implementation first
        base.GAgentTransitionState(state, @event);
        
        // Track all events for testing purposes
        if (@event is MetaDataStateLogEvent metaEvent)
        {
            // Update last activity time for all metadata events
            state.LastActivity = DateTime.UtcNow;
            
            // Track the event for testing
            state.TestEventCount++;
            state.TestMessages.Add($"Metadata event processed: {metaEvent.GetType().Name}");
            
            // If it's specifically a test tracking event, handle it specially
            if (metaEvent is TestMetaDataAgentEvent testEvent && !string.IsNullOrEmpty(testEvent.TestMessage))
            {
                state.TestMessages.Add(testEvent.TestMessage);
            }
        }
    }
}