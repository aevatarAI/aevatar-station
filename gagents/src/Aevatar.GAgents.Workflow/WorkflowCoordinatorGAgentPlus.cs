using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow.Core;
using Aevatar.GAgents.Workflow.Core.Configs;
using Aevatar.GAgents.Workflow.Core.Events;
using Aevatar.GAgents.Workflow.Core.Models;
using Aevatar.GAgents.Workflow.Core.States;
using Aevatar.GAgents.MCP.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.Threading;
using Aevatar.Core.Placement;
using Aevatar.GAgents.Core;

namespace Aevatar.GAgents.Workflow;

[GAgent]
[SiloNamePatternPlacement("Projector")]
public class WorkflowCoordinatorGAgentPlus : GAgentBasePlus<WorkflowCoordinatorStatePlus, WorkflowCoordinatorLogEvent, WorkflowEvent, WorkflowCoordinatorConfigDto>, IWorkflowCoordinatorGAgentPlus
{
    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        // WorkflowCoordinatorGAgent is a system agent, not a business agent
        await base.OnGAgentActivateAsync(cancellationToken);
    }
    
    /// <summary>
    /// Returns true to indicate this is a workflow system agent
    /// (Not a business processing agent)
    /// </summary>
    public Task<bool> GetIsWorkflowAgentAsync()
    {
        return Task.FromResult(true);
    }

    public override Task<string> GetDescriptionAsync()
    {
        var status = State.WorkflowStatus.ToString();
        var nodeCount = State.CurrentWorkUnitInfos?.Count ?? 0;
        var completedCount =
            State.CurrentWorkUnitInfos?.Count(u => u.UnitStatusEnum == WorkerUnitStatusEnum.Finished) ?? 0;

        return Task.FromResult(
            "WorkflowCoordinatorGAgent - Orchestrates complex workflow execution with DAG-based task dependencies. " +
            "Manages workflow lifecycle (Pending→InProgress→Finished), validates topology to prevent loops, " +
            "coordinates parallel execution of independent nodes, and ensures data flow through the Blackboard pattern. " +
            $"Current Status: {status}, Nodes: {nodeCount} (Completed: {completedCount})"
        );
    }

    #region WorkflowEvent Handling

    /// <summary>
    /// ✅ REFACTORED: Direct implementation of WorkflowEvent handling
    /// No longer inherits from BusinessAgentBase - coordinator is not a business processor
    /// Implements its own validation and routing logic
    /// </summary>
    protected override async Task<bool> OnEventForwardingEventHandlerAsync(WorkflowEvent workflowEvent)
    {
        // ✅ VALIDATION: Basic checks for WorkflowEvent
        if (workflowEvent == null)
        {
            Logger.LogWarning("[WorkflowCoordinatorGAgent] Received null WorkflowEvent");
            return false;
        }

        if (workflowEvent.WorkflowId == Guid.Empty)
        {
            Logger.LogWarning("[WorkflowCoordinatorGAgent] Received WorkflowEvent with empty WorkflowId");
            return false;
        }

        // ✅ CRITICAL: Allow WorkflowFailed events (coordinator must route failure events to ExecutionRecordGAgent)
        // Unlike business agents, coordinator doesn't skip events with ErrorMessage
        if (workflowEvent.WorkflowEventType == WorkflowEventType.WorkflowFailed)
        {
            Logger.LogInformation("[WorkflowCoordinatorGAgent] Accepting WorkflowFailed event with error: {ErrorMessage}",
                workflowEvent.ErrorMessage);
        }

        Logger.LogDebug("[WorkflowCoordinatorGAgent] OnEventForwardingEventHandlerAsync: {WorkflowEventType}", 
            workflowEvent.WorkflowEventType);

        try
        {
            // Always raise WorkflowEventReceivedLogEvent first
            RaiseEvent(new WorkflowEventReceivedLogEvent
            {
                WorkflowId = workflowEvent.WorkflowId, // Use original WorkflowId from WorkflowViewAgent
                AgentId = this.GetGrainId().GetGuidKey(),
                EventType = workflowEvent.WorkflowEventType,
                AgentName = this.GetType().FullName,
                TaskResult = workflowEvent.TaskResult,
                Status = WorkflowAgentStatus.Completed,
                ReceivedAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            // Handle different workflow event types
            switch (workflowEvent.WorkflowEventType)
            {
                case WorkflowEventType.WorkflowInProgress:
                    await HandleWorkflowAsync(workflowEvent);
                    break;
                    
                case WorkflowEventType.WorkflowStarted:
                    await HandleWorkflowStartAsync(workflowEvent);
                    break;
                    
                case WorkflowEventType.WorkflowCompleted:
                    await HandleWorkflowCompletedAsync(workflowEvent);
                    break;
                    
                case WorkflowEventType.WorkflowReset:
                    await HandleWorkflowResetAsync(workflowEvent);
                    break;
                    
                case WorkflowEventType.WorkflowFailed:
                    await HandleWorkflowFailedAsync(workflowEvent);
                    break;
                    
                default:
                    Logger.LogWarning("[WorkflowCoordinatorGAgent] Unknown WorkflowEventType: {EventType}", 
                        workflowEvent.WorkflowEventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[WorkflowCoordinatorGAgent] Error processing WorkflowEvent: {WorkflowEventType}", 
                workflowEvent.WorkflowEventType);
            workflowEvent.ErrorMessage = ex.Message;
            workflowEvent.WorkflowEventType = WorkflowEventType.WorkflowFailed;
            return false;
        }
        
        // ✅ Event processed successfully - GAgentBasePlus will forward to children automatically
        return true;
    }

    private async Task HandleWorkflowAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] HandleWorkflowTaskCompletedAsync start");

        // NEW: Direct AgentId correlation using GrainId string (replaces Term-based system per design document)
        var workUnitAgentId = workflowEvent.WorkUnitAgentId;
        if (string.IsNullOrEmpty(workUnitAgentId))
        {
            Logger.LogError("[WorkflowCoordinatorGAgent] WorkUnitAgentId is empty in workflow event");
            return;
        }
        
        var workUnitInfo = State.CurrentWorkUnitInfos
            .FirstOrDefault(w => w.AgentId == workUnitAgentId);
        
        if (workUnitInfo == null)
        {
            Logger.LogError("[WorkflowCoordinatorGAgent] No work unit found for agent {WorkUnitAgentId}", workUnitAgentId);
            return;
        }
        
        // Log warning if not in expected state, but continue processing
        if (workUnitInfo.UnitStatusEnum != WorkerUnitStatusEnum.InProgress)
        {
            Logger.LogWarning("[WorkflowCoordinatorGAgent] Work unit for agent {WorkUnitAgentId} was in state {CurrentState} instead of InProgress, but marking as finished anyway", 
                workUnitAgentId, workUnitInfo.UnitStatusEnum);
        }

        if (!workflowEvent.ErrorMessage.IsNullOrEmpty())
        {
            Logger.LogError("[WorkflowCoordinatorGAgent] WorkflowTaskCompleted failed for agent {WorkUnitAgentId}: {ErrorMessage}", 
                workUnitAgentId, workflowEvent.ErrorMessage);
            RaiseEvent(new WorkflowStartFailedLogEvent());
            await ConfirmEvents();
            return;
        }

        // Mark work unit as finished using AgentId (no Term needed)
        RaiseEvent(new FinishedWorkUnitLogEvent() { 
            WorkUnitGrainId = workUnitInfo.AgentId,
            Term = 0 // Set to 0 since we're not using Term system anymore
        });
        await ConfirmEvents();
        
        Logger.LogInformation("[WorkflowCoordinatorGAgent] Work unit {WorkUnitAgentId} completed successfully", workUnitAgentId);


        Logger.LogDebug("[WorkflowCoordinatorGAgent] HandleWorkflowTaskCompletedAsync end");
    }

    private async Task HandleWorkflowStartAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] HandleWorkflowStartAsync start");
        
        // if (State.WorkflowStatus != WorkflowCoordinatorStatus.Pending && State.WorkflowStatus != WorkflowCoordinatorStatus.Failed)
        // {
        //     Logger.LogError("[WorkflowCoordinatorGAgent] The workflow is not ready to run.");
        //     workflowEvent.ErrorMessage = "The workflow is not ready to run.";
        //     return;
        // }

        // NEW: Dynamically discover and build workflow topology from actual agent relationships
        // Parse WorkUnitAgentId to extract AgentId and AgentTypeName
        var startGrainId = GrainId.Parse(workflowEvent.WorkUnitAgentId);
        var startAgentId = startGrainId.GetGuidKey();
        var startAgentType = startGrainId.Type.ToString();
        await DiscoverAndBuildWorkflowTopologyAsync(startAgentId, startAgentType);

        // Extract initial content from metadata
        var initContent = workflowEvent.Metadata.TryGetValue("InitContent", out var initContentObj) 
            ? initContentObj?.ToString() ?? State.Content
            : State.Content;

        // Extract execution name from metadata - REQUIRED
        var executionName = workflowEvent.Metadata.TryGetValue("ExecutionName", out var executionNameObj)
            ? executionNameObj?.ToString()
            : null;
            
        if (string.IsNullOrEmpty(executionName))
        {
            Logger.LogError("[WorkflowCoordinatorGAgent] Execution name is required but not provided in WorkflowEvent metadata for {WorkflowEventType}", 
                workflowEvent.WorkflowEventType);
            workflowEvent.ErrorMessage = $"ExecutionName is required in WorkflowEvent metadata for {workflowEvent.WorkflowEventType}";
            return;
        }

        // Register ExecutionRecord agent to receive workflow events
        var executionRecordId = await RegisterExecutionRecordAsync(executionName, initContent ?? string.Empty);
        
        // Raise WorkflowStartLogEvent to update coordinator state
        RaiseEvent(new WorkflowStartLogEvent
        {
            ExecutionRecordId = executionRecordId,
            ExecutionName = executionName
        });
        await ConfirmEvents();
        
        Logger.LogInformation("[WorkflowCoordinatorGAgent] Workflow started: ExecutionName={ExecutionName}, ExecutionRecordId={ExecutionRecordId}", 
            executionName, executionRecordId);

        // Compose essential metadata for WorkflowExecutionRecordGAgent
        workflowEvent.Metadata["RoundId"] = State.RoundId + 1;
        workflowEvent.Metadata["Content"] = initContent;
        workflowEvent.Metadata["WorkUnitInfos"] = State.CurrentWorkUnitInfos.ToList();
        Logger.LogDebug("[WorkflowCoordinatorGAgent] HandleWorkflowStartAsync end");
    }

    private async Task HandleWorkflowResetAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] HandleWorkflowResetAsync start");

        RaiseEvent(new ResetWorkflowLogEvent());
        await ConfirmEvents();

        Logger.LogDebug("[WorkflowCoordinatorGAgent] HandleWorkflowResetAsync end");
    }

    private async Task HandleWorkflowCompletedAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] HandleWorkflowCompletedAsync start");
        
        // Log workflow completion - this event comes from WorkflowEndAgent
        Logger.LogInformation("[WorkflowCoordinatorGAgent] Workflow {WorkflowId} completed by agent {WorkUnitAgentId}",
            workflowEvent.WorkflowId, workflowEvent.WorkUnitAgentId);
        
        // NOTE: ExecutionRecord agent will unregister itself after processing completion event
        // This ensures all workflow events are properly forwarded before breaking parent-child relationship
            
        // Raise state event to mark workflow as completed (per requirements)
        RaiseEvent(new WorkflowFinishLogEvent());
        await ConfirmEvents();
        
        Logger.LogDebug("[WorkflowCoordinatorGAgent] HandleWorkflowCompletedAsync end");
    }

    private async Task HandleWorkflowFailedAsync(WorkflowEvent workflowEvent)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] HandleWorkflowFailedAsync start");
        
        // Log workflow failure and update coordinator state
        Logger.LogError("[WorkflowCoordinatorGAgent] Workflow {WorkflowId} failed by agent {WorkUnitAgentId}: {ErrorMessage}",
            workflowEvent.WorkflowId, workflowEvent.WorkUnitAgentId, workflowEvent.ErrorMessage);
            
        // Raise failure event to update coordinator state
        RaiseEvent(new WorkflowStartFailedLogEvent());
        await ConfirmEvents();
        
        Logger.LogDebug("[WorkflowCoordinatorGAgent] HandleWorkflowFailedAsync end");
    }

    #endregion

    #region override method

    protected override async Task PerformConfigAsync(WorkflowCoordinatorConfigDto configuration)
    {
        Logger.LogDebug(
            $"[WorkflowCoordinatorGAgent] [PerformConfigAsync] WorkflowCoordinatorConfigDto:{JsonConvert.SerializeObject(configuration)}");

        await TryRegisterWorkUnitsAsync(configuration.WorkflowUnitList);

        var toUnregisterWorkUnit = new List<WorkUnitInfo>();
        if (State.WorkflowStatus == WorkflowCoordinatorStatus.Pending)
        {
            toUnregisterWorkUnit = State.CurrentWorkUnitInfos.Where(backup =>
                configuration.WorkflowUnitList.All(current => current.GrainId != backup.AgentId.ToString())).ToList();
        }
        else
        {
            toUnregisterWorkUnit = State.BackupWorkUnitInfos.Where(backup =>
                configuration.WorkflowUnitList.All(current => current.GrainId != backup.AgentId.ToString())).ToList();
        }

        RaiseEvent(new SetWorkflowCoordinatorLogEvent
            { WorkflowUnit = configuration.WorkflowUnitList, BlackBoardId = State.BlackboardId, InitContent = configuration.InitContent, EnableExecutionRecord = configuration.EnableExecutionRecord});

        await ConfirmEvents();

    }

    protected override void GAgentTransitionState(WorkflowCoordinatorStatePlus state,
        StateLogEventBase<WorkflowCoordinatorLogEvent> @event)
    {
        switch (@event)
        {
            case SetWorkflowCoordinatorLogEvent setWorkflowCoordinatorLogEvent:
                var nodeList = setWorkflowCoordinatorLogEvent.WorkflowUnit.Select(s => new WorkUnitInfo
                {
                    AgentId = s.GrainId,
                    NextAgentId = s.NextGrainId ?? string.Empty,
                    NodeId = s.GrainId, // Use AgentId as NodeId for dynamic discovery
                    NextNodeId = s.NextGrainId ?? string.Empty,
                    UnitStatusEnum = WorkerUnitStatusEnum.Pending,
                    ExtendedData = s.ExtendedData
                }).ToList();

                if (state.WorkflowStatus is WorkflowCoordinatorStatus.Pending or WorkflowCoordinatorStatus.Failed)
                {
                    state.CurrentWorkUnitInfos = nodeList;
                    state.WorkflowStatus = WorkflowCoordinatorStatus.Pending;
                }
                else
                {
                    state.BackupWorkUnitInfos = nodeList;
                }

                state.BlackboardId = setWorkflowCoordinatorLogEvent.BlackBoardId;
                state.Content = setWorkflowCoordinatorLogEvent.InitContent;
                state.EnableRunRecord = setWorkflowCoordinatorLogEvent.EnableExecutionRecord;
                break;

            case SetWorkflowCoordinatorDirectLogEvent setWorkflowCoordinatorDirectLogEvent:
                // Direct assignment - no DTO conversion needed!
                if (state.WorkflowStatus is WorkflowCoordinatorStatus.Pending or WorkflowCoordinatorStatus.Failed)
                {
                    state.CurrentWorkUnitInfos = setWorkflowCoordinatorDirectLogEvent.WorkUnitInfos;
                    state.WorkflowStatus = WorkflowCoordinatorStatus.Pending;
                }
                else
                {
                    state.BackupWorkUnitInfos = setWorkflowCoordinatorDirectLogEvent.WorkUnitInfos;
                }

                state.BlackboardId = setWorkflowCoordinatorDirectLogEvent.BlackBoardId;
                state.Content = setWorkflowCoordinatorDirectLogEvent.InitContent;
                state.EnableRunRecord = setWorkflowCoordinatorDirectLogEvent.EnableExecutionRecord;
                break;

            case FinishedWorkUnitLogEvent finishedWorkUnitLogEvent:
                var workUnitInfoList =
                    state.CurrentWorkUnitInfos.FindAll(f => f.AgentId == finishedWorkUnitLogEvent.WorkUnitGrainId);
                foreach (var workUnit in workUnitInfoList)
                {
                    workUnit.UnitStatusEnum = WorkerUnitStatusEnum.Finished;
                }

                // REMOVED: TermToWorkUnitGrainId system no longer used (direct AgentId correlation)
                break;

            case WorkflowFinishLogEvent:
                State.WorkflowStatus = WorkflowCoordinatorStatus.Failed;
                state.WorkflowStatus = WorkflowCoordinatorStatus.Pending;
                // REMOVED: TermToWorkUnitGrainId system no longer used (direct AgentId correlation)
                if (state.BackupWorkUnitInfos.Count > 0)
                {
                    state.CurrentWorkUnitInfos = State.BackupWorkUnitInfos.Select(s => s).ToList();
                    state.BackupWorkUnitInfos.Clear();
                }
                else
                {
                    foreach (var workUnit in state.CurrentWorkUnitInfos)
                    {
                        workUnit.UnitStatusEnum = WorkerUnitStatusEnum.Pending;
                    }
                }
                // Clear current execution tracking but PRESERVE execution records dictionary
                state.CurrentExecutionRecordId = Guid.Empty;
                state.CurrentExecutionName = null;
                // NOTE: ExecutionRecords dictionary is preserved for historical tracking

                break;

            case StartWorkUnitLogEvent workUnitLogEvent:
                var startWorkUnitInfoList =
                    state.CurrentWorkUnitInfos.FindAll(f => f.AgentId == workUnitLogEvent.WorkUnitGrainId);
                foreach (var startWorkUnitInfo in startWorkUnitInfoList)
                {
                    startWorkUnitInfo.UnitStatusEnum = WorkerUnitStatusEnum.InProgress;
                }

                // REMOVED: TermToWorkUnitGrainId and Term system no longer used (direct AgentId correlation)
                break;

            case WorkflowStartLogEvent workflowStartLogEvent:
                state.WorkflowStatus = WorkflowCoordinatorStatus.InProgress;
                state.LastRunningTime = DateTime.UtcNow;
                state.CurrentExecutionRecordId = workflowStartLogEvent.ExecutionRecordId;
                
                // Add execution record to dictionary with name
                // ExecutionName is now guaranteed to be non-empty due to validation in HandleWorkflowStartAsync
                state.CurrentExecutionName = workflowStartLogEvent.ExecutionName;
                state.ExecutionRecords[workflowStartLogEvent.ExecutionName] = workflowStartLogEvent.ExecutionRecordId;
                
                state.RoundId += 1;
                break;

            case ResetWorkflowLogEvent:
                state.WorkflowStatus = WorkflowCoordinatorStatus.Pending;
                state.CurrentWorkUnitInfos.Clear();
                state.BackupWorkUnitInfos.Clear();
                state.CurrentExecutionRecordId = Guid.Empty;
                state.CurrentExecutionName = null;
                state.ExecutionRecords.Clear(); // Clear all execution history on reset
                break;

            case WorkflowStartFailedLogEvent:
                State.WorkflowStatus = WorkflowCoordinatorStatus.Failed;
                State.LastRunningTime = DateTime.UtcNow;
                
                break;
        }
        base.GAgentTransitionState(state, @event);
    }

    #endregion

    #region Dynamic Topology Discovery

    /// <summary>
    /// NEW: Dynamically discover workflow topology from actual agent relationships
    /// Traverses from start agent through all children to build complete workflow graph
    /// CORRECTED: NodeId is a new Guid (workflow node), NOT the same as AgentId (grain)
    /// </summary>
    private async Task DiscoverAndBuildWorkflowTopologyAsync(Guid startAgentId, string startAgentType)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] DiscoverAndBuildWorkflowTopologyAsync starting from agent {StartAgentId}", startAgentId);

        var discoveredWorkUnits = new List<WorkUnitInfo>();
        var visited = new HashSet<GrainId>();
        var queue = new Queue<GrainId>();
        
        // Map AgentId to NodeId (workflow topology concept vs runtime concept)
        var agentToNodeMap = new Dictionary<Guid, Guid>();
        var currentAgent = GrainFactory.GetGrain<IBusinessAgentBase>(startAgentId, grainClassNamePrefix: startAgentType);
        // Start discovery from the start agent
        queue.Enqueue(currentAgent.GetGrainId());
        visited.Add(currentAgent.GetGrainId());

        while (queue.Count > 0)
        {

            var currentAgentGrainId = queue.Dequeue();
            var currentAgentId = currentAgentGrainId.GetGuidKey();
            var agentType = currentAgentGrainId.Type.ToString();

            try
            {
                // Get reference to current agent with grain type prefix
                currentAgent = GrainFactory.GetGrain<IBusinessAgentBase>(currentAgentId, grainClassNamePrefix: agentType);

                // Check if this agent is a workflow agent and exclude it
                if (this.GetGrainId() == currentAgent.GetGrainId() || await currentAgent.GetIsWorkflowAgentAsync())
                {
                    Logger.LogDebug("[WorkflowCoordinatorGAgent] Skipping workflow agent {AgentId}", currentAgentId);
                    continue;
                }

                // Generate a NEW NodeId for this workflow node (never use AgentId as NodeId!)
                if (!agentToNodeMap.ContainsKey(currentAgentId))
                {
                    agentToNodeMap[currentAgentId] = Guid.NewGuid(); // NEW workflow node ID
                }
                var currentNodeId = agentToNodeMap[currentAgentId];

                // Get agent information using proper state access (not parsing!)
                var (agentName, agentTypeName) = await currentAgent.GetAgentInfoAsync();

                Logger.LogDebug("[WorkflowCoordinatorGAgent] Agent {AgentId} → Node {NodeId}: Name='{Name}', Type='{Type}'",
                    currentAgentId, currentNodeId, agentName, agentTypeName);

                // Get children of current agent
                var children = await currentAgent.GetChildrenAsync();

                Logger.LogDebug("[WorkflowCoordinatorGAgent] Agent {AgentId} has {ChildCount} children",
                    currentAgentId, children.Count);

                children.RemoveAll(childId => childId == this.GetGrainId()); // Remove self-references if any
                
                // Create work unit for current agent
                if (children.Count > 0)
                {
                    // Agent has children - create work units for each relationship
                    foreach (var childId in children)
                    {
                        var childGuid = childId.GetGuidKey();

                        // Generate NodeId for child agent if not exists
                        if (!agentToNodeMap.ContainsKey(childGuid))
                        {
                            agentToNodeMap[childGuid] = Guid.NewGuid(); // NEW workflow node ID for child
                        }
                        var childNodeId = agentToNodeMap[childGuid];

                        discoveredWorkUnits.Add(new WorkUnitInfo
                        {
                            NodeId = currentNodeId.ToString("N"),        // Workflow node ID as string
                            NextNodeId = childNodeId.ToString("N"),      // Next workflow node ID as string
                            AgentId = currentAgentGrainId.ToString(),    // Runtime grain ID (full GrainId string)
                            NextAgentId = childId.ToString(),            // Next runtime grain ID (full GrainId string)
                            Name = agentName,
                            AgentType = agentTypeName,
                            UnitStatusEnum = WorkerUnitStatusEnum.Pending,
                            ExtendedData = new Dictionary<string, string>
                            {
                                ["DiscoveredAt"] = DateTime.UtcNow.ToString("O")
                            }
                        });

                        // Add child to discovery queue if not already visited
                        if (visited.Add(childId))
                        {
                            queue.Enqueue(childId);
                        }
                    }
                }
                else
                {
                    // Terminal agent (no children) - create end node
                    discoveredWorkUnits.Add(new WorkUnitInfo
                    {
                        NodeId = currentNodeId.ToString("N"),       // Workflow node ID as string
                        NextNodeId = string.Empty,                  // No next workflow node
                        AgentId = currentAgentGrainId.ToString(),   // Runtime grain ID (full GrainId string)
                        NextAgentId = string.Empty,                 // No next runtime grain
                        Name = agentName,
                        AgentType = agentTypeName,
                        UnitStatusEnum = WorkerUnitStatusEnum.Pending,
                        ExtendedData = new Dictionary<string, string>
                        {
                            ["DiscoveredAt"] = DateTime.UtcNow.ToString("O")
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "[WorkflowCoordinatorGAgent] Failed to discover topology for agent {AgentId}", currentAgentId);

                // Generate fallback NodeId
                if (!agentToNodeMap.ContainsKey(currentAgentId))
                {
                    agentToNodeMap[currentAgentId] = Guid.NewGuid();
                }

                // Create fallback work unit for this agent
                discoveredWorkUnits.Add(new WorkUnitInfo
                {
                    NodeId = agentToNodeMap[currentAgentId].ToString("N"),  // Workflow node ID as string
                    NextNodeId = string.Empty,
                    AgentId = currentAgentGrainId.ToString(),                // Runtime grain ID (full GrainId string)
                    NextAgentId = string.Empty,
                    Name = $"Agent-{currentAgentId:N}",
                    AgentType = agentType ?? "Unknown",
                    UnitStatusEnum = WorkerUnitStatusEnum.Pending,
                    ExtendedData = new Dictionary<string, string>
                    {
                        ["DiscoveredAt"] = DateTime.UtcNow.ToString("O"),
                        ["Error"] = ex.Message
                    }
                });
            }
        }

        Logger.LogInformation("[WorkflowCoordinatorGAgent] Discovered {WorkUnitCount} work units with {NodeCount} unique workflow nodes",
            discoveredWorkUnits.Count, agentToNodeMap.Count);

        // Directly populate CurrentWorkUnitInfos with discovered topology (no DTO conversion!)
        RaiseEvent(new SetWorkflowCoordinatorDirectLogEvent
        {
            WorkUnitInfos = discoveredWorkUnits,
            BlackBoardId = State.BlackboardId,
            InitContent = State.Content,
            EnableExecutionRecord = State.EnableRunRecord
        });
        await ConfirmEvents();

        Logger.LogDebug("[WorkflowCoordinatorGAgent] Dynamic topology discovery completed");
    }

    #endregion

    #region private method

    private IEnumerable<WorkUnitInfo> GetNewWorkUnits()
    {
        return State.BackupWorkUnitInfos.Where(backup =>
            !State.CurrentWorkUnitInfos.Any(current => current.AgentId == backup.AgentId));
    }
/*
    private async Task TryFinishWorkflowAsync()
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] TryFinishWorkflowAsync start");
        if (State.WorkflowStatus != WorkflowCoordinatorStatus.InProgress)
        {
            Logger.LogDebug("[WorkflowCoordinatorGAgent] TryFinishWorkflowAsync: WorkflowStatus not InProgress");
            return;
        }

        if (State.CheckAllWorkUnitFinished())
        {
            Logger.LogDebug("[WorkflowCoordinatorGAgent] All work units finished, finishing workflow");
            
            await UnregisterExecutionRecordAsync();

            RaiseEvent(new WorkflowFinishLogEvent());
            await ConfirmEvents();

            Logger.LogDebug("[WorkflowCoordinatorGAgent] Workflow finished and work units unregistered");
        }

        Logger.LogDebug("[WorkflowCoordinatorGAgent] TryFinishWorkflowAsync end");
    }
*/
    private async Task TryRegisterWorkUnitsAsync(List<WorkflowUnitDto> workflowUnits)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] TryRegisterWorkUnitsAsync start, count: {WorkflowUnitsCount}", workflowUnits.Count);
        if (workflowUnits.Count == 0)
        {
            Logger.LogDebug("[WorkflowCoordinatorGAgent] No workflow units to register");
            return;
        }

        if (!IsAllPathsCanReachTerminal(workflowUnits))
        {
            Logger.LogError("[WorkflowCoordinatorGAgent] The workflow has a loop and cannot end normally.");
            throw new ArgumentException("The workflow has a loop and cannot end normally.");
        }

        Logger.LogDebug("[WorkflowCoordinatorGAgent] TryRegisterWorkUnitsAsync end");
    }

    private bool IsAllPathsCanReachTerminal(List<WorkflowUnitDto> workflowUnits)
    {
        Dictionary<string, List<string>> graph = new();
        HashSet<string> allNodeIds = [];

        foreach (var unit in workflowUnits)
        {
            allNodeIds.Add(unit.GrainId);
            if (!graph.ContainsKey(unit.GrainId))
                graph[unit.GrainId] = new List<string>();

            if (!string.IsNullOrWhiteSpace(unit.NextGrainId))
            {
                graph[unit.GrainId].Add(unit.NextGrainId);
                allNodeIds.Add(unit.NextGrainId);
            }
        }

        var terminalNodes = workflowUnits
            .Where(n => string.IsNullOrWhiteSpace(n.NextGrainId))
            .Select(n => n.GrainId)
            .ToHashSet();

        var reachable = new HashSet<string>(terminalNodes);
        var queue = new Queue<string>(terminalNodes);

        Dictionary<string, List<string>> reverseGraph = new();

        foreach (var unit in workflowUnits)
        {
            if (!string.IsNullOrWhiteSpace(unit.NextGrainId))
            {
                if (!reverseGraph.ContainsKey(unit.NextGrainId))
                    reverseGraph[unit.NextGrainId] = new List<string>();
                reverseGraph[unit.NextGrainId].Add(unit.GrainId);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (reverseGraph.TryGetValue(current, out var preNodes))
            {
                foreach (var node in preNodes.Where(node => reachable.Add(node)))
                {
                    queue.Enqueue(node);
                }
            }
        }

        return allNodeIds.All(nodeId => reachable.Contains(nodeId));
    }

    private async Task<Guid> RegisterExecutionRecordAsync(string executionName, string content)
    {
        // ✅ FIXED: For Plus system, ALWAYS enable execution recording
        // The State.EnableRunRecord check is unreliable due to event sourcing timing
        // Configuration is already enforced at WorkflowViewServicePlus level
        
        var id = Guid.NewGuid();
        Logger.LogInformation("[WorkflowCoordinatorGAgent] Creating ExecutionRecordAgent with ID: {ExecutionRecordId}", id);
        
        var executionRecordAgent = GrainFactory.GetGrain<IWorkflowExecutionRecordGAgentPlus>(id);
        Logger.LogInformation("[WorkflowCoordinatorGAgent] Got ExecutionRecordAgent grain, about to call RegisterAsync");
        
        await RegisterAsync(executionRecordAgent);
        Logger.LogInformation("[WorkflowCoordinatorGAgent] RegisterAsync completed for ExecutionRecordAgent");
        
        // Verify registration
        var children = await GetChildrenAsync();
        var hasChild = children.Any(c => c == executionRecordAgent.GetGrainId());
        Logger.LogInformation("[WorkflowCoordinatorGAgent] Verification: Children count={Count}, HasExecutionRecord={HasChild}", 
            children.Count, hasChild);
        
        Logger.LogInformation("[WorkflowCoordinatorGAgent] Registered ExecutionRecordAgent: {ExecutionRecordId}, ExecutionName: {ExecutionName}", 
            id, executionName);
        
        // Note: ExecutionRecords dictionary will be populated by WorkflowStartLogEvent
        return id;
    }
    
    private async Task UnregisterExecutionRecordAsync()
    {
        if (State.CurrentExecutionRecordId == Guid.Empty)
        {
            return;
        }
        
        var executionRecordAgent = GrainFactory.GetGrain<IWorkflowExecutionRecordGAgentPlus>(State.CurrentExecutionRecordId);
        
        await UnregisterAsync(executionRecordAgent);
    }

    #endregion

    #region Task 16: Service-Direct Workflow Coordination

    /// <summary>
    /// ✅ TASK 16: Get start node agent IDs for service-direct workflow execution
    /// Uses existing GetTopUpStreamGrainIds() method to find start nodes (no incoming connections)
    /// </summary>
    public async Task<List<string>> GetStartNodeAgentIdsAsync()
    {
        // Use existing method to find start nodes (no incoming connections)
        var startNodeIds = State.GetTopUpStreamGrainIds();
        
        // Convert Guid list to string list for return type compatibility
        var startNodeStringIds = startNodeIds.Select(id => id.ToString()).ToList();
        
        Logger.LogDebug("[WorkflowCoordinatorGAgent] Found {Count} start nodes: {StartNodes}", 
            startNodeIds.Count, string.Join(", ", startNodeIds));
        
        return await Task.FromResult(startNodeStringIds);
    }

    #endregion

    #region Execution Record Query Methods

    /// <summary>
    /// Get execution record ID by execution name
    /// </summary>
    public async Task<Guid> GetExecutionRecordIdAsync(string executionName)
    {
        if (string.IsNullOrEmpty(executionName))
        {
            Logger.LogWarning("[WorkflowCoordinatorGAgent] GetExecutionRecordIdAsync called with null or empty execution name");
            return Guid.Empty;
        }

        if (State.ExecutionRecords.TryGetValue(executionName, out var recordId))
        {
            Logger.LogDebug("[WorkflowCoordinatorGAgent] Found execution record {RecordId} for execution '{ExecutionName}'", 
                recordId, executionName);
            return recordId;
        }

        Logger.LogDebug("[WorkflowCoordinatorGAgent] No execution record found for execution '{ExecutionName}'", 
            executionName);
        return Guid.Empty;
    }

    /// <summary>
    /// Get all execution records (name -> record ID mapping)
    /// </summary>
    public async Task<Dictionary<string, Guid>> GetAllExecutionRecordsAsync()
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] GetAllExecutionRecordsAsync returning {Count} execution records", 
            State.ExecutionRecords.Count);
            
        // Return a copy to prevent external modifications
        return await Task.FromResult(new Dictionary<string, Guid>(State.ExecutionRecords));
    }

    /// <summary>
    /// Check if an execution with the given name exists
    /// </summary>
    public async Task<bool> HasExecutionAsync(string executionName)
    {
        if (string.IsNullOrEmpty(executionName))
        {
            return false;
        }

        var exists = State.ExecutionRecords.ContainsKey(executionName);
        Logger.LogDebug("[WorkflowCoordinatorGAgent] Execution '{ExecutionName}' exists: {Exists}", 
            executionName, exists);
            
        return await Task.FromResult(exists);
    }

    #endregion

}

