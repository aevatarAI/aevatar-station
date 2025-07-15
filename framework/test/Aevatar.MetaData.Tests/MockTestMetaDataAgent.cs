// ABOUTME: This file defines a mock test agent for simplified integration testing
// ABOUTME: Works without Orleans context for testing IMetaDataStateGAgent interface functionality

using Aevatar.Core.Abstractions;
using Aevatar.MetaData;
using Aevatar.MetaData.Enums;
using Aevatar.MetaData.Events;

namespace Aevatar.MetaData.Tests;

/// <summary>
/// Mock test agent for simplified integration testing without Orleans dependencies.
/// Tests the IMetaDataStateGAgent interface implementation in isolation.
/// </summary>
public class MockTestMetaDataAgent : ITestMetaDataAgent
{
    private readonly TestMetaDataAgentState _state;
    private readonly List<TestMetaDataAgentEvent> _raisedEvents;
    private readonly MockMetaDataHelper _metaDataHelper;
    private int _confirmEventsCalls = 0;

    public MockTestMetaDataAgent()
    {
        _state = new TestMetaDataAgentState();
        _raisedEvents = new List<TestMetaDataAgentEvent>();
        _metaDataHelper = new MockMetaDataHelper(this);
    }

    public Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Mock test agent for metadata operations without Orleans dependencies");
    }

    public Task<TestMetaDataAgentState> GetStateAsync()
    {
        return Task.FromResult(_state);
    }

    public async Task HandleTestEventAsync(TestMetaDataAgentEvent @event)
    {
        _raisedEvents.Add(@event);
        
        // Apply event to state
        _state.TestEventCount++;
        if (!string.IsNullOrEmpty(@event.TestMessage))
        {
            _state.TestMessages.Add(@event.TestMessage);
        }
        _state.LastActivity = DateTime.UtcNow;
        
        _confirmEventsCalls++;
        await Task.CompletedTask;
    }

    public Task<int> GetTestEventCountAsync()
    {
        return Task.FromResult(_state.TestEventCount);
    }

    public Task<List<string>> GetTestMessagesAsync()
    {
        return Task.FromResult(new List<string>(_state.TestMessages));
    }

    public async Task ClearTestDataAsync()
    {
        _raisedEvents.Clear();
        _state.TestEventCount = 0;
        _state.TestMessages.Clear();
        _confirmEventsCalls++;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the metadata helper for testing IMetaDataStateGAgent interface
    /// </summary>
    public IMetaDataStateGAgent<TestMetaDataAgentState> GetMetaDataHelper()
    {
        return _metaDataHelper;
    }

    // IGAgent interface methods (mock implementations)
    public Task ActivateAsync() => Task.CompletedTask;
    public Task RegisterAsync(IGAgent agent) => Task.CompletedTask;
    public Task SubscribeToAsync(IGAgent agent) => Task.CompletedTask;
    public Task UnsubscribeFromAsync(IGAgent agent) => Task.CompletedTask;
    public Task UnregisterAsync(IGAgent agent) => Task.CompletedTask;
    public Task<List<Type>?> GetAllSubscribedEventsAsync(bool includeBaseHandlers = false) => 
        Task.FromResult<List<Type>?>(new List<Type>());
    public Task<List<GrainId>> GetChildrenAsync() => 
        Task.FromResult(new List<GrainId>());
    public Task<GrainId> GetParentAsync() => Task.FromResult(default(GrainId));
    public Task<Type?> GetConfigurationTypeAsync() => Task.FromResult<Type?>(null);
    public Task ConfigAsync(ConfigurationBase configuration) => Task.CompletedTask;

    // Internal state for testing
    internal TestMetaDataAgentState InternalState => _state;
    internal List<TestMetaDataAgentEvent> RaisedEvents => _raisedEvents;
    internal int ConfirmEventsCalls => _confirmEventsCalls;
}

/// <summary>
/// Mock helper class that implements IMetaDataStateGAgent for testing without Orleans.
/// </summary>
internal class MockMetaDataHelper : IMetaDataStateGAgent<TestMetaDataAgentState>
{
    private readonly MockTestMetaDataAgent _agent;

    public MockMetaDataHelper(MockTestMetaDataAgent agent)
    {
        _agent = agent;
    }

    public void RaiseEvent(MetaDataStateLogEvent @event)
    {
        // For testing purposes, we'll simulate the event processing
        var testEvent = new TestMetaDataAgentEvent
        {
            Action = @event.GetType().Name,
            TestMessage = $"Metadata event: {@event.GetType().Name}"
        };
        
        _agent.RaisedEvents.Add(testEvent);
    }

    public Task ConfirmEvents()
    {
        // Mock implementation - just track the call
        return Task.CompletedTask;
    }

    public TestMetaDataAgentState GetState()
    {
        return _agent.InternalState;
    }
    
    IMetaDataState IMetaDataStateGAgent.GetState() => _agent.InternalState;

    public GrainId GetGrainId()
    {
        // Return a mock GrainId for testing
        return GrainId.Create("MockTestAgent", Guid.NewGuid().ToString());
    }
}