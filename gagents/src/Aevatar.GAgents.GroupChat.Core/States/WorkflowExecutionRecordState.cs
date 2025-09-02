using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;

namespace Aevatar.GAgents.GroupChat.Core.States;

[GenerateSerializer]
public class WorkflowExecutionRecordState : StateBase
{
    [Id(0)] 
    public Guid WorkflowId { get; set; }
    [Id(1)] 
    public long RoundId { get; set; }
    [Id(2)] 
    public List<WorkUnitInfo> WorkUnitInfos { get; set; } = new ();
    [Id(3)]
    public DateTime StartTime { get; set; }
    [Id(4)]
    public DateTime? EndTime { get; set; }
    [Id(5)]
    public WorkflowExecutionStatus Status { get; set; }
    [Id(6)]
    public string? InitContent { get; set; }

    [Id(7)] public List<WorkUnitExecutionRecord> WorkUnitRecords { get; set; } = new();
}

[GenerateSerializer]
public class WorkUnitExecutionRecord
{
    [Id(0)]
    public string WorkUnitGrainId { get; set; }
    [Id(1)]
    public DateTime StartTime { get; set; }
    [Id(2)]
    public DateTime? EndTime { get; set; }
    [Id(3)]
    public WorkflowExecutionStatus Status { get; set; }
    [Id(4)]
    public string InputData { get; set; }
    [Id(5)]
    public string OutputData { get; set; }
}