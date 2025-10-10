using Orleans;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Placement;

using Orleans.Streams;
using Orleans.Concurrency;
using System.Diagnostics;
using Aevatar.Core.Interception;

namespace E2E.Grains;

/// <summary>
/// State for test agents to track received events using Plus enhanced functionality
/// </summary>
[GenerateSerializer]
public class EventForwardingTestAgentState : StateBasePlus
{
    [Id(0)] public List<TestEventReceived> ReceivedEvents { get; set; } = new();
    [Id(1)] public string AgentName { get; set; } = string.Empty;
    [Id(2)] public string AgentLevel { get; set; } = string.Empty; // Root, Middle, Leaf
}

/// <summary>
/// Record of a received event for tracking
/// </summary>
[GenerateSerializer]
public class TestEventReceived
{
    [Id(0)] public Guid TestId { get; set; }
    [Id(1)] public string EventType { get; set; } = string.Empty;
    [Id(2)] public string TestMessage { get; set; } = string.Empty;
    [Id(3)] public EventDirection Direction { get; set; }
    [Id(4)] public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    [Id(5)] public string OriginAgentId { get; set; } = string.Empty;
    [Id(6)] public List<string> ForwardingPath { get; set; } = new();
    [Id(7)] public int MaxHopCount { get; set; } = -1;
    [Id(8)] public int MinHopCount { get; set; } = -1;
    [Id(9)] public int CurrentHopCount { get; set; } = 0;
}

/// <summary>
/// Base state log event for test agents
/// </summary>
[GenerateSerializer]
public class EventForwardingTestAgentStateLogEvent : StateLogEventBase<EventForwardingTestAgentStateLogEvent>
{
    [Id(0)] public string ActionType { get; set; } = string.Empty;
    [Id(1)] public string AgentName { get; set; } = string.Empty;
    [Id(2)] public string AgentLevel { get; set; } = string.Empty;
    [Id(3)] public TestEventReceived? EventReceived { get; set; }
}

