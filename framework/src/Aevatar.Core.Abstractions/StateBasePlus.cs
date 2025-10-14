namespace Aevatar.Core.Abstractions;

[GenerateSerializer]
public class StateBasePlus : BroadcastGState
{
    [Id(0)] public List<GrainId> Children { get; set; } = [];
    [Id(2)] public string? GAgentCreator { get; set; }
    [Id(3)] public List<GrainId> Parents { get; set; } = [];

    public new void Apply(StateLogEventBase @stateLogEvent)
    {
        // Just to avoid exception on GAgentBasePlus.TransitionState.
        base.Apply(@stateLogEvent);
    }
}
