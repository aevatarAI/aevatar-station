using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator;

[GenerateSerializer]
public class WorkflowCoordinatorState : StateBase
{
    [Id(0)] public Guid BlackboardId { get; set; }
    [Id(1)] public long Term { get; set; } = 0;
    [Id(2)] public List<WorkUnitInfo> CurrentWorkUnitInfos { get; set; } = new List<WorkUnitInfo>();
    [Id(3)] public Dictionary<long, string> TermToWorkUnitGrainId { get; set; } = new Dictionary<long, string>();
    [Id(4)] public WorkflowCoordinatorStatus WorkflowStatus { get; set; } = WorkflowCoordinatorStatus.Pending;
    [Id(5)] public List<WorkUnitInfo> BackupWorkUnitInfos { get; set; } = new List<WorkUnitInfo>();
    [Id(6)] public DateTime? LastRunningTime { get; set; }
    [Id(7)] public string? Content { get; set; } = null;
    [Id(8)] public bool EnableRunRecord { get; set; }
    [Id(9)] public Guid CurrentExecutionRecordId { get; set; }
    [Id(10)] public long RoundId { get; set; }

    public WorkUnitInfo? GetWorkUnit(string workUnitGrainId)
    {
        return CurrentWorkUnitInfos.FirstOrDefault(f => f.GrainId == workUnitGrainId);
    }

    public bool CheckAllWorkUnitFinished()
    {
        return CurrentWorkUnitInfos.Exists(
            e => e.UnitStatusEnum is WorkerUnitStatusEnum.Pending or WorkerUnitStatusEnum.InProgress) == false;
    }

    public bool CheckWorkUnitCanProgress(string workUnitGrainId)
    {
        var workUnitInfo = CurrentWorkUnitInfos.FindAll(f => f.GrainId == workUnitGrainId);
        if (workUnitInfo.Count == 0)
        {
            return false;
        }

        if (workUnitInfo.Exists(e => e.UnitStatusEnum != WorkerUnitStatusEnum.Pending))
        {
            return false;
        }

        var preWorkUnits = CurrentWorkUnitInfos.FindAll(f => f.NextGrainId == workUnitGrainId);
        return preWorkUnits.Exists(e => e.UnitStatusEnum != WorkerUnitStatusEnum.Finished) == false;
    }

    public List<string> GetUpStreamGrainIds(string currentGrainId)
    {
        return CurrentWorkUnitInfos.Where(w => w.NextGrainId == currentGrainId).Select(s => s.GrainId).ToList();
    }

    public List<string> GetTopUpStreamGrainIds()
    {
        var downStreamGrainIds =
            CurrentWorkUnitInfos.Where(w => !w.NextGrainId.IsNullOrEmpty()).Select(s => s.NextGrainId);
        return CurrentWorkUnitInfos.Where(w => downStreamGrainIds.Contains(w.GrainId) == false).Select(s => s.GrainId)
            .ToList();
    }

    public List<string> GetDownStreamGrainIds(string currentGrainId)
    {
        var downStream = CurrentWorkUnitInfos.FindAll(f => f.GrainId == currentGrainId);

        return (from item in downStream where item.NextGrainId.IsNullOrEmpty() == false select item.NextGrainId)
            .Distinct()
            .ToList();
    }

    public WorkUnitInfo? GetWorkUnitFromTerm(long termId)
    {
        return TermToWorkUnitGrainId.TryGetValue(termId, out var result) == false
            ? null
            : CurrentWorkUnitInfos.First(f => f.GrainId == result);
    }

    public List<string> GetAllWorkerUnitGrainIds()
    {
        return CurrentWorkUnitInfos.Select(s => s.GrainId).Distinct().ToList();
    }
}