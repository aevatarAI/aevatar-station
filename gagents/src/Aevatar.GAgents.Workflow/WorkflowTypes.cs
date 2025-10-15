using System;
using System.Collections.Generic;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Core; // Import WorkflowEvent types from Core

namespace Aevatar.GAgents.Workflow;

/// <summary>
/// ✅ WORKFLOW: Workflow lifecycle event for point-to-point coordination
/// Located in GAgents.Workflow alongside workflow coordination logic
/// Uses types from GAgents.Core for consistency
/// </summary>
[GenerateSerializer]
public class WorkflowLifecycleEvent : EventBase
{
    [Id(0)] public Guid WorkflowId { get; set; }
    [Id(1)] public Guid AgentId { get; set; }
    [Id(2)] public WorkflowEventType EventType { get; set; }
    [Id(3)] public string AgentName { get; set; } = string.Empty;
    [Id(4)] public string TaskResult { get; set; } = string.Empty;
    [Id(5)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [Id(6)] public WorkflowAgentStatus Status { get; set; }
    [Id(7)] public string ErrorMessage { get; set; } = string.Empty;
    [Id(8)] public TimeSpan ExecutionDuration { get; set; }
    [Id(9)] public Dictionary<string, object> Metadata { get; set; } = new();
}
