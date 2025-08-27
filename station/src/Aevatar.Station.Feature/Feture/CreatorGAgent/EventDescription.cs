namespace Aevatar.Station.Feature.CreatorGAgent;

[GenerateSerializer]
public class EventDescription
{
    [Id(0)] public Type EventType { get; set; }
    [Id(1)] public string Description { get; set; }
}
