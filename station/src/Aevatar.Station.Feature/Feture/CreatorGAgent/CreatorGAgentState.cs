namespace Aevatar.Station.Feature.CreatorGAgent;


[GenerateSerializer]
public class CreatorGAgentState : GroupAgentState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid UserId { get; set; }
    [Id(3)] public string AgentType { get; set; }
    [Id(4)] public string Name { get; set; }
    [Id(5)] public string Properties { get; set; }
    [Id(6)] public GrainId BusinessAgentGrainId { get; set; }
    [Id(7)] public List<EventDescription> EventInfoList { get; set; } = new();
    [Id(8)] public DateTime CreateTime { get; set; } 
    [Id(9)] public string FormattedBusinessAgentGrainId { get; set; }
}
