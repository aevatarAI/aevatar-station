using Orleans.Concurrency;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Aevatar.GAgents.Core;


/// <summary>
/// ✅ ENHANCED: BusinessAgentBase for WorkflowEvent handling
/// Inherits from GAgentBase with WorkflowEvent as TEvent type parameter
/// Supports automatic GAgentBase event forwarding with WorkflowEvent
/// </summary>
public abstract class BusinessAgentBase<TState, TStateLogEvent, TConfiguration> :
    GAgentBasePlus<TState, TStateLogEvent, WorkflowEvent, TConfiguration>, IBusinessAgentBase
    where TState : BusinessAgentState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TConfiguration : ConfigurationBase
{
    /// <summary>
    /// Indicates whether this agent is a workflow agent (coordinator, execution record, etc.)
    /// Workflow agents should be excluded from workflow topology discovery
    /// </summary>
    protected bool _isWorkflowAgent = false;

    /// <summary>
    /// Gets whether this agent is a workflow agent (Orleans interface method)
    /// </summary>
    public Task<bool> GetIsWorkflowAgentAsync()
    {
        return Task.FromResult(_isWorkflowAgent);
    }

    /// <summary>
    /// Gets the agent information (name and type) from class name prefix - proper way instead of parsing descriptions
    /// </summary>
    public Task<(string Name, string Type)> GetAgentInfoAsync()
    {
        // Agent name = class name prefix + grain ID
        var agentName = $"{GetType().Name}-{this.GetGrainId().GetGuidKey():N}";
        var agentType = GetType().FullName;

        return Task.FromResult((agentName, agentType));
    }

    /// <summary>
    /// Initialize GrainIdString property for Interceptor logging during agent activation
    /// </summary>
    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Initialize GrainIdString for Interceptor attribute logging
        GrainIdString = this.GetGrainId().ToString();
        
        Logger.LogDebug("[BusinessAgentBase] Initialized GrainIdString property: {GrainIdString}", GrainIdString);
        
        return base.OnGAgentActivateAsync(cancellationToken);
    }

    /// <summary>
    /// Workflow context property constants for interceptor logging
    /// </summary>
    protected const string WorkflowLogCategory = "WORKFLOW";
    protected const string WorkflowIdProperty = "WorkflowId";
    protected const string RoundIdProperty = "RoundId";
    protected const string GrainIdProperty = "GrainIdString";

    /// <summary>
    /// Workflow ID for this agent instance, used by InterceptorAttribute for workflow logging
    /// Initialized from WorkflowEvent.WorkflowId during event processing
    /// </summary>
    public virtual string? WorkflowId { get; protected set; }

    /// <summary>
    /// Round identifier for current workflow execution cycle. Used by InterceptorAttribute as contextual field.
    /// Currently not used in Plus workflow system, but maintained for compatibility with old workflow logging
    /// </summary>
    public virtual long? RoundId { get; protected set; }

    /// <summary>
    /// Grain ID string for this agent instance, used by InterceptorAttribute for workflow logging
    /// Initialized from Orleans GrainId during agent activation
    /// Property name uses "GrainIdString" to avoid conflict with Orleans.Runtime.GrainId type
    /// </summary>
    public virtual string? GrainIdString { get; protected set; }

    /// <summary>
    /// ✅ NEW: Private field to track received messages from upstream agents
    /// Cleared after processing to avoid state persistence
    /// </summary>
    protected List<string> _receivedMessages = new();
    /// <summary>
    /// ✅ SEALED: Event forwarding handler for WorkflowEvent routing
    /// Ensures validation runs first before inheriting class handlers
    /// </summary>
    protected sealed override async Task<bool> OnEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogDebug("Business agent {AgentId} received WorkflowEvent: {WorkflowEventType} for workflow {WorkflowId}",
            this.GetGrainId(), workflowEvent.WorkflowEventType, workflowEvent.WorkflowId);

        try
        {
            // ✅ Validation first (before fast-path check)
            // This allows derived classes (e.g., WorkflowEndAgent) to override validation and accept failure events
            if (!await ValidateWorkflowEventAsync(workflowEvent))
            {
                return false; // Skip processing if validation fails
            }
            // ✅ Normal success event processing (with dependency checking)
            // Record input message from upstream agent (for dependency checking)
            if (!string.IsNullOrEmpty(workflowEvent.Message) && !string.IsNullOrEmpty(workflowEvent.WorkUnitAgentId))
            {
                await RecordInputMessageAsync(workflowEvent);
            }

            // Check dependencies after recording input
            if (!AreAllDependenciesReadyAsync(workflowEvent))
            {
                Logger.LogWarning("Business agent {AgentId} dependencies not ready for WorkflowEvent: {WorkflowEventType}",
                    this.GetGrainId(), workflowEvent.WorkflowEventType);
                return false;
            }

            // Pre-processing
            await PreBusinessAgentProcessingAsync(workflowEvent);

            // Call inheriting class handler
            await OnBusinessAgentEventForwardingEventHandlerAsync(workflowEvent);

            // Post-processing
            await PostBusinessAgentProcessingAsync(workflowEvent);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Business agent {AgentId} failed to process WorkflowEvent: {WorkflowEventType}",
                this.GetGrainId(), workflowEvent.WorkflowEventType);

            // Update event with error information
            workflowEvent.WorkflowEventType = WorkflowEventType.WorkflowFailed;
            workflowEvent.ErrorMessage = ex.Message;
            workflowEvent.WorkUnitAgentId = this.GetGrainId().ToString();
            workflowEvent.StepEndTime = DateTime.UtcNow;
            workflowEvent.WorkflowAgentStatus = WorkflowAgentStatus.Failed;
            
            // ✅ P2P MESSAGING: Send failure event directly to coordinator
            await SendFailureEventToCoordinatorAsync(workflowEvent);
            
            // Clear received messages even on error to avoid stale data
            ClearReceivedMessages();
            return false;
        }
    }

    /// <summary>
    /// ✅ NEW: Virtual method for inheriting classes to override
    /// Called after BusinessAgentBase validation passes
    /// Override this method in inheriting classes instead of OnEventForwardingEventHandlerAsync
    /// </summary>
    protected virtual Task OnBusinessAgentEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        // Default implementation does nothing - inheriting classes can override
        return Task.CompletedTask;
    }

    /// <summary>
    /// ✅ NEW: Pre-processing method executed before OnBusinessAgentEventForwardingEventHandlerAsync
    /// Handles input message recording and dependency checks
    /// </summary>
    protected virtual async Task PreBusinessAgentProcessingAsync(WorkflowEvent workflowEvent)
    {
        // Initialize WorkflowId property for Interceptor logging
        if (workflowEvent.WorkflowId != Guid.Empty)
        {
            WorkflowId = workflowEvent.WorkflowId.ToString();
        }

        // Initialize RoundId property for Interceptor logging from Metadata
        if (workflowEvent.Metadata.TryGetValue("RoundId", out var roundIdObj))
        {
            if (long.TryParse(roundIdObj?.ToString(), out var parsedRoundId))
            {
                RoundId = parsedRoundId;
            }
        }

        // Update workflow status
        UpdateWorkflowStatusPre(workflowEvent);

        await Task.CompletedTask;
    }

    /// <summary>
    /// ✅ NEW: Post-processing method executed after OnBusinessAgentEventForwardingEventHandlerAsync
    /// Handles status updates and cleanup
    /// </summary>
    protected virtual async Task PostBusinessAgentProcessingAsync(WorkflowEvent workflowEvent)
    {
        // Update workflow status
        UpdateWorkflowStatusPost(workflowEvent);
        
        // ✅ FIX: Only transfer TaskResult to Message if NO error occurred
        // This prevents error messages from propagating as normal data
        if (string.IsNullOrEmpty(workflowEvent.ErrorMessage))
        {
            // SUCCESS: Transfer TaskResult to Message for downstream agents
            workflowEvent.Message = workflowEvent.TaskResult ?? workflowEvent.Message;
            
            Logger.LogInformation("✅ Business agent {AgentId} completed successfully, forwarding Message: {Message}",
                this.GetGrainId(), 
                workflowEvent.Message?.Length > 50 ? workflowEvent.Message.Substring(0, 50) + "..." : workflowEvent.Message);
        }
        else
        {
            // FAILURE: Keep Message as is (original InputData), don't propagate error as normal data
            Logger.LogWarning("❌ Business agent {AgentId} failed with error: {ErrorMessage}, NOT forwarding TaskResult",
                this.GetGrainId(), workflowEvent.ErrorMessage);
        }

        // Clear received messages after processing
        ClearReceivedMessages();

        await Task.CompletedTask;
    }

    /// <summary>
    /// ✅ NEW: Simple validation method for WorkflowEvent
    /// Override in derived classes for specific validation logic
    /// </summary>
    protected virtual async Task<bool> ValidateWorkflowEventAsync(WorkflowEvent workflowEvent)
    {
        // If there's already an error, skip processing
        if (!string.IsNullOrEmpty(workflowEvent?.ErrorMessage))
        {
            Logger.LogWarning("Business agent {AgentId} received WorkflowEvent with existing error: {ErrorMessage}",
                this.GetGrainId(), workflowEvent.ErrorMessage);
            return false;
        }

        // Basic null check
        if (workflowEvent == null)
        {
            Logger.LogWarning("Business agent {AgentId} received null WorkflowEvent", this.GetGrainId());
            return false;
        }

        // WorkflowId validation
        if (workflowEvent.WorkflowId == Guid.Empty)
        {
            Logger.LogWarning("Business agent {AgentId} received WorkflowEvent with empty WorkflowId", this.GetGrainId());
            return false;
        }

        await Task.CompletedTask;
        return true;
    }

    /// <summary>
    /// ✅ NEW: Updates workflow status when processing starts (Pre processing)
    /// Override in derived classes for specific implementation
    /// </summary>
    protected virtual void UpdateWorkflowStatusPre(WorkflowEvent workflowEvent)

    {
       // Assign the WorkUnitAgentId to represent this processing node
        workflowEvent.WorkUnitAgentId = this.GetGrainId().ToString();
      
        // ✅ Save all received messages as JSON array for accurate InputData tracking
        workflowEvent.Metadata["inputData"] = JsonSerializer.Serialize(_receivedMessages);
        
        // Update workflow tracking information for processing start
        workflowEvent.StepStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ✅ NEW: Updates workflow status when processing completes (Post processing)
    /// Override in derived classes for specific implementation
    /// </summary>
    protected virtual void UpdateWorkflowStatusPost(WorkflowEvent workflowEvent)
    {
        // ✅ FIX: Only set Completed status if no error occurred
        if (string.IsNullOrEmpty(workflowEvent.ErrorMessage))
        {
            // SUCCESS: Update workflow tracking information for processing completion
            workflowEvent.WorkflowAgentStatus = WorkflowAgentStatus.Completed;
            workflowEvent.StepEndTime = DateTime.UtcNow;
            
            // Only set TaskResult if it's empty (agent should have set it)
            if (string.IsNullOrEmpty(workflowEvent.TaskResult))
            {
                workflowEvent.TaskResult = workflowEvent.Message ?? string.Empty;
            }

            Logger.LogDebug("✅ Agent {AgentId} completed successfully: {WorkflowEventType}, Status: {WorkflowStatus}, TaskResult: {TaskResult}",
                this.GetGrainId(), workflowEvent.WorkflowEventType, workflowEvent.WorkflowAgentStatus, 
                workflowEvent.TaskResult?.Length > 50 ? workflowEvent.TaskResult.Substring(0, 50) + "..." : workflowEvent.TaskResult);
        }
        else
        {
            // ✅ FAILURE: Set Failed status when error occurred
            workflowEvent.WorkflowAgentStatus = WorkflowAgentStatus.Failed;
            workflowEvent.StepEndTime = DateTime.UtcNow;
            // Keep TaskResult as is (may contain error-friendly message or be empty)
            
            Logger.LogWarning("❌ Agent {AgentId} failed: {WorkflowEventType}, Status: {WorkflowStatus}, ErrorMessage: {ErrorMessage}",
                this.GetGrainId(), workflowEvent.WorkflowEventType, workflowEvent.WorkflowAgentStatus, 
                workflowEvent.ErrorMessage);
        }
    }

    /// <summary>
    /// ✅ SIMPLIFIED: Virtual method for dependency readiness checks
    /// Focuses on input message dependencies - basic validation is handled elsewhere
    /// Override in derived classes to implement specific dependency logic
    /// </summary>
    protected virtual bool AreAllDependenciesReadyAsync(WorkflowEvent workflowEvent)
    {
        try
        {
            // Agent state readiness
            if (State == null)
            {
                Logger.LogWarning("[BusinessAgentBase] Agent state not initialized - dependencies not ready");
                return false;
            }

            // Check if all required input messages are received
            if (!AreAllInputMessagesReceived(workflowEvent.WorkflowId))
            {
                Logger.LogDebug("[BusinessAgentBase] Not all input messages received for WorkflowId: {WorkflowId}",
                    workflowEvent.WorkflowId);
                return false;
            }

            Logger.LogDebug("[BusinessAgentBase] All dependencies ready for WorkflowId: {WorkflowId}, WorkUnitAgentId: {WorkUnitAgentId}",
                workflowEvent.WorkflowId, workflowEvent.WorkUnitAgentId);
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[BusinessAgentBase] Error checking dependencies for WorkflowId: {WorkflowId}",
                workflowEvent?.WorkflowId);
            return false;
        }
    }

    /// <summary>
    /// ✅ NEW: Record input message from upstream agent in private field
    /// </summary>
    protected virtual Task RecordInputMessageAsync(WorkflowEvent workflowEvent)
    {
        try
        {
            if (!string.IsNullOrEmpty(workflowEvent.Message))
            {
                // ✅ CORRECT: Use private field - no state persistence needed
                _receivedMessages.Add(workflowEvent.Message);

                Logger.LogDebug("[BusinessAgentBase] Recorded input message from agent {FromWorkUnitAgentId} for WorkflowId: {WorkflowId}. Total messages: {Count}",
                    workflowEvent.WorkUnitAgentId, workflowEvent.WorkflowId, _receivedMessages.Count);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[BusinessAgentBase] Error recording input message for WorkflowId: {WorkflowId}",
                workflowEvent?.WorkflowId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// ✅ NEW: Check if all required input messages are received
    /// Expected count = number of parent agents
    /// </summary>
    protected virtual bool AreAllInputMessagesReceived(Guid workflowId)
    {
        try
        {
            // If no parents, no input messages needed
            if (State?.Parents == null || State.Parents.Count == 0)
            {
                Logger.LogDebug("[BusinessAgentBase] No parent agents - no input messages required for WorkflowId: {WorkflowId}",
                    workflowId);
                return true;
            }

            var expectedCount = State.Parents.Count;
            var receivedCount = _receivedMessages.Count;

            Logger.LogDebug("[BusinessAgentBase] Input message check for WorkflowId: {WorkflowId} - Expected: {Expected}, Received: {Received}",
                workflowId, expectedCount, receivedCount);

            return receivedCount == expectedCount;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[BusinessAgentBase] Error checking input messages for WorkflowId: {WorkflowId}",
                workflowId);
            return false;
        }
    }

    /// <summary>
    /// ✅ NEW: Clear received messages after processing
    /// </summary>
    protected virtual void ClearReceivedMessages()
    {
        _receivedMessages.Clear();
        Logger.LogDebug("[BusinessAgentBase] Cleared received messages after processing");
    }

    /// <summary>
    /// ✅ P2P MESSAGING: Send WorkflowFailed event directly to coordinator using SendEventToAgentAsync
    /// This bypasses parent-child event forwarding system for immediate failure notification
    /// Uses WorkflowEvent.WorkflowId directly (accurate) instead of State.WorkflowCoordinatorId
    /// </summary>
    protected virtual async Task SendFailureEventToCoordinatorAsync(WorkflowEvent failureEvent)
    {
        try
        {
            // ✅ Use WorkflowEvent.WorkflowId directly - it's the coordinator's GrainId
            if (failureEvent.WorkflowId == Guid.Empty)
            {
                Logger.LogWarning("[BusinessAgentBase] WorkflowEvent.WorkflowId is empty - cannot send failure event");
                return;
            }

            Logger.LogInformation("[BusinessAgentBase] Sending WorkflowFailed event to coordinator {CoordinatorId} from agent {AgentId}",
                failureEvent.WorkflowId, this.GetGrainId());

            // ✅ BEST PRACTICE: Use SendEventToAgentAsync for P2P event transmission
            // Get coordinator grain reference using IGAgentPlus interface (avoids circular dependency)
            var coordinator = GrainFactory.GetGrain<IGAgentPlus>(failureEvent.WorkflowId);
            var coordinatorGrainId = coordinator.GetGrainId();
            await SendEventToAgentAsync(failureEvent, coordinatorGrainId);

            Logger.LogInformation("[BusinessAgentBase] Successfully sent WorkflowFailed event to coordinator via P2P");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[BusinessAgentBase] Failed to send failure event to coordinator {CoordinatorId}",
                failureEvent.WorkflowId);
            // Don't rethrow - failure to notify coordinator shouldn't crash the agent
        }
    }

    /// <summary>
    /// ✅ CRITICAL: Handle state transitions using proper event sourcing
    /// </summary>
    protected override void GAgentTransitionState(TState state, StateLogEventBase<TStateLogEvent> @event)
    {
        // No business agent specific events currently - just call base

        // CRITICAL: Call base implementation to handle parent/child relationships and other standard events
        base.GAgentTransitionState(state, @event);
    }

}

/// <summary>
/// Interface for business agents to expose their state for upstream dependency checking
/// </summary>
public interface IBusinessAgentBase : IGAgentPlus
{
    /// <summary>
    /// Gets whether this agent is a workflow agent
    /// </summary>
    [ReadOnly]
    Task<bool> GetIsWorkflowAgentAsync();
    
    /// <summary>
    /// Gets the agent information (name and type) from state
    /// </summary>
    [ReadOnly]
    Task<(string Name, string Type)> GetAgentInfoAsync();
}