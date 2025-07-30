namespace Aevatar.Core.Abstractions;

[GenerateSerializer]
public class StateBase : BroadcastGState
{
    [Id(0)] public List<GrainId> Children { get; set; } = [];
    [Id(1)] public GrainId? Parent { get; set; }
    [Id(2)] public string? GAgentCreator { get; set; }

    public new void Apply(StateLogEventBase @stateLogEvent)
    {
        // Just to avoid exception on GAgentBase.TransitionState.
        base.Apply(@stateLogEvent);
    }
}