// using Aevatar.Core;
// using Aevatar.Core.Abstractions;
// using Aevatar.GAgents.Core;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Volo.Abp;

// namespace Aevatar.GAgents.Workflow;


// /// <summary>
// /// Interface for WorkflowQueryAgent - pure proxy agent for workflow status queries
// /// </summary>
// public interface IWorkflowQueryAgent : IStateGAgent<WorkflowQueryState>
// {
//     /// <summary>
//     /// Get workflow status for a specific workflow
//     /// </summary>
//     Task<WorkflowStatusDto> GetWorkflowStatusAsync(Guid workflowId);
    
//     /// <summary>
//     /// Get workflow node status for all nodes in a workflow
//     /// </summary>
//     Task<List<WorkflowNodeStatusDto>> GetWorkflowNodeStatusAsync(Guid workflowId);
    
//     /// <summary>
//     /// Trigger workflow execution using event forwarding
//     /// </summary>
//     Task<bool> TriggerWorkflowWithEventForwardingAsync(Guid workflowId, string initialMessage);
    
//     /// <summary>
//     /// Configure the WorkflowQueryAgent with references to other workflow agents
//     /// </summary>
//     Task ConfigureAsync(WorkflowQueryConfigDto config);
// }

// /// <summary>
// /// ✅ WorkflowQueryAgent - Pure proxy agent that receives WorkflowEvent updates and provides status queries
// /// Inherits from GAgentBase (not BusinessAgentBase) since it's not part of the business workflow
// /// Registered as child of workflow agents to receive status updates automatically
// /// </summary>
// [GAgent("workflow-query-agent", "workflow")]
// public class WorkflowQueryAgent : GAgentBase<WorkflowQueryState, WorkflowQueryLogEvent, WorkflowEvent, WorkflowQueryConfigDto>, IWorkflowQueryAgent
// {
//     private IGAgentFactory GAgentFactory => ServiceProvider.GetRequiredService<IGAgentFactory>();

//     public override Task<string> GetDescriptionAsync()
//         => Task.FromResult("Pure proxy agent that receives WorkflowEvent updates and provides workflow status queries to frontend");

//     /// <summary>
//     /// Called by WorkflowRunService to get workflow status
//     /// WorkflowQueryAgent maintains this status by receiving WorkflowEvent updates
//     /// </summary>
//     public async Task<WorkflowStatusDto> GetWorkflowStatusAsync(Guid workflowId)
//     {
//         var workflowInfo = State.WorkflowInfos.GetValueOrDefault(workflowId);
//         if (workflowInfo == null)
//         {
//             throw new UserFriendlyException($"Workflow {workflowId} not found");
//         }
        
//         return new WorkflowStatusDto
//         {
//             WorkflowId = workflowId,
//             Status = workflowInfo.Status,
//             StartTime = workflowInfo.StartTime,
//             EndTime = workflowInfo.EndTime,
//             CurrentStep = workflowInfo.CurrentStep,
//             TotalSteps = workflowInfo.TotalSteps
//         };
//     }
    
//     /// <summary>
//     /// Called by WorkflowRunService to get node status
//     /// </summary>
//     public async Task<List<WorkflowNodeStatusDto>> GetWorkflowNodeStatusAsync(Guid workflowId)
//     {
//         var workflowInfo = State.WorkflowInfos.GetValueOrDefault(workflowId);
//         if (workflowInfo == null)
//         {
//             return new List<WorkflowNodeStatusDto>();
//         }
        
//         return workflowInfo.NodeStatuses.Values.ToList();
//     }
    
//     /// <summary>
//     /// Called by WorkflowRunService to trigger workflow with event forwarding
//     /// </summary>
//     public async Task<bool> TriggerWorkflowWithEventForwardingAsync(Guid workflowId, string initialMessage)
//     {
//         // Register this workflow for tracking
//         var workflowInfo = new WorkflowInfo
//         {
//             WorkflowId = workflowId,
//             Status = WorkflowStatus.Running,
//             StartTime = DateTime.UtcNow,
//             CurrentStep = 0,
//             TotalSteps = 4 // Start → Input1 → Input2 → ChatAI → End
//         };
        
//         RaiseEvent(new WorkflowRegisteredLogEvent { WorkflowInfo = workflowInfo });
//         await ConfirmEvents();
        
//         // Trigger workflow using event forwarding
//         var workflowStartAgent = GrainFactory.GetGrain<IWorkflowStartAgent>(State.WorkflowStartAgentId);
        
//         var workflowEvent = new WorkflowEvent
//         {
//             WorkflowId = workflowId,
//             Message = initialMessage,
//             Direction = EventDirection.Down,
//             WorkflowEventType = WorkflowEventType.WorkflowStarted,
//             AgentId = this.GetGrainId().GetGuidKey(),
//             AgentName = "WorkflowQueryAgent"
//         };
        
//         await workflowStartAgent.PublishEventByDirectionAsync(workflowEvent);
        
