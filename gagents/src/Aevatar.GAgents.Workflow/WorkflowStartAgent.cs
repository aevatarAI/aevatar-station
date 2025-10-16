using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Core;
using Orleans;
using Aevatar.Core.Placement;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Workflow;

/// <summary>
/// Interface for WorkflowStartAgent
/// </summary>
public interface IWorkflowStartAgent : IStateGAgentPlus<WorkflowStartState>
{
}

/// <summary>
/// State for WorkflowStartAgent
/// </summary>
[GenerateSerializer]
public class WorkflowStartState : BusinessAgentState
{
}

/// <summary>
/// Base class for WorkflowStartAgent log events
/// </summary>
[GenerateSerializer]
public abstract class WorkflowStartAgentLogEvent : StateLogEventBase<WorkflowStartAgentLogEvent>
{
}

/// <summary>
/// Configuration DTO for WorkflowStartAgent
/// </summary>
[GenerateSerializer]
public class WorkflowStartConfigDto : ConfigurationBase
{
    [Id(0)] public string AgentName { get; set; } = "WorkflowStartAgent";
}

/// <summary>
/// ✅ WORKFLOW: WorkflowStartAgent for workflow initiation
/// Inherits from BusinessAgentBase to handle WorkflowEvent forwarding
/// Simply forwards events to child agents in the workflow chain
/// </summary>
[GAgent]
[SiloNamePatternPlacement("Projector")]
public class WorkflowStartAgent : BusinessAgentBase<WorkflowStartState, WorkflowStartAgentLogEvent, WorkflowStartConfigDto>, IWorkflowStartAgent
{
    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Mark this agent as a workflow infrastructure agent to exclude from topology discovery
        this._isWorkflowAgent = true;
        
        await base.OnGAgentActivateAsync(cancellationToken);
    }

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Workflow Start Agent that initiates workflows by forwarding events to child agents");

    /// <summary>
    /// ✅ TASK 14: Override BusinessAgentBase event handler for workflow start processing
    /// Handles WorkflowEvent and sets WorkflowEventType.WorkflowStarted
    /// </summary>
    protected override async Task OnBusinessAgentEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogInformation("[WorkflowStartAgent] Starting workflow execution {WorkflowId}", 
            workflowEvent.WorkflowId);  // Execution Record Agent ID
        
        // Assign the WorkUnitAgentId to represent this processing node
        workflowEvent.WorkUnitAgentId = this.GetPrimaryKey();
        
        // Update existing event fields for this step
        workflowEvent.WorkflowEventType = WorkflowEventType.WorkflowStarted;
        workflowEvent.WorkflowAgentStatus = WorkflowAgentStatus.Completed;  // ✅ FIX: Mark as completed like other agents
        
        // Process initial message using existing EventBase.Message field (text pipeline)
        var initialMessage = workflowEvent.Message;
        var processedMessage = $"Started: {initialMessage}";
        workflowEvent.Message = processedMessage;     // Update for downstream agents
        workflowEvent.TaskResult = processedMessage;
        
        Logger.LogInformation("[WorkflowStartAgent] Processed start message: {Message}", processedMessage);
        
        // BusinessAgentBase automatically forwards to registered children via GAgentBase
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handle state transitions for WorkflowStartAgent events
    /// </summary>
    protected override void GAgentTransitionState(WorkflowStartState state, StateLogEventBase<WorkflowStartAgentLogEvent> @event)
    {
        // Handle WorkflowStartAgent-specific events here if needed
        switch (@event)
        {
            // Add custom WorkflowStartAgent event handling here when needed
            // case SomeWorkflowStartEvent someEvent:
            //     // Handle custom event
            //     break;
        }
        
        // CRITICAL: Call base implementation to handle standard GAgent events
        // (AddChildStateLogEvent, RemoveChildStateLogEvent, AddParentStateLogEvent, etc.)
        base.GAgentTransitionState(state, @event);
    }
}