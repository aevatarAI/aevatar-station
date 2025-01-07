namespace Aevatar.Domain.Grains.Event;

[GenerateSerializer]
public class BasicEvent
{
    [Id(0)] public Guid Id { get; set; } = Guid.NewGuid();
    [Id(1)] public string Description { get; set; }
    [Id(2)] public string AgentTopic { get; set; }
    [Id(3)] public List<Guid> Downstreams { get; set; } = new List<Guid>();
    
    [Id(4)] public string Content { get; set; }
    
    [Id(5)] public EventStateEnum State { get; set; }

}