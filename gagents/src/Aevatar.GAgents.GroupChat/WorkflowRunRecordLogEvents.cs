using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core.States;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator;

/// <summary>
/// Base log event for WorkflowRunRecordGAgent
/// </summary>
[GenerateSerializer]
public class WorkflowRunRecordLogEvent : StateLogEventBase<WorkflowRunRecordLogEvent>
{
}

/// <summary>
/// Log event when workflow execution starts
/// </summary>
[GenerateSerializer]
public class StartWorkflowRunLogEvent : WorkflowRunRecordLogEvent
{
    [Id(0)] public Guid WorkflowId { get; set; }
    [Id(1)] public long Term { get; set; }
    [Id(2)] public string? InitContent { get; set; }
    [Id(3)] public DateTime StartTime { get; set; }
}

/// <summary>
/// Log event when work unit execution starts
/// </summary>
[GenerateSerializer]
public class StartWorkUnitRunLogEvent : WorkflowRunRecordLogEvent
{
    [Id(0)] public string AgentGrainId { get; set; } = string.Empty;
    [Id(1)] public string AgentType { get; set; } = string.Empty;
    [Id(2)] public List<string> BeforeAgentIds { get; set; } = new();
    [Id(3)] public string InputDataJson { get; set; } = string.Empty;
    [Id(4)] public string CurrentStateJson { get; set; } = string.Empty;
    [Id(5)] public DateTime StartTime { get; set; }
}

/// <summary>
/// Log event when work unit execution finishes
/// </summary>
[GenerateSerializer]
public class FinishWorkUnitRunLogEvent : WorkflowRunRecordLogEvent
{
    [Id(0)] public string AgentGrainId { get; set; } = string.Empty;
    [Id(1)] public string OutputDataJson { get; set; } = string.Empty;
    [Id(2)] public string CurrentStateJson { get; set; } = string.Empty;
    [Id(3)] public DateTime EndTime { get; set; }
    [Id(4)] public Core.States.WorkflowExecutionStatus Status { get; set; }
    [Id(5)] public List<string> ErrorMessages { get; set; } = new();
}

/// <summary>
/// Log event when workflow execution finishes
/// </summary>
[GenerateSerializer]
public class FinishWorkflowRunLogEvent : WorkflowRunRecordLogEvent
{
    [Id(0)] public DateTime EndTime { get; set; }
    [Id(1)] public WorkflowRunStatus Status { get; set; }
}

