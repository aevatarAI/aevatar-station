using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Core;
using Aevatar.GAgents.Workflow.Core.Models;

namespace Aevatar.GAgents.Workflow.Core.States;

/// <summary>
/// Updated WorkflowCoordinatorState using consolidated WorkUnitInfo and removing Term complexity
/// as per design document - uses direct AgentId correlation instead of Term mapping
/// </summary>
[GenerateSerializer]
public class WorkflowCoordinatorStatePlus : BusinessAgentState
{
    [Id(0)] public Guid BlackboardId { get; set; }
    // REMOVED: [Id(1)] public long Term { get; set; } = 0;                 // Unnecessary complexity 
    [Id(2)] public List<WorkUnitInfo> CurrentWorkUnitInfos { get; set; } = new List<WorkUnitInfo>();    // Enhanced with UI data
    // REMOVED: [Id(3)] public Dictionary<long, string> TermToWorkUnitGrainId { get; set; }  // Unnecessary mapping
    [Id(4)] public WorkflowCoordinatorStatus WorkflowStatus { get; set; } = WorkflowCoordinatorStatus.Pending;
    [Id(5)] public List<WorkUnitInfo> BackupWorkUnitInfos { get; set; } = new List<WorkUnitInfo>();
    [Id(6)] public DateTime? LastRunningTime { get; set; }
    [Id(7)] public string? Content { get; set; } = null;
    [Id(8)] public bool EnableRunRecord { get; set; }
    [Id(9)] public Guid CurrentExecutionRecordId { get; set; }
    [Id(10)] public long RoundId { get; set; }
    
    /// <summary>
    /// Dictionary mapping execution names to their execution record IDs for persistent tracking
    /// </summary>
    [Id(11)] public Dictionary<string, Guid> ExecutionRecords { get; set; } = new();
    
    /// <summary>
    /// Name of the currently running execution
    /// </summary>
    [Id(12)] public string? CurrentExecutionName { get; set; }

    // Updated method using AgentId as GrainId string instead of Guid (signature changed from design document)
    public WorkUnitInfo? GetWorkUnit(string agentId)
    {
        return CurrentWorkUnitInfos.FirstOrDefault(w => w.AgentId == agentId);
    }

    public bool CheckAllWorkUnitFinished()
    {
        return CurrentWorkUnitInfos.Exists(
            e => e.UnitStatusEnum is WorkerUnitStatusEnum.Pending or WorkerUnitStatusEnum.InProgress) == false;
    }

    public bool CheckWorkUnitCanProgress(string agentId)
    {
        var workUnitInfo = CurrentWorkUnitInfos.FindAll(f => f.AgentId == agentId);
        if (workUnitInfo.Count == 0)
        {
            return false;
        }

        if (workUnitInfo.Exists(e => e.UnitStatusEnum != WorkerUnitStatusEnum.Pending))
        {
            return false;
        }

        var preWorkUnits = CurrentWorkUnitInfos.FindAll(f => f.NextAgentId == agentId);
        return preWorkUnits.Exists(e => e.UnitStatusEnum != WorkerUnitStatusEnum.Finished) == false;
    }

    public List<string> GetUpStreamGrainIds(string currentAgentId)
    {
        return CurrentWorkUnitInfos.Where(w => w.NextAgentId == currentAgentId).Select(s => s.AgentId).ToList();
    }

    public List<string> GetTopUpStreamGrainIds()
    {
        var downStreamAgentIds = CurrentWorkUnitInfos.Where(w => !string.IsNullOrEmpty(w.NextAgentId)).Select(s => s.NextAgentId);
        return CurrentWorkUnitInfos.Where(w => downStreamAgentIds.Contains(w.AgentId) == false).Select(s => s.AgentId).ToList();
    }

    // Updated existing method to work with new consolidated fields (signature changed from design document)
    public List<string> GetDownStreamGrainIds(string currentAgentId)
    {
        var downStream = CurrentWorkUnitInfos.FindAll(f => f.AgentId == currentAgentId);
        return (from item in downStream where !string.IsNullOrEmpty(item.NextAgentId) select item.NextAgentId)
               .Distinct()
               .ToList();
    }

    // REMOVED: GetWorkUnitFromTerm method - no longer needed without Term system

    public List<string> GetAllWorkerUnitGrainIds()
    {
        return CurrentWorkUnitInfos.Select(s => s.AgentId).Distinct().ToList();
    }
}