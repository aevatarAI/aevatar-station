using Orleans.Concurrency;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;

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
            // Validation
            if (!await ValidateWorkflowEventAsync(workflowEvent))
            {
                return false; // Skip processing if validation fails
            }

            // Record input message from upstream agent
            if (!string.IsNullOrEmpty(workflowEvent.Message) && workflowEvent.AgentId != Guid.Empty)
            {
                await RecordInputMessageAsync(workflowEvent);
            }

            // Check dependencies after pre-processing
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

            // Clear received messages even on error to avoid stale data
            ClearReceivedMessages();
            return false;
        }

        // GAgentBase automatically forwards to children - no explicit PublishEventByDirectionAsync needed
        // Event forwarding is handled by the base class based on agent hierarchy
        return await base.OnEventForwardingEventHandlerAsync(workflowEvent);    
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

        Logger.LogInformation("Business agent {AgentId} completed WorkflowEvent processing: {WorkflowEventType}",
            this.GetGrainId(), workflowEvent.WorkflowEventType);

        // Clear received messages after successful processing
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
        // Update workflow tracking information for processing start
        workflowEvent.StepStartTime = DateTime.UtcNow;
    }

    /// <summary>
    /// ✅ NEW: Updates workflow status when processing completes (Post processing)
    /// Override in derived classes for specific implementation
    /// </summary>
    protected virtual void UpdateWorkflowStatusPost(WorkflowEvent workflowEvent)
    {
        // Update workflow tracking information for processing completion
        workflowEvent.AgentId = this.GetGrainId().GetGuidKey();
        workflowEvent.AgentName = this.GetType().FullName;
        workflowEvent.WorkflowAgentStatus = WorkflowAgentStatus.Completed;
        workflowEvent.StepEndTime = DateTime.UtcNow;

        Logger.LogDebug("Updated WorkflowEvent and agent state status for agent {AgentId}: {WorkflowEventType}, WorkflowStatus: {WorkflowStatus}",
            this.GetGrainId(), workflowEvent.WorkflowEventType, State.WorkflowAgentStatus);
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

            Logger.LogDebug("[BusinessAgentBase] All dependencies ready for WorkflowId: {WorkflowId}, AgentId: {AgentId}",
                workflowEvent.WorkflowId, workflowEvent.AgentId);
            
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

                Logger.LogDebug("[BusinessAgentBase] Recorded input message from agent {FromAgentId} for WorkflowId: {WorkflowId}. Total messages: {Count}",
                    workflowEvent.AgentId, workflowEvent.WorkflowId, _receivedMessages.Count);
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