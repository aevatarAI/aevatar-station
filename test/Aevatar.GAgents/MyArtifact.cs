using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents;

[GenerateSerializer]
public class GeneratedGAgentState : StateBase;

[GenerateSerializer]
public class GeneratedStateLogEvent : StateLogEventBase<GeneratedStateLogEvent>;

public class MyArtifact : IArtifact<GeneratedGAgentState, GeneratedStateLogEvent>
{
    public string GetDescription() => "MyArtifact Description";
    public void TransitionState(GeneratedGAgentState state, StateLogEventBase<GeneratedStateLogEvent> stateLogEvent) { /* custom logic */ }
}