using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.GroupChat.Core.States;

/// <summary>
/// Workflow run record state with real-time tracking and data lineage
/// </summary>
[GenerateSerializer]
public class WorkflowRunRecordState : StateBase
{
    /// <summary>
    /// Workflow identifier
    /// </summary>
    [Id(0)] public Guid WorkflowId { get; set; }
    
    /// <summary>
    /// Round/Term identifier
    /// </summary>
    [Id(1)] public long Term { get; set; }
    
    /// <summary>
    /// Workflow start time
    /// </summary>
    [Id(2)] public DateTime StartTime { get; set; }
    
    /// <summary>
    /// Workflow end time
    /// </summary>
    [Id(3)] public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Workflow execution status
    /// </summary>
    [Id(4)] public WorkflowRunStatus Status { get; set; }
    
    /// <summary>
    /// Initial content for the workflow
    /// </summary>
    [Id(5)] public string? InitContent { get; set; }
    
    /// <summary>
    /// Core: Pure "node" records focusing on individual agent execution
    /// </summary>
    [Id(6)] public List<WorkUnitExecutionFlowRecord> ExecutionRecords { get; set; } = new();
    
}

/// <summary>
/// Work unit execution flow record - focuses on single agent node execution process
/// </summary>
[GenerateSerializer]
public class WorkUnitExecutionFlowRecord
{
    /// <summary>
    /// Agent node identifier
    /// </summary>
    [Id(0)] public string AgentGrainId { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent type for debugging
    /// </summary>
    [Id(1)] public string AgentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Data lineage: records where the agent's data comes from
    /// </summary>
    [Id(2)] public List<string> BeforeAgentIds { get; set; } = new();
    
    /// <summary>
    /// Execution start time
    /// </summary>
    [Id(3)] public DateTime? StartTime { get; set; }
    
    /// <summary>
    /// Execution end time
    /// </summary>
    [Id(4)] public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Execution status
    /// </summary>
    [Id(5)] public WorkflowExecutionStatus Status { get; set; }
    
    /// <summary>
    /// Input data as JSON string (avoid duplication)
    /// </summary>
    [Id(6)] public string InputDataJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Output data as JSON string (avoid duplication)
    /// </summary>
    [Id(7)] public string OutputDataJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Real-time state snapshot (updated based on execution phase)
    /// - If Status is Running: contains pre-execution state
    /// - If Status is Completed: contains post-execution state
    /// </summary>
    [Id(8)] public string CurrentStateJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Retry count for error handling
    /// </summary>
    [Id(9)] public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Error messages for debugging
    /// </summary>
    [Id(10)] public List<string> ErrorMessages { get; set; } = new();
}


/// <summary>
/// Workflow run status
/// </summary>
public enum WorkflowRunStatus
{
    Pending,
    InProgress, 
    Completed,
    Failed
}

/// <summary>
/// Work unit execution status
/// </summary>
public enum WorkflowExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed
}
