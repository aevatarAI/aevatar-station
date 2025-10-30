using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.Core;

/// <summary>
/// Workflow event types for coordination
/// </summary>
[GenerateSerializer]
public enum WorkflowEventType
{
    WorkflowStarted,
    WorkflowInProgress,
    WorkflowCompleted,
    WorkflowFailed,
    WorkflowReset,
}

/// <summary>
/// Workflow status enumeration for overall workflow state
/// </summary>
[GenerateSerializer]
public enum WorkflowStatus
{
    Pending,
    Running,
    Completed,
    Failed
}

/// <summary>
/// Workflow agent status enumeration for individual workflow nodes
/// </summary>
[GenerateSerializer]
public enum WorkflowAgentStatus
{
    Pending,
    Completed,
    Failed
}

/// <summary>
/// Workflow lifecycle event for point-to-point coordination
/// </summary>
[GenerateSerializer]
public class WorkflowEvent : EventBase
{
    // Workflow coordination properties
    [Id(0)] public Guid WorkflowId { get; set; }
    // Removed [Id(1)] AgentId and [Id(3)] AgentTypeName - replaced by WorkUnitAgentId
    [Id(2)] public WorkflowEventType WorkflowEventType { get; set; }
    [Id(3)] public string AgentName { get; set; } = string.Empty;
    [Id(4)] public string TaskResult { get; set; } = string.Empty;
    [Id(5)] public WorkflowAgentStatus WorkflowAgentStatus { get; set; }
    [Id(6)] public string ErrorMessage { get; set; } = string.Empty;
    [Id(7)] public TimeSpan ExecutionDuration { get; set; }
    [Id(8)] public DateTime StepStartTime { get; set; } = DateTime.UtcNow;
    [Id(9)] public DateTime? StepEndTime { get; set; }
    [Id(10)] public Dictionary<string, object> Metadata { get; set; } = new();
    [Id(11)] public string WorkUnitAgentId { get; set; } = string.Empty;
    
    // Inherits from EventBase:
    // - Message: Text data pipeline between agents
    // - Direction: Event routing direction  
    // - MaxHopCount: Hop limiting
    // - Publishers: Loop prevention
    // - CorrelationId: Event correlation
}