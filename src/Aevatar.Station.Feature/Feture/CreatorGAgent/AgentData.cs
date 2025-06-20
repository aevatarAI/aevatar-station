namespace Aevatar.Station.Feature.CreatorGAgent;

[GenerateSerializer]
public class AgentData
{
    [Id(0)] public string AgentType { get; set; }
    [Id(1)] public string Name { get; set; }
    [Id(2)] public string Properties { get; set; }
    [Id(3)] public Guid UserId { get; set; }
    [Id(4)] public GrainId BusinessAgentGrainId { get; set; }
}