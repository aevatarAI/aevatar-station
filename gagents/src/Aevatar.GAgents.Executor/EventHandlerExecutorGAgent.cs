using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Concurrency;

namespace Aevatar.GAgents.Executor;

[GenerateSerializer]
public class EventHandlerExecutorGAgentState : StateBase;

[GenerateSerializer]
public class EventHandlerExecutorStateLogEvent : StateLogEventBase<EventHandlerExecutorStateLogEvent>;

/// <summary>
/// This GAgent will keep instantiation for GAgentExecutor in silo.
/// </summary>
public interface IEventHandlerExecutorGAgent : IStateGAgent<EventHandlerExecutorGAgentState>
{
    Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, EventBase @event, Type? expectedResultType = null);
    Task<string> ExecuteGAgentEventHandler(GrainId grainId, EventBase @event, Type? expectedResultType = null);
    Task<string> ExecuteGAgentEventHandler(GrainType grainType, EventBase @event, Type? expectedResultType = null);

    Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, string eventTypeName, string eventJson,
        Type? expectedResultType = null);

    Task<string> ExecuteGAgentEventHandler(GrainId grainId, string eventTypeName, string eventJson,
        Type? expectedResultType = null);

    Task<string> ExecuteGAgentEventHandler(GrainType grainType, string eventTypeName, string eventJson,
        Type? expectedResultType = null);
}

[Reentrant]
[GAgent("event-handler-executor", "aevatar")]
public class EventHandlerExecutorGAgent :
    GAgentBase<EventHandlerExecutorGAgentState, EventHandlerExecutorStateLogEvent>, IEventHandlerExecutorGAgent
{
    private IGAgentExecutor? _gAgentExecutor;

    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        _gAgentExecutor = ServiceProvider.GetRequiredService<IGAgentExecutor>();
        return base.OnGAgentActivateAsync(cancellationToken);
    }

    public Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, EventBase @event, Type? expectedResultType = null)
    {
        return _gAgentExecutor!.ExecuteGAgentEventHandler(gAgent, @event, expectedResultType);
    }

    public Task<string> ExecuteGAgentEventHandler(GrainId grainId, EventBase @event, Type? expectedResultType = null)
    {
        return _gAgentExecutor!.ExecuteGAgentEventHandler(grainId, @event, expectedResultType);
    }

    public Task<string> ExecuteGAgentEventHandler(GrainType grainType, EventBase @event,
        Type? expectedResultType = null)
    {
        return _gAgentExecutor!.ExecuteGAgentEventHandler(grainType, @event, expectedResultType);
    }

    public Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, string eventTypeName, string eventJson,
        Type? expectedResultType = null)
    {
        return _gAgentExecutor!.ExecuteGAgentEventHandler(gAgent, eventTypeName, eventJson, expectedResultType);
    }

    public Task<string> ExecuteGAgentEventHandler(GrainId grainId, string eventTypeName, string eventJson,
        Type? expectedResultType = null)
    {
        return _gAgentExecutor!.ExecuteGAgentEventHandler(grainId, eventTypeName, eventJson, expectedResultType);
    }

    public Task<string> ExecuteGAgentEventHandler(GrainType grainType, string eventTypeName, string eventJson,
        Type? expectedResultType = null)
    {
        return _gAgentExecutor!.ExecuteGAgentEventHandler(grainType, eventTypeName, eventJson, expectedResultType);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Event Handler Executor GAgent - Executes event handlers within Orleans grain context");
    }
}