// ABOUTME: This file provides a mock implementation of IMetaDataStateGAgent for testing
// ABOUTME: Tracks raised events and state changes for verification in unit tests

using Aevatar.Core.Abstractions;
using Aevatar.MetaData;
using Aevatar.MetaData.Events;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Mock implementation of IMetaDataStateGAgent for testing purposes.
/// </summary>
public class MockEventRaiser : IMetaDataStateGAgent<TestMetaDataState>
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
    
    Task<IMetaDataState> IMetaDataStateGAgent.GetState() => Task.FromResult<IMetaDataState>(_state);

    public Task<GrainId> GetGrainIdAsync()
    {
        return Task.FromResult(GrainId.Create("test", "test-grain-id"));
    }

    // IGAgent interface implementations (required by IMetaDataStateGAgent)
    public Task ActivateAsync() => Task.CompletedTask;
    public Task<string> GetDescriptionAsync() => Task.FromResult("Mock test agent");
    public Task RegisterAsync(IGAgent agent) => Task.CompletedTask;
    public Task SubscribeToAsync(IGAgent agent) => Task.CompletedTask;
    public Task UnsubscribeFromAsync(IGAgent agent) => Task.CompletedTask;
    public Task UnregisterAsync(IGAgent agent) => Task.CompletedTask;
    public Task<List<Type>?> GetAllSubscribedEventsAsync(bool includeBaseHandlers = false) => Task.FromResult<List<Type>?>(new List<Type>());
    public Task<List<GrainId>> GetChildrenAsync() => Task.FromResult(new List<GrainId>());
    public Task<GrainId> GetParentAsync() => Task.FromResult(default(GrainId));
    public Task<Type?> GetConfigurationTypeAsync() => Task.FromResult<Type?>(null);
    public Task ConfigAsync(ConfigurationBase configuration) => Task.CompletedTask;
}