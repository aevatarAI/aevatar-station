using Aevatar.Core.Abstractions;

namespace Aevatar.Core.Abstractions;

public interface IArtifact<in TState, TStateLogEvent>
    where TState : StateBase
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
{
    void SetState(TState state);
    void ApplyEvent(StateLogEventBase<TStateLogEvent> stateLogEvent);
    string GetDescription();
}