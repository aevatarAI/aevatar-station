// ABOUTME: This file defines a test agent that implements both GAgentBase and IMetaDataStateGAgent
// ABOUTME: Used for integration testing to verify Orleans event sourcing works with metadata events

using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.MetaData;
using Aevatar.MetaData.Events;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Test agent that implements both GAgentBase and IMetaDataStateGAgent for integration testing.
/// Demonstrates how to use the metadata state interface with Orleans event sourcing.
/// </summary>
public class TestMetaDataAgent : IMetaDataStateGAgent<TestMetaDataAgentState>
{
    public TestMetaDataAgentState State { get; } = new TestMetaDataAgentState();
    private readonly List<MetaDataStateLogEvent> _raisedEvents = new();

    // Implementation of IMetaDataStateGAgent required methods
    public void RaiseEvent(MetaDataStateLogEvent @event)
    {
        // Store the event for testing verification
        _raisedEvents.Add(@event);
        // Apply the event to state for testing
        State.Apply(@event);
    }

    public Task ConfirmEvents()
    {
        // In a real Orleans implementation, this would confirm events to storage
        // For testing, we just return a completed task
        return Task.CompletedTask;
    }

    public TestMetaDataAgentState GetState()
    {
        return State;
    }
    
    IMetaDataState IMetaDataStateGAgent.GetState() => State;

    public GrainId GetGrainId()
    {
        // Return a test grain ID
        return GrainId.Create("test", "test-metadata-agent");
    }

    // Helper methods for testing
    public List<MetaDataStateLogEvent> GetRaisedEvents()
    {
        return new List<MetaDataStateLogEvent>(_raisedEvents);
    }

    public void ClearRaisedEvents()
    {
        _raisedEvents.Clear();
    }
}