[GenerateSerializer]
public class WorkflowCoordinatorLogEvent : StateLogEventBase<WorkflowCoordinatorLogEvent>;

[GenerateSerializer]
public class SetWorkflowCoordinatorLogEvent : WorkflowCoordinatorLogEvent
{
    [Id(0)] public List<WorkflowUnitDto> WorkflowUnit { get; set; } = new();
    [Id(1)] public Guid BlackBoardId { get; set; }
    [Id(2)] public string? InitContent { get; set; } = null;
    [Id(3)] public bool EnableExecutionRecord { get; set; }
}

[GenerateSerializer]
public class SetWorkflowCoordinatorDirectLogEvent : WorkflowCoordinatorLogEvent
{
    [Id(0)] public List<WorkUnitInfo> WorkUnitInfos { get; set; } = new();
    [Id(1)] public Guid BlackBoardId { get; set; }
    [Id(2)] public string? InitContent { get; set; } = null;
    [Id(3)] public bool EnableExecutionRecord { get; set; }
}

[GenerateSerializer]
public class StartWorkUnitLogEvent : WorkflowCoordinatorLogEvent
{
    [Id(0)] public string WorkUnitGrainId { get; set; }
    [Id(1)] public long Term { get; set; }
}

[GenerateSerializer]
public class FinishedWorkUnitLogEvent : WorkflowCoordinatorLogEvent
{
    [Id(0)] public string WorkUnitGrainId { get; set; }
    [Id(1)] public long Term { get; set; }
}

[GenerateSerializer]
public class WorkflowFinishLogEvent : WorkflowCoordinatorLogEvent;

[GenerateSerializer]
public class WorkflowStartLogEvent : WorkflowCoordinatorLogEvent
{
    [Id(0)] public Guid ExecutionRecordId { get; set; }
    [Id(1)] public string ExecutionName { get; set; } = string.Empty;
}

[GenerateSerializer]
public class ResetWorkflowLogEvent : WorkflowCoordinatorLogEvent;

[GenerateSerializer]
public class WorkflowStartFailedLogEvent : WorkflowCoordinatorLogEvent;

[GenerateSerializer]
public class WorkflowEventReceivedLogEvent : WorkflowCoordinatorLogEvent
{
    [Id(0)] public Guid WorkflowId { get; set; }
    [Id(1)] public Guid AgentId { get; set; }
    [Id(2)] public WorkflowEventType EventType { get; set; }
    [Id(3)] public string AgentName { get; set; } = string.Empty;
    [Id(4)] public string TaskResult { get; set; } = string.Empty;
    [Id(5)] public WorkflowAgentStatus Status { get; set; }
    [Id(6)] public DateTime ReceivedAt { get; set; }
}