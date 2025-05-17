namespace Aevatar.Station.Feature.CreatorGAgent;

[GenerateSerializer]
public class UpdateAgentInput
{
    [Id(0)] public string Name { get; set; }
    [Id(1)] public string Properties { get; set; }
}