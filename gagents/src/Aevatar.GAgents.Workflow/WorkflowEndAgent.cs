using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Placement;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Core;
using Orleans;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Workflow;

/// <summary>
/// Interface for WorkflowEndAgent
/// </summary>
public interface IWorkflowEndAgent : IStateGAgentPlus<WorkflowEndState>
{
}

/// <summary>
/// State for WorkflowEndAgent
/// </summary>
[GenerateSerializer]
public class WorkflowEndState : BusinessAgentState
{
}

/// <summary>
/// Base class for WorkflowEndAgent log events
/// </summary>
[GenerateSerializer]
public abstract class WorkflowEndAgentLogEvent : StateLogEventBase<WorkflowEndAgentLogEvent>
{
}

/// <summary>
/// Configuration DTO for WorkflowEndAgent
/// </summary>
[GenerateSerializer]
public class WorkflowEndConfigDto : ConfigurationBase
{
    [Id(0)] public string AgentName { get; set; } = "WorkflowEndAgent";
}

/// <summary>
/// ✅ WORKFLOW: WorkflowEndAgent for workflow completion
/// Inherits from BusinessAgentBase to handle WorkflowEvent completion
/// Simply marks workflow as complete - WorkflowCoordinator has all the information
/// </summary>
[GAgent]
[SiloNamePatternPlacement("Projector")]
public class WorkflowEndAgent : BusinessAgentBase<WorkflowEndState, WorkflowEndAgentLogEvent, WorkflowEndConfigDto>, IWorkflowEndAgent
{
    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // ✅ WorkflowEndAgent is a business agent and should be included in topology discovery
        // DO NOT set _isWorkflowAgent = true here
        this._isWorkflowAgent = false;
        
        await base.OnGAgentActivateAsync(cancellationToken);
    }

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Workflow End Agent that marks workflow completion - WorkflowCoordinator maintains all workflow information");

    /// <summary>
    /// ✅ CRITICAL: Override validation to accept failure events
    /// WorkflowEndAgent is the fixed end node that must receive both success and failure events
    /// Unlike other business agents, it accepts failure events to mark workflow completion
    /// </summary>
    protected override async Task<bool> ValidateWorkflowEventAsync(WorkflowEvent workflowEvent)
    {
        // Basic null check
        if (workflowEvent == null)
        {
            Logger.LogWarning("WorkflowEndAgent received null WorkflowEvent");
            return false;
        }

        // WorkflowId validation
        if (workflowEvent.WorkflowId == Guid.Empty)
        {
            Logger.LogWarning("WorkflowEndAgent received WorkflowEvent with empty WorkflowId");
            return false;
        }

        // ✅ CRITICAL: Accept both success and failure events
        // Do NOT reject events with ErrorMessage (unlike other business agents)
        // This allows EndAgent to mark workflow completion regardless of outcome
        
        await Task.CompletedTask;
        return true;
    }

    /// <summary>
    /// ✅ TASK 14: Override BusinessAgentBase event handler for workflow completion
    /// Handles WorkflowEvent and sets WorkflowEventType.WorkflowCompleted
    /// </summary>
    protected override async Task OnBusinessAgentEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogInformation("[WorkflowEndAgent] Completing workflow execution {WorkflowId}", 
            workflowEvent.WorkflowId);  // Execution Record Agent ID
        
        // Mark workflow as completed using existing event fields
        workflowEvent.WorkflowAgentStatus = WorkflowAgentStatus.Completed;
        workflowEvent.WorkflowEventType = WorkflowEventType.WorkflowCompleted;
        
        // Process final result using existing EventBase.Message field (text pipeline)
        var finalMessage = workflowEvent.Message;     // Text from previous agents
        var completedMessage = $"Completed: {finalMessage}";
        workflowEvent.Message = completedMessage;
        workflowEvent.TaskResult = completedMessage;
        
        Logger.LogInformation("[WorkflowEndAgent] Final result: {Message}", completedMessage);
        
        // End of workflow - no forwarding needed
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handle state transitions for WorkflowEndAgent events
    /// </summary>
    protected override void GAgentTransitionState(WorkflowEndState state, StateLogEventBase<WorkflowEndAgentLogEvent> @event)
    {
        // Handle WorkflowEndAgent-specific events here if needed
        switch (@event)
        {
            // Add custom WorkflowEndAgent event handling here when needed
            // case SomeWorkflowEndEvent someEvent:
            //     // Handle custom event
            //     break;
        }
        
        // CRITICAL: Call base implementation to handle standard GAgent events
        // (AddChildStateLogEvent, RemoveChildStateLogEvent, AddParentStateLogEvent, etc.)
        base.GAgentTransitionState(state, @event);
    }
}
