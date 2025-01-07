namespace Aevatar.Domain.Grains.Event;
[GenerateSerializer]
public class EventFlowTemplateState
{
    [Id(0)] public Guid Id { get; set; } = Guid.NewGuid();
    [Id(1)] public string Description { get; set; }
    [Id(2)] public string AgentTopic { get; set; }
    [Id(3)] public Guid? Upstream { get; set; }
    [Id(4)]public List<Guid> Downstreams { get; set; } = new List<Guid>();
}
