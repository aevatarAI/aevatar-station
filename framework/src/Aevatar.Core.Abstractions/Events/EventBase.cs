// ReSharper disable once CheckNamespace
namespace Aevatar.Core.Abstractions;

[GenerateSerializer]
public abstract class EventBase
{
    [Id(0)] public Guid? CorrelationId { get; set; }

    [Id(1)] public GrainId PublisherGrainId { get; set; }

    [Id(2)] public EventDirection Direction { get; set; } = EventDirection.Down;

    [Id(3)] public bool ShouldStopPropagation { get; set; } = false;
    ///stream constraint: max hop count
    [Id(4)] public int MaxHopCount { get; set; } = -1;

    [Id(5)] public int CurrentHopCount { get; set; } = 0;

    [Id(6)] public int MinHopCount { get; set; } = -1;

    [Id(7)] public string Message { get; set; } = string.Empty;

    [Id(8)] public List<GrainId> Publishers { get; set; } = [];
}

public enum EventDirection
{
    Up,
    
    Down,

    //The event will be forwarded to parent and its child (publiser's siblings)
    UpThenDown,

    // Make sure you apply the event hopping constraints 
    // before using this direction or you will get into infinite loop.
    Bidirectional,
}