//         Logger.LogInformation("Triggered workflow {WorkflowId} with event forwarding", workflowId);
//         return true;
//     }
    
//     /// <summary>
//     /// Configure the WorkflowQueryAgent with references to other workflow agents
//     /// </summary>
//     public async Task ConfigureAsync(WorkflowQueryConfigDto config)
//     {
//         await base.ConfigAsync(config);
        
//         Logger.LogInformation("WorkflowQueryAgent configured with references to workflow agents");
//     }

//     /// <summary>
//     /// EVENT HANDLER: Receives WorkflowEvent updates from the workflow chain
//     /// WorkflowQueryAgent is registered as a child of workflow agents to receive status updates
//     /// </summary>
//     protected override async Task OnEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
//     {
//         // Call base implementation first
//         await base.OnEventForwardingEventHandlerAsync(workflowEvent);
        
//         Logger.LogDebug("WorkflowQueryAgent received WorkflowEvent: {EventType} for workflow {WorkflowId} from agent {AgentName}",
//             workflowEvent.WorkflowEventType, workflowEvent.WorkflowId, workflowEvent.AgentName);
        
//         // Update workflow status based on received events
//         var workflowInfo = State.WorkflowInfos.GetValueOrDefault(workflowEvent.WorkflowId);
//         if (workflowInfo != null)
//         {
//             // Update workflow progress
//             workflowInfo.CurrentStep++;
//             workflowInfo.LastUpdateTime = DateTime.UtcNow;
            
//             // Update node status
//             var nodeStatus = new WorkflowNodeStatusDto
//             {
//                 NodeId = workflowEvent.AgentId,
//                 NodeName = workflowEvent.AgentName,
//                 Status = workflowEvent.WorkflowAgentStatus,
//                 StartTime = workflowEvent.StepStartTime,
//                 EndTime = DateTime.UtcNow,
//                 Result = workflowEvent.TaskResult,
//                 ErrorMessage = workflowEvent.ErrorMessage
//             };
            
//             workflowInfo.NodeStatuses[workflowEvent.AgentId] = nodeStatus;
            
//             // Check if workflow is complete
//             if (workflowEvent.WorkflowEventType == WorkflowEventType.WorkflowCompleted)
//             {
//                 workflowInfo.Status = WorkflowStatus.Completed;
//                 workflowInfo.EndTime = DateTime.UtcNow;
//             }
//             else if (workflowEvent.WorkflowEventType == WorkflowEventType.WorkflowFailed)
//             {
//                 workflowInfo.Status = WorkflowStatus.Failed;
//                 workflowInfo.EndTime = DateTime.UtcNow;
//             }
            
//             RaiseEvent(new WorkflowUpdatedLogEvent { WorkflowInfo = workflowInfo });
//             await ConfirmEvents();
            
//             Logger.LogInformation("Updated workflow {WorkflowId} status: {Status}, step {CurrentStep}/{TotalSteps}",
//                 workflowEvent.WorkflowId, workflowInfo.Status, workflowInfo.CurrentStep, workflowInfo.TotalSteps);
//         }
        
//         // WorkflowQueryAgent does NOT forward events - it's a pure proxy/observer
//         // No call to base.OnEventForwardingEventHandlerAsync() for forwarding
//     }

//     /// <summary>
//     /// Handle state transitions for WorkflowQueryAgent
//     /// </summary>
//     protected override void GAgentTransitionState(WorkflowQueryState state, StateLogEventBase<WorkflowQueryLogEvent> @event)
//     {
//         switch (@event)
//         {
//             case WorkflowRegisteredLogEvent registered:
//                 state.WorkflowInfos[registered.WorkflowInfo.WorkflowId] = registered.WorkflowInfo;
//                 break;
//             case WorkflowUpdatedLogEvent updated:
//                 state.WorkflowInfos[updated.WorkflowInfo.WorkflowId] = updated.WorkflowInfo;
//                 break;
//             case WorkflowQueryConfiguredLogEvent configured:
//                 state.WorkflowStartAgentId = configured.WorkflowStartAgentId;
//                 state.WorkflowViewAgentId = configured.WorkflowViewAgentId;
//                 state.WorkflowExecutionRecordId = configured.WorkflowExecutionRecordId;
//                 state.WorkflowCoordinatorId = configured.WorkflowCoordinatorId;
//                 break;
//         }
//     }

//     /// <summary>
//     /// Configure agent with workflow references
//     /// </summary>
//     protected override async Task PerformConfigAsync(WorkflowQueryConfigDto configuration)
//     {
//         await base.PerformConfigAsync(configuration);
        
//         RaiseEvent(new WorkflowQueryConfiguredLogEvent
//         {
//             WorkflowStartAgentId = configuration.WorkflowStartAgentId,
//             WorkflowViewAgentId = configuration.WorkflowViewAgentId,
//             WorkflowExecutionRecordId = configuration.WorkflowExecutionRecordId,
//             WorkflowCoordinatorId = configuration.WorkflowCoordinatorId
//         });
        
