using Aevatar.Core.Abstractions;
using Volo.Abp.DependencyInjection;

namespace Aevatar.ArtifactGAgents;

[GenerateSerializer]
public class GeneratedGAgentState : StateBase;

[GenerateSerializer]
public class GeneratedStateLogEvent : StateLogEventBase<GeneratedStateLogEvent>;

public interface IMyArtifact : IArtifact<GeneratedGAgentState, GeneratedStateLogEvent>
{
    string TestMethod();
}

[GenerateSerializer]
public class MyArtifactEvent : EventBase
{
    [Id(0)] public Dictionary<string, object> Content { get; set; }
}

public class MyArtifact : IMyArtifact, ISingletonDependency
{
    public string GetDescription() => "MyArtifact Description, this is for testing.";

    [EventHandler]
    public async Task HandleEventAsync(MyArtifactEvent eventData)
    {
        await Task.Yield();
    }

    public string TestMethod()
    {
        return "Test";
    }

    public void TransitionState(GeneratedGAgentState state, StateLogEventBase<GeneratedStateLogEvent> stateLogEvent)
    {
        /* custom logic */
    }
}