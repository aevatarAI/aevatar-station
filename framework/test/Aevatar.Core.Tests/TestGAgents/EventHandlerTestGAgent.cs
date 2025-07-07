using Aevatar.Core.Abstractions;
using Aevatar.Core.Tests.TestEvents;
using Aevatar.Core.Tests.TestStateLogEvents;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core.Tests.TestGAgents;

public interface IEventHandlerTestGAgent : IStateGAgent<EventHandlerTestGAgentState>
{
    Task TestPublicRaiseEvent(EventHandlerTestStateLogEvent @event);
    Task TestPublicRaiseEventAndConfirm(EventHandlerTestStateLogEvent @event);
    Task TestPublicRaiseEventWithNull();
    Task TestPublicConfirmEventsOnly();
    Task TestPublicRaiseMultipleEvents(EventHandlerTestStateLogEvent[] events);
    Task<Task> TestPublicConfirmEventsReturnValue();
    Task TestDirectStateModification(string content);
}

[GenerateSerializer]
public class EventHandlerTestGAgentState : StateBase
{
    [Id(0)]  public List<string> Content { get; set; } = [];
    [Id(1)]  public List<string> Messages { get; set; } = [];
}

public class EventHandlerTestStateLogEvent : StateLogEventBase<EventHandlerTestStateLogEvent>;

[GAgent("eventHandlerTest", "test")]
public class EventHandlerTestGAgent : GAgentBase<EventHandlerTestGAgentState, EventHandlerTestStateLogEvent>, IEventHandlerTestGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("This GAgent is used for testing event handlers.");
    }

    // This method can be recognized as an event handler,
    // because the method name matches `HandleEventAsync`.
    public Task HandleEventAsync(NaiveTestEvent eventData)
    {
        AddContent(eventData.Greeting);
        return Task.CompletedTask;
    }

    [EventHandler]
    public Task ExecuteAsync(NaiveTestEvent eventData)
    {
        AddContent(eventData.Greeting);
        return Task.CompletedTask;
    }

    [AllEventHandler]
    public Task HandleEventAsync(EventWrapperBase eventData)
    {
        if (eventData is EventWrapper<EventBase> wrapper)
        {
            AddContent($"{wrapper.EventId}: {wrapper.Event.GetType()}");
        }

        return Task.CompletedTask;
    }

    [EventHandler]
    public Task HandleEventWithExceptionAsync(NaiveTestEvent eventData)
    {
        // By design, for testing
        throw new Exception();
    }

    private void AddContent(string content)
    {
        if (State.Content.IsNullOrEmpty())
        {
            State.Content = [];
        }

        State.Content.Add(content);
    }

    // Test methods for public RaiseEvent and ConfirmEvents
    public async Task TestPublicRaiseEvent(EventHandlerTestStateLogEvent @event)
    {
        // Test the public RaiseEvent method
        RaiseEvent(@event);
        await ConfirmEvents();
    }

    public async Task TestPublicRaiseEventAndConfirm(EventHandlerTestStateLogEvent @event)
    {
        // Test public RaiseEvent followed by public ConfirmEvents
        RaiseEvent(@event);
        await ConfirmEvents();
    }

    public async Task TestPublicRaiseEventWithNull()
    {
        // Test public RaiseEvent with null - should handle gracefully
        try
        {
            RaiseEvent(null!);
            await ConfirmEvents();
        }
        catch
        {
            // Expected behavior for null input
        }
    }

    public async Task TestPublicConfirmEventsOnly()
    {
        // Test calling public ConfirmEvents without raising events
        await ConfirmEvents();
    }

    public async Task TestPublicRaiseMultipleEvents(EventHandlerTestStateLogEvent[] events)
    {
        // Test multiple events using public methods
        foreach (var @event in events)
        {
            RaiseEvent(@event);
        }
        await ConfirmEvents();
    }

    public Task<Task> TestPublicConfirmEventsReturnValue()
    {
        // Test that public ConfirmEvents returns a Task
        var task = ConfirmEvents();
        return Task.FromResult(task);
    }

    // Event handler for ReceiveMessageTestStateLogEvent
    [EventHandler]
    protected Task HandleReceiveMessageAsync(ReceiveMessageTestStateLogEvent @event)
    {
        if (State.Messages == null)
        {
            State.Messages = [];
        }
        State.Messages.Add(@event.Message);
        return Task.CompletedTask;
    }

    // Test method that directly modifies state to verify public method functionality
    public async Task TestDirectStateModification(string content)
    {
        // This method directly modifies state to test that the public methods work
        AddContent(content);
        
        // Test that we can use the public RaiseEvent method (for event sourcing)
        var stateEvent = new EventHandlerTestStateLogEvent();
        RaiseEvent(stateEvent);
        await ConfirmEvents();
    }
}