//         await ConfirmEvents();
        
//         Logger.LogInformation("WorkflowQueryAgent configured with workflow agent references");
//     }
// }

// /// <summary>
// /// State for WorkflowQueryAgent
// /// </summary>
// [GenerateSerializer]
// public class WorkflowQueryState : StateBase
// {
//     // Reference to the WorkflowStartAgent for triggering workflows
//     [Id(0)] public Guid WorkflowStartAgentId { get; set; }
    
//     // References to other workflow agents
//     [Id(1)] public Guid WorkflowViewAgentId { get; set; }
//     [Id(2)] public Guid WorkflowExecutionRecordId { get; set; }
//     [Id(3)] public Guid WorkflowCoordinatorId { get; set; }
    
//     // Workflow tracking data (maintained by receiving WorkflowEvent updates)
//     [Id(4)] public Dictionary<Guid, WorkflowInfo> WorkflowInfos { get; set; } = new();
// }

// /// <summary>
// /// Configuration for WorkflowQueryAgent
// /// </summary>
// [GenerateSerializer]
// public class WorkflowQueryConfigDto : ConfigurationBase
// {
//     [Id(0)] public Guid WorkflowStartAgentId { get; set; }
//     [Id(1)] public Guid WorkflowViewAgentId { get; set; }
//     [Id(2)] public Guid WorkflowExecutionRecordId { get; set; }
//     [Id(3)] public Guid WorkflowCoordinatorId { get; set; }
// }

// /// <summary>
// /// Information about a workflow being tracked
// /// </summary>
// [GenerateSerializer]
// public class WorkflowInfo
// {
//     [Id(0)] public Guid WorkflowId { get; set; }
//     [Id(1)] public WorkflowStatus Status { get; set; }
//     [Id(2)] public DateTime StartTime { get; set; }
//     [Id(3)] public DateTime? EndTime { get; set; }
//     [Id(4)] public DateTime LastUpdateTime { get; set; }
//     [Id(5)] public int CurrentStep { get; set; }
//     [Id(6)] public int TotalSteps { get; set; }
//     [Id(7)] public Dictionary<Guid, WorkflowNodeStatusDto> NodeStatuses { get; set; } = new();
// }

// /// <summary>
// /// DTO for workflow status information
// /// </summary>
// [GenerateSerializer]
// public class WorkflowStatusDto
// {
//     [Id(0)] public Guid WorkflowId { get; set; }
//     [Id(1)] public WorkflowStatus Status { get; set; }
//     [Id(2)] public DateTime StartTime { get; set; }
//     [Id(3)] public DateTime? EndTime { get; set; }
//     [Id(4)] public int CurrentStep { get; set; }
//     [Id(5)] public int TotalSteps { get; set; }
// }

// /// <summary>
// /// DTO for workflow node status information
// /// </summary>
// [GenerateSerializer]
// public class WorkflowNodeStatusDto
// {
//     [Id(0)] public Guid NodeId { get; set; }
//     [Id(1)] public string NodeName { get; set; } = string.Empty;
//     [Id(2)] public WorkflowAgentStatus Status { get; set; }
//     [Id(3)] public DateTime StartTime { get; set; }
//     [Id(4)] public DateTime EndTime { get; set; }
//     [Id(5)] public string Result { get; set; } = string.Empty;
//     [Id(6)] public string ErrorMessage { get; set; } = string.Empty;
// }

// /// <summary>
// /// Base class for WorkflowQueryAgent log events
// /// </summary>
// [GenerateSerializer]
// public abstract class WorkflowQueryLogEvent : StateLogEventBase<WorkflowQueryLogEvent>
// {
// }

// /// <summary>
// /// Log event for workflow registration
// /// </summary>
// [GenerateSerializer]
// public class WorkflowRegisteredLogEvent : WorkflowQueryLogEvent
// {
//     [Id(0)] public WorkflowInfo WorkflowInfo { get; set; } = new();
// }

// /// <summary>
// /// Log event for workflow status updates
// /// </summary>
// [GenerateSerializer]
// public class WorkflowUpdatedLogEvent : WorkflowQueryLogEvent
// {
//     [Id(0)] public WorkflowInfo WorkflowInfo { get; set; } = new();
// }

// /// <summary>
// /// Log event for WorkflowQueryAgent configuration
// /// </summary>
// [GenerateSerializer]
// public class WorkflowQueryConfiguredLogEvent : WorkflowQueryLogEvent
// {
//     [Id(0)] public Guid WorkflowStartAgentId { get; set; }
//     [Id(1)] public Guid WorkflowViewAgentId { get; set; }
//     [Id(2)] public Guid WorkflowExecutionRecordId { get; set; }
//     [Id(3)] public Guid WorkflowCoordinatorId { get; set; }
// }
