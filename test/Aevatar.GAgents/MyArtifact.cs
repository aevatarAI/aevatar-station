using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents;

[GenerateSerializer]
public class GeneratedGAgentState : StateBase;

[GenerateSerializer]
public class GeneratedStateLogEvent : StateLogEventBase<GeneratedStateLogEvent>;

public interface IMyArtifact : IArtifact<GeneratedGAgentState, GeneratedStateLogEvent>
{
    
}

public class MyArtifact : IMyArtifact
{
    public string GetDescription() => "MyArtifact Description";
    public void TransitionState(GeneratedGAgentState state, StateLogEventBase<GeneratedStateLogEvent> stateLogEvent) { /* custom logic */ }
}