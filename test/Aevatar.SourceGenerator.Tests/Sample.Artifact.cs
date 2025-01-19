using Aevatar.Core.Abstractions;

namespace Aevatar.SourceGenerator.Tests;

[GenerateSerializer]
public class GeneratedGAgentState : StateBase;

[GenerateSerializer]
public class GeneratedStateLogEvent : StateLogEventBase<GeneratedStateLogEvent>;

public class TestMyArtifact : IArtifact<GeneratedGAgentState, GeneratedStateLogEvent>
{
    public void TransitionState(GeneratedGAgentState state, StateLogEventBase<GeneratedStateLogEvent> stateLogEvent)
    {
        throw new NotImplementedException();
    }

    public string GetDescription()
    {
        throw new NotImplementedException();
    }
}