/// <summary>
/// Concrete test event class for all event forwarding scenarios
/// </summary>
[GenerateSerializer]
public class TestEvent : EventBase
{
    [Id(0)] public string TestMessage { get; set; } = string.Empty;
    [Id(1)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [Id(2)] public Guid TestId { get; set; } = Guid.NewGuid();
    [Id(3)] public string OriginAgentId { get; set; } = string.Empty;
    [Id(4)] public List<string> ForwardingPath { get; set; } = new();
    
    public TestEvent()
    {
        // Default constructor for serialization
    }
    
    public TestEvent(EventDirection direction, string message, int maxHops = -1, int minHops = -1)
    {
        Direction = direction;
        TestMessage = message;
        MaxHopCount = maxHops;
        MinHopCount = minHops;
    }
    
    public TestEvent(EventDirection direction, string message, int hopCount)
    {
        Direction = direction;
        TestMessage = message;
        MaxHopCount = hopCount;
        MinHopCount = hopCount;
    }
}

/// <summary>
/// Interface for test agents using Plus enhanced functionality
/// </summary>
public interface IEventForwardingTestAgent : IGAgentPlus
{
    Task InitializeAsync(string agentName, string agentLevel);
    Task<List<TestEventReceived>> GetReceivedEventsAsync();
    Task<int> GetReceivedEventCountAsync();
    Task<List<TestEventReceived>> GetReceivedEventsByTypeAsync(string eventType);
    Task<List<TestEventReceived>> GetReceivedEventsByDirectionAsync(EventDirection direction);
    Task ClearReceivedEventsAsync();
    Task<string> GetAgentInfoAsync();
}

/// <summary>
/// Test agent that tracks event forwarding using Plus enhanced functionality
/// </summary>
[KeepAlive] 
[SiloNamePatternPlacement("User")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class EventForwardingTestAgent : GAgentBasePlus<EventForwardingTestAgentState, EventForwardingTestAgentStateLogEvent, TestEvent, ConfigurationBase>, IEventForwardingTestAgent
{
    private readonly ILogger<EventForwardingTestAgent> _logger;

    public EventForwardingTestAgent(ILogger<EventForwardingTestAgent> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(string agentName, string agentLevel)
    {
        RaiseEvent(new EventForwardingTestAgentStateLogEvent
        {
            ActionType = "Initialize",
            AgentName = agentName,
            AgentLevel = agentLevel
        });
        await ConfirmEvents();

        _logger.LogInformation("Initialized test agent: {AgentName} at level {AgentLevel}", agentName, agentLevel);
    }

    public Task<List<TestEventReceived>> GetReceivedEventsAsync()
    {
        return Task.FromResult(State.ReceivedEvents.ToList());
    }

    public Task<int> GetReceivedEventCountAsync()
    {
        return Task.FromResult(State.ReceivedEvents.Count);
    }

    public Task<List<TestEventReceived>> GetReceivedEventsByTypeAsync(string eventType)
    {
        var filteredEvents = State.ReceivedEvents.Where(e => e.EventType == eventType).ToList();
        return Task.FromResult(filteredEvents);
    }

    public Task<List<TestEventReceived>> GetReceivedEventsByDirectionAsync(EventDirection direction)
    {
        var filteredEvents = State.ReceivedEvents.Where(e => e.Direction == direction).ToList();
        return Task.FromResult(filteredEvents);
    }

    public async Task ClearReceivedEventsAsync()
    {
        RaiseEvent(new EventForwardingTestAgentStateLogEvent
        {
            ActionType = "Clear"
        });
        await ConfirmEvents();
        _logger.LogInformation("Cleared received events for agent {AgentName}", State.AgentName);
    }


    protected override async Task<bool> OnEventForwardingEventHandlerAsync(TestEvent @event)
    {
        // Record the event reception when we receive events through the event handler
        var eventReceived = new TestEventReceived
        {
            TestId = @event.TestId,
            EventType = @event.GetType().Name,
            TestMessage = @event.TestMessage,
            Direction = @event.Direction,
            ReceivedAt = DateTime.UtcNow,
            OriginAgentId = @event.OriginAgentId,
            ForwardingPath = @event.ForwardingPath.ToList(),
            MaxHopCount = @event.MaxHopCount,
            MinHopCount = @event.MinHopCount,
            CurrentHopCount = @event.CurrentHopCount
        };

        // Add this agent to the forwarding path
        @event.ForwardingPath.Add($"{State.AgentName}({State.AgentLevel})");

        RaiseEvent(new EventForwardingTestAgentStateLogEvent
        {
            ActionType = "RecordEvent",
            EventReceived = eventReceived
        });
        await ConfirmEvents();

        _logger.LogInformation("Agent {AgentName}({AgentLevel}) received {EventType} from {Origin} with direction {Direction} and HopCount {HopCount}. Path: {Path}",
            State.AgentName, State.AgentLevel, @event.GetType().Name, @event.OriginAgentId,
            @event.Direction, @event.CurrentHopCount, string.Join(" -> ", @event.ForwardingPath));

        // Call base implementation to handle forwarding
        return await base.OnEventForwardingEventHandlerAsync(@event);
    }


    public Task<string> GetAgentInfoAsync()
    {
        return Task.FromResult($"{State.AgentName}({State.AgentLevel})");
    }

    protected override void GAgentTransitionState(EventForwardingTestAgentState state, StateLogEventBase<EventForwardingTestAgentStateLogEvent> @event)
    {
        if (@event is EventForwardingTestAgentStateLogEvent testEvent)
        {
            switch (testEvent.ActionType)
            {
                case "Initialize":
                    state.AgentName = testEvent.AgentName;
                    state.AgentLevel = testEvent.AgentLevel;
                    break;
                case "RecordEvent":
                    if (testEvent.EventReceived != null)
                        state.ReceivedEvents.Add(testEvent.EventReceived);
                    break;
                case "Clear":
                    state.ReceivedEvents.Clear();
                    break;
            }
        }

        base.GAgentTransitionState(state, @event);
    }
}
