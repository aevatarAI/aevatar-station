namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator;

[GenerateSerializer]
public class WorkUnitInfo
{
    [Id(0)] public string GrainId { get; set; }
    [Id(1)] public string NextGrainId { get; set; }
    [Id(2)] public WorkerUnitStatusEnum UnitStatusEnum { get; set; }
    [Id(3)] public Dictionary<string,string> ExtendedData { get; set; } = new();
}