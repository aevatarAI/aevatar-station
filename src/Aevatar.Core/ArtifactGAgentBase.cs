using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.Core;

[GAgent]
public class ArtifactGAgentBase<TArtifact, TState, TStateLogEvent>(ILogger logger)
    : ArtifactGAgentBase<TArtifact, TState, TStateLogEvent, EventBase>(logger)
    where TArtifact : IArtifact<TState, TStateLogEvent>
    where TState : StateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>;

[GAgent]
public class ArtifactGAgentBase<TArtifact, TState, TStateLogEvent, TEvent>
    : GAgentBase<TState, TStateLogEvent, TEvent>
    where TArtifact : IArtifact<TState, TStateLogEvent>
    where TState : StateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
{
    private readonly TArtifact _artifact;

    public ArtifactGAgentBase(ILogger logger) : base(logger)
    {
        _artifact = ActivatorUtilities.CreateInstance<TArtifact>(ServiceProvider);
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(_artifact.GetDescription());
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);
        await UpdateObserverList(_artifact.GetType());
    }

    protected override void GAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        base.GAgentTransitionState(state, @event);
        _artifact.TransitionState(state, @event);
    }

    public override async Task<List<Type>?> GetAllSubscribedEventsAsync(bool includeBaseHandlers = false)
    {
        var gAgentEvents = await base.GetAllSubscribedEventsAsync(includeBaseHandlers);
        var artifactEventHandlerMethods = GetEventHandlerMethods(_artifact.GetType());
        var handlingTypes = artifactEventHandlerMethods
            .Select(m => m.GetParameters().First().ParameterType);
        return handlingTypes.Concat(gAgentEvents!).ToList();
    }
}