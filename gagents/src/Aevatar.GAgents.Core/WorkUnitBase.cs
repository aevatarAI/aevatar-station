using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aevatar.GAgents.Core;

/// <summary>
/// Base class for workflow unit agents that encapsulates common execution logic
/// Usage: Inherit from this class and override UpdateWorkflowStatusPre/Post as needed
/// </summary>
public abstract class WorkUnitBase<TState, TStateLogEvent, TConfiguration> : 
    BusinessAgentBase<TState, TStateLogEvent, TConfiguration>
    where TState : BusinessAgentState, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TConfiguration : ConfigurationBase
{
    /// <summary>
    /// Initialize GrainIdString property for Interceptor logging during agent activation
    /// </summary>
    protected override Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // Initialize GrainIdString for Interceptor attribute logging
        GrainIdString = this.GetGrainId().ToString();
        return base.OnGAgentActivateAsync(cancellationToken);
    }

    /// <summary>
    /// Workflow context property constants for interceptor logging
    /// </summary>
    public const string WorkflowLogCategory = "WORKFLOW";
    public const string WorkflowIdProperty = "WorkflowId";
    public const string GrainIdProperty = "GrainIdString";

    /// <summary>
    /// Workflow ID for this agent instance, used by InterceptorAttribute for workflow logging
    /// </summary>
    public virtual string? WorkflowId { get; protected set; }

    /// <summary>
    /// Grain ID string for this agent instance, used by InterceptorAttribute for workflow logging
    /// </summary>
    public virtual string? GrainIdString { get; protected set; }


    /// <summary>
    /// Updates workflow status when processing starts
    /// </summary>
    protected override void UpdateWorkflowStatusPre(WorkflowEvent workflowEvent)
    {
        // Initialize WorkflowId property for Interceptor logging
        if (workflowEvent.WorkflowId != Guid.Empty)
        {
            WorkflowId = workflowEvent.WorkflowId.ToString();
        }
        // Save all received messages as JSON array for accurate InputData tracking
        workflowEvent.Metadata["inputData"] = JsonSerializer.Serialize(_receivedMessages);
        base.UpdateWorkflowStatusPre(workflowEvent);
    }

    protected override void UpdateWorkflowStatusPost(WorkflowEvent workflowEvent)
    {
        // Assign the WorkUnitAgentId to represent this processing node
        workflowEvent.WorkUnitAgentId = this.GetGrainId().ToString();
        base.UpdateWorkflowStatusPost(workflowEvent);
    }
}

