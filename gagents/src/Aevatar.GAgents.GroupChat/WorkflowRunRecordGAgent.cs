using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core;
using Aevatar.GAgents.GroupChat.Core.States;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator;

/// <summary>
/// Workflow Run Record GAgent - tracks workflow execution with real-time state and data lineage
/// Focuses on "node" recording mode, tracking individual agent execution process
/// </summary>
[GAgent]
public class WorkflowRunRecordGAgent :
    GAgentBase<WorkflowRunRecordState, WorkflowRunRecordLogEvent, EventBase>, IWorkflowRunRecordGAgent
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        NullValueHandling = NullValueHandling.Ignore
    };

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Workflow Run Record GAgent - Tracks workflow execution with real-time state snapshots and data lineage. " +
            "Uses pure 'node' recording mode focusing on individual agent execution process, " +
            "with BeforeAgentIds for data lineage tracking and CurrentStateJson for real-time state.");
    }

    #region Event Handlers

    [EventHandler]
    public async Task HandleEventAsync(StartExecuteWorkflowEvent @event)
    {
        Logger.LogInformation("Starting workflow run record for WorkflowId: {WorkflowId}, Term: {Term}",
            @event.WorkflowId, @event.RoundId);

        RaiseEvent(new StartWorkflowRunLogEvent
        {
            WorkflowId = @event.WorkflowId,
            Term = @event.RoundId,
            InitContent = @event.Content,
            StartTime = DateTime.UtcNow
        });
        
        await ConfirmEvents();
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatEvent @event)
    {
        try
        {
            Logger.LogDebug("Recording work unit start for Agent: {AgentId}", @event.Speaker);

            // Get data lineage (where the agent's data comes from)
            var beforeAgentIds = GetBeforeAgentIds(@event.Speaker.ToString());
            
            // Capture pre-execution state
            var currentStateJson = await CaptureAgentStateAsync(@event.Speaker.ToString());
            
            // Get agent type for debugging
            var agentType = GetAgentType(@event.Speaker.ToString());

            RaiseEvent(new StartWorkUnitRunLogEvent
            {
                AgentGrainId = @event.Speaker.ToString(),
                AgentType = agentType,
                BeforeAgentIds = beforeAgentIds,
                InputDataJson = JsonConvert.SerializeObject(@event.CoordinatorMessages, JsonSettings),
                CurrentStateJson = currentStateJson,
                StartTime = DateTime.UtcNow
            });


            await ConfirmEvents();
            
            Logger.LogDebug("Successfully recorded work unit start for Agent: {AgentId}", @event.Speaker);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to handle ChatEvent for Agent: {AgentId}", @event.Speaker);
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(ChatResponseEvent @event)
    {
        try
        {
            Logger.LogDebug("Recording work unit completion for Agent: {AgentId}", @event.PublisherGrainId);

            // Capture post-execution state
            var currentStateJson = await CaptureAgentStateAsync(@event.PublisherGrainId.ToString());

            RaiseEvent(new FinishWorkUnitRunLogEvent
            {
                AgentGrainId = @event.PublisherGrainId.ToString(),
                OutputDataJson = JsonConvert.SerializeObject(@event.ChatResponse?.Content, JsonSettings),
                CurrentStateJson = currentStateJson,
                EndTime = DateTime.UtcNow,
                Status = Core.States.WorkflowExecutionStatus.Completed,
                ErrorMessages = new List<string>() // TODO: Extract from response if needed
            });


            await ConfirmEvents();
            
            Logger.LogDebug("Successfully recorded work unit completion for Agent: {AgentId}", @event.PublisherGrainId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to handle ChatResponseEvent for Agent: {AgentId}", @event.PublisherGrainId);
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(GroupChatFinishEvent @event)
    {
        Logger.LogInformation("Finishing workflow run record for WorkflowId: {WorkflowId}", State.WorkflowId);

        RaiseEvent(new FinishWorkflowRunLogEvent
        {
            EndTime = DateTime.UtcNow,
            Status = WorkflowRunStatus.Completed
        });
        
        await ConfirmEvents();
    }

    #endregion

    #region State Transition

    protected override void GAgentTransitionState(WorkflowRunRecordState state,
        StateLogEventBase<WorkflowRunRecordLogEvent> @event)
    {
        switch (@event)
        {
            case StartWorkflowRunLogEvent startEvent:
                state.WorkflowId = startEvent.WorkflowId;
                state.Term = startEvent.Term;
                state.InitContent = startEvent.InitContent;
                state.StartTime = startEvent.StartTime;
                state.Status = WorkflowRunStatus.InProgress;
                state.ExecutionRecords = new List<WorkUnitExecutionFlowRecord>();
                break;

            case StartWorkUnitRunLogEvent startUnitEvent:
                var existingRecord = state.ExecutionRecords.FirstOrDefault(r => 
                    r.AgentGrainId == startUnitEvent.AgentGrainId);
                
                if (existingRecord == null)
                {
                    state.ExecutionRecords.Add(new WorkUnitExecutionFlowRecord
                    {
                        AgentGrainId = startUnitEvent.AgentGrainId,
                        AgentType = startUnitEvent.AgentType,
                        BeforeAgentIds = startUnitEvent.BeforeAgentIds,
                        StartTime = startUnitEvent.StartTime,
                        Status = Core.States.WorkflowExecutionStatus.Running,
                        InputDataJson = startUnitEvent.InputDataJson,
                        CurrentStateJson = startUnitEvent.CurrentStateJson, // Pre-execution state
                        RetryCount = 0,
                        ErrorMessages = new List<string>()
                    });
                }
                else
                {
                    // Handle retry case
                    existingRecord.StartTime = startUnitEvent.StartTime;
                    existingRecord.Status = Core.States.WorkflowExecutionStatus.Running;
                    existingRecord.InputDataJson = startUnitEvent.InputDataJson;
                    existingRecord.CurrentStateJson = startUnitEvent.CurrentStateJson;
                    existingRecord.RetryCount++;
                }
                break;

            case FinishWorkUnitRunLogEvent finishUnitEvent:
                var workUnit = state.ExecutionRecords.FirstOrDefault(r => 
                    r.AgentGrainId == finishUnitEvent.AgentGrainId);
                
                if (workUnit != null)
                {
                    workUnit.EndTime = finishUnitEvent.EndTime;
                    workUnit.Status = finishUnitEvent.Status;
                    workUnit.OutputDataJson = finishUnitEvent.OutputDataJson;
                    workUnit.CurrentStateJson = finishUnitEvent.CurrentStateJson; // Post-execution state
                    workUnit.ErrorMessages = finishUnitEvent.ErrorMessages;
                }
                break;

            case FinishWorkflowRunLogEvent finishEvent:
                state.EndTime = finishEvent.EndTime;
                state.Status = finishEvent.Status;
                break;

        }
    }

    #endregion

    #region Public Methods

    public Task<string> GetExecutionSummaryAsync()
    {
        var summary = $"Workflow {State.WorkflowId} (Term: {State.Term})\n" +
                     $"Status: {State.Status}, Duration: {GetWorkflowDuration()}\n" +
                     $"Work Units: {State.ExecutionRecords.Count}\n" +
                     $"- Completed: {State.ExecutionRecords.Count(r => r.IsCompleted())}\n" +
                     $"- Running: {State.ExecutionRecords.Count(r => r.Status == Core.States.WorkflowExecutionStatus.Running)}\n" +
                     $"- Failed: {State.ExecutionRecords.Count(r => r.Status == Core.States.WorkflowExecutionStatus.Failed)}";
        
        return Task.FromResult(summary);
    }

    public Task<List<WorkUnitExecutionFlowRecord>> GetExecutionRecordsAsync()
    {
        return Task.FromResult(State.ExecutionRecords);
    }


    public Task<WorkUnitExecutionFlowRecord?> GetWorkUnitRecordAsync(string agentGrainId)
    {
        var record = State.ExecutionRecords.FirstOrDefault(r => r.AgentGrainId == agentGrainId);
        return Task.FromResult(record);
    }

    #endregion

    #region Helper Methods

    private List<string> GetBeforeAgentIds(string currentAgentId)
    {
        // TODO: Get upstream agent IDs from WorkflowCoordinator state
        // This would require access to the workflow topology
        // For now, return empty list as placeholder
        return new List<string>();
    }

    private async Task<string> CaptureAgentStateAsync(string agentGrainId)
    {
        try
        {
            // TODO: Implement agent state capture
            // This requires accessing the specific GAgent implementation
            // For now, return placeholder JSON
            return $"{{\"agentId\": \"{agentGrainId}\", \"timestamp\": \"{DateTime.UtcNow:O}\"}}";
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Failed to capture agent state for {AgentId}: {Error}", 
                agentGrainId, ex.Message);
            return $"{{\"error\": \"{ex.Message}\"}}";
        }
    }

    private string GetAgentType(string agentGrainId)
    {
        try
        {
            // Extract agent type from grain ID string
            // This is a simplified version
            return agentGrainId.Contains("GAgent") ? "GAgent" : "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private TimeSpan? GetWorkflowDuration()
    {
        if (!State.EndTime.HasValue) return null;
        return State.EndTime.Value - State.StartTime;
    }

    #endregion
}
