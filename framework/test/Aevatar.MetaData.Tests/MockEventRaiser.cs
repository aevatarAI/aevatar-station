// ABOUTME: This file provides a mock implementation of IMetaDataStateEventRaiser for testing
// ABOUTME: Tracks raised events and state changes for verification in unit tests

using Aevatar.MetaData;
using Aevatar.MetaData.Events;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Mock implementation of IMetaDataStateEventRaiser for testing purposes.
/// </summary>
public class MockEventRaiser : IMetaDataStateEventRaiser<TestMetaDataState>
{
    private readonly TestMetaDataState _state;
    private readonly List<MetaDataStateLogEvent> _raisedEvents = new();
    private readonly List<DateTime> _confirmEventsTimes = new();

    public MockEventRaiser(TestMetaDataState state)
    {
        _state = state;
    }

    public List<MetaDataStateLogEvent> RaisedEvents => _raisedEvents;
    public List<DateTime> ConfirmEventsTimes => _confirmEventsTimes;

    public void RaiseEvent(MetaDataStateLogEvent @event)
    {
        _raisedEvents.Add(@event);
        // Apply the event to state for testing
        _state.Apply(@event);
    }

    public Task ConfirmEvents()
    {
        _confirmEventsTimes.Add(DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public TestMetaDataState GetState()
    {
        return _state;
    }

    public GrainId GetGrainId()
    {
        return GrainId.Create("test", "test-grain-id");
    }
}