using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.GAgents.MCP.Core;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.Feature.Coordinator.GEvent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.Threading;

namespace Aevatar.GAgents.GroupChat.WorkflowCoordinator;

[GAgent]
public class WorkflowCoordinatorGAgent : GAgentBase<WorkflowCoordinatorState, WorkflowCoordinatorLogEvent, EventBase,
    WorkflowCoordinatorConfigDto>, IWorkflowCoordinatorGAgent
{
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

    #region EventHandler

    [EventHandler]
    public async Task HandleEventAsync(ChatResponseEvent @event)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] handler ChatResponseEvent start");

        var workUnitInfo = State.GetWorkUnitFromTerm(@event.Term);
        if (workUnitInfo == null)
        {
            Logger.LogError($"[WorkflowCoordinatorGAgent] ChatResponseEvent can not fund term:{@event.Term}");
            return;
        }

        if (workUnitInfo.UnitStatusEnum != WorkerUnitStatusEnum.InProgress)
        {
            Logger.LogError(
                $"[WorkflowCoordinatorGAgent] ChatResponseEvent term status not correct term:{@event.Term}");
            return;
        }

        var blackboard = GrainFactory.GetGrain<IBlackboardGAgent>(State.BlackboardId);
        await blackboard.SetMessageAsync(new CoordinatorConfirmChatResponse()
        {
            BlackboardId = @event.BlackboardId, MemberId = @event.MemberId, MemberName = @event.MemberName,
            ChatResponse = @event.ChatResponse
        });

        // maker sure this work unit has done
        RaiseEvent(new FinishedWorkUnitLogEvent() { Term = @event.Term, WorkUnitGrainId = workUnitInfo.GrainId });
        await ConfirmEvents();

        var downStreamList = State.GetDownStreamGrainIds(workUnitInfo.GrainId);
        // indicate: no next work unit
        if (downStreamList.Count == 0)
        {
            await TryFinishWorkflowAsync();
            return;
        }

        foreach (var grainId in downStreamList)
        {
            await TryActiveWorkUnitAsync(grainId);
        }

        Logger.LogDebug("[WorkflowCoordinatorGAgent] handler ChatResponseEvent end");
    }

    [EventHandler]
    public async Task HandleEventAsync(StartWorkflowCoordinatorEvent @event)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] handler StartWorkflowCoordinatorEvent start");
        if (State.WorkflowStatus != WorkflowCoordinatorStatus.Pending)
        {
            Logger.LogError("[WorkflowCoordinatorGAgent] The workflow is not ready to run.");
            return;
        }

        if (State.BlackboardId == Guid.Empty || !State.CurrentWorkUnitInfos.Any())
        {
            Logger.LogError("[WorkflowCoordinatorGAgent] The workflow has not been initialized.");

            RaiseEvent(new WorkflowStartFailedLogEvent());
            await ConfirmEvents();
            return;
        }

        var blackboard = GrainFactory.GetGrain<IBlackboardGAgent>(State.BlackboardId);
        await blackboard.ResetAsync();

        var executionRecordId = await RegisterExecutionRecordAsync(@event.InitContent ?? State.Content);
    
        RaiseEvent(new WorkflowStartLogEvent
        {
            ExecutionRecordId = executionRecordId
        });
        await ConfirmEvents();

        // try start top upstream work unit
        var topStreamGrainIds = State.GetTopUpStreamGrainIds();
        foreach (var item in topStreamGrainIds)
        {
            await TryActiveWorkUnitAsync(item, @event.InitContent ?? State.Content);
        }
    }

    [EventHandler]
    public async Task HandleEventAsync(ResetWorkflowEvent @event)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] handler ResetWorkflowEvent start");

        var blackboard = GrainFactory.GetGrain<IBlackboardGAgent>(State.BlackboardId);
        await blackboard.ResetAsync();

        await UnregisterWorkUnitAsync(State.BackupWorkUnitInfos);
        await UnregisterWorkUnitAsync(State.CurrentWorkUnitInfos);

        RaiseEvent(new ResetWorkflowLogEvent());
        await ConfirmEvents();

        Logger.LogDebug("[WorkflowCoordinatorGAgent] handler ResetWorkflowEvent end");
    }

    #endregion

    #region override method

    protected override async Task PerformConfigAsync(WorkflowCoordinatorConfigDto configuration)
    {
        Logger.LogDebug(
            $"[WorkflowCoordinatorGAgent] [PerformConfigAsync] WorkflowCoordinatorConfigDto:{JsonConvert.SerializeObject(configuration)}");

        await TryRegisterWorkUnitsAsync(configuration.WorkflowUnitList);
        var blackBoardId = this.GetPrimaryKey();
        var blackboardAgent = GrainFactory.GetGrain<IBlackboardGAgent>(blackBoardId);
        await RegisterAsync(blackboardAgent);

        var toUnregisterWorkUnit = new List<WorkUnitInfo>();
        if (State.WorkflowStatus == WorkflowCoordinatorStatus.Pending)
        {
            toUnregisterWorkUnit = State.CurrentWorkUnitInfos.Where(backup =>
                configuration.WorkflowUnitList.All(current => current.GrainId != backup.GrainId)).ToList();
        }
        else
        {
            toUnregisterWorkUnit = State.BackupWorkUnitInfos.Where(backup =>
                configuration.WorkflowUnitList.All(current => current.GrainId != backup.GrainId)).ToList();
        }

        RaiseEvent(new SetWorkflowCoordinatorLogEvent
            { WorkflowUnit = configuration.WorkflowUnitList, BlackBoardId = blackBoardId, InitContent = configuration.InitContent, EnableExecutionRecord = configuration.EnableExecutionRecord});

        await ConfirmEvents();

        await UnregisterWorkUnitAsync(toUnregisterWorkUnit);
    }

    protected override void GAgentTransitionState(WorkflowCoordinatorState state,
        StateLogEventBase<WorkflowCoordinatorLogEvent> @event)
    {
        switch (@event)
        {
            case SetWorkflowCoordinatorLogEvent setWorkflowCoordinatorLogEvent:
                var nodeList = setWorkflowCoordinatorLogEvent.WorkflowUnit.Select(s => new WorkUnitInfo
                {
                    GrainId = s.GrainId,
                    NextGrainId = s.NextGrainId,
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

            case FinishedWorkUnitLogEvent finishedWorkUnitLogEvent:
                var workUnitInfoList =
                    state.CurrentWorkUnitInfos.FindAll(f => f.GrainId == finishedWorkUnitLogEvent.WorkUnitGrainId);
                foreach (var workUnit in workUnitInfoList)
                {
                    workUnit.UnitStatusEnum = WorkerUnitStatusEnum.Finished;
                }

                state.TermToWorkUnitGrainId.Remove(finishedWorkUnitLogEvent.Term);
                break;

            case WorkflowFinishLogEvent:
                state.WorkflowStatus = WorkflowCoordinatorStatus.Pending;
                state.TermToWorkUnitGrainId = new Dictionary<long, string>();
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
                State.CurrentExecutionRecordId = Guid.Empty;

                break;

            case StartWorkUnitLogEvent workUnitLogEvent:
                var startWorkUnitInfoList =
                    state.CurrentWorkUnitInfos.FindAll(f => f.GrainId == workUnitLogEvent.WorkUnitGrainId);
                foreach (var startWorkUnitInfo in startWorkUnitInfoList)
                {
                    startWorkUnitInfo.UnitStatusEnum = WorkerUnitStatusEnum.InProgress;
                }

                state.TermToWorkUnitGrainId.Add(workUnitLogEvent.Term, workUnitLogEvent.WorkUnitGrainId);
                state.Term += 1;
                break;

            case WorkflowStartLogEvent workflowStartLogEvent:
                state.WorkflowStatus = WorkflowCoordinatorStatus.InProgress;
                state.LastRunningTime = DateTime.UtcNow;
                state.CurrentExecutionRecordId = workflowStartLogEvent.ExecutionRecordId;
                state.RoundId += 1;
                break;

            case ResetWorkflowLogEvent:
                state.WorkflowStatus = WorkflowCoordinatorStatus.Pending;
                state.CurrentWorkUnitInfos.Clear();
                state.BackupWorkUnitInfos.Clear();
                state.CurrentExecutionRecordId = Guid.Empty;
                break;

            case WorkflowStartFailedLogEvent:
                State.WorkflowStatus = WorkflowCoordinatorStatus.Failed;
                State.LastRunningTime = DateTime.UtcNow;
                State.CurrentExecutionRecordId = Guid.Empty;
                break;
        }
    }

    #endregion

    #region private method

    private IEnumerable<WorkUnitInfo> GetNewWorkUnits()
    {
        return State.BackupWorkUnitInfos.Where(backup =>
            !State.CurrentWorkUnitInfos.Any(current => current.GrainId == backup.GrainId));
    }

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
            var grainIdList = TentativeState.GetAllWorkerUnitGrainIds();
            foreach (var grainId in grainIdList)
            {
                var speaker = GrainId.Parse(grainId);
                await PublishP2PAsync(speaker, new GroupChatFinishEvent()
                {
                    BlackboardId = State.BlackboardId
                });
            }
            
            await UnregisterExecutionRecordAsync();

            var toUnregisterWorkUnit = new List<WorkUnitInfo>();
            if (State.BackupWorkUnitInfos.Count > 0)
            {
                toUnregisterWorkUnit = State.CurrentWorkUnitInfos.Where(backup =>
                    State.BackupWorkUnitInfos.All(current => current.GrainId != backup.GrainId)).ToList();
            }

            RaiseEvent(new WorkflowFinishLogEvent());
            await ConfirmEvents();

            await UnregisterWorkUnitAsync(toUnregisterWorkUnit);
            Logger.LogDebug("[WorkflowCoordinatorGAgent] Workflow finished and work units unregistered");
        }

        Logger.LogDebug("[WorkflowCoordinatorGAgent] TryFinishWorkflowAsync end");
    }

    private async Task TryActiveWorkUnitAsync(string workUnitGrainId, string? content = null)
    {
        Logger.LogDebug($"[WorkflowCoordinatorGAgent] Active work:{workUnitGrainId} start");
        if (State.CheckWorkUnitCanProgress(workUnitGrainId) == false)
        {
            Logger.LogDebug($"[WorkflowCoordinatorGAgent] Active work:{workUnitGrainId} fail");
            return;
        }

        // Prepare workflow execution context
        try
        {
            var gAgentFactory = ServiceProvider.GetRequiredService<IGAgentFactory>();
            var workUnitAgent = await gAgentFactory.GetGAgentAsync(GrainId.Parse(workUnitGrainId));

            // Prepare resource context for all workflow units (including AI agents that may need MCP tools)
            var resourceContext = ResourceContext.Create(
                State.GetAllWorkerUnitGrainIds().Select(GrainId.Parse),
                $"workflow:{State.BlackboardId}"
            )
            .WithMetadata("WorkflowId", State.BlackboardId)
            .WithMetadata("InitContent", content ?? State.Content ?? string.Empty);

            // Call PrepareResourceContextAsync for automatic resource discovery (e.g., MCP tool registration)
            await workUnitAgent.PrepareResourceContextAsync(resourceContext);
            Logger.LogInformation("Prepared resource context for workflow unit {WorkUnitGrainId} with {ResourceCount} resources", 
                workUnitGrainId, resourceContext.AvailableResources.Count);

            // // Also call the workflow-specific preparation if the agent implements IWorkflowUnit
            // if (workUnitAgent is IWorkflowUnit workflowUnit)
            // {
            //     // Build execution context, including all workflow nodes as available resources
            //     var workflowContext = new WorkflowExecutionContext
            //     {
            //         WorkflowId = State.BlackboardId, // Use BlackboardId as workflow ID
            //         AvailableResources = State.GetAllWorkerUnitGrainIds()
            //             .Select(GrainId.Parse)
            //             .ToList(),
            //         SharedData = new Dictionary<string, object>
            //         {
            //             ["NodeCapabilities"] = State.NodeCapabilities,
            //             ["InitContent"] = content ?? State.Content ?? string.Empty
            //         }
            //     };
            //
            //     await workflowUnit.PrepareForExecutionAsync(workflowContext);
            //     Logger.LogInformation("Prepared workflow execution context for workflow unit {WorkUnitGrainId}", workUnitGrainId);
            // }
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Failed to prepare execution context for {WorkUnitGrainId}: {ExMessage}", workUnitGrainId, ex.Message);
        }

        var upstreamGrains = State.GetUpStreamGrainIds(workUnitGrainId).Select(s => GrainId.Parse(s).GetGuidKey());
        var blackboard = GrainFactory.GetGrain<IBlackboardGAgent>(State.BlackboardId);
        var messages = await blackboard.GetLastChatMessageAsync(upstreamGrains.ToList());
        if (content != null)
        {
            messages.Add(new ChatMessage { MessageType = MessageType.BlackboardTopic, Content = content });
        }

        var speaker = GrainId.Parse(workUnitGrainId);
        await PublishP2PAsync(speaker, new ChatEvent
        {
            BlackboardId = State.BlackboardId, Speaker = speaker.GetGuidKey(), Term = State.Term,
            CoordinatorMessages = messages
        });

        await PublishAsync(new StartExecuteWorkUnitEvent
        {
            WorkUnitGrainId = speaker.ToString(),
            CoordinatorMessages = messages
        });

        RaiseEvent(new StartWorkUnitLogEvent() { WorkUnitGrainId = workUnitGrainId, Term = State.Term });
        await ConfirmEvents();

        Logger.LogDebug("[WorkflowCoordinatorGAgent] Active work:{WorkUnitGrainId} end", workUnitGrainId);
    }

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

        var workflowUnitGrains = new Dictionary<string, IGAgent>();
        foreach (var unit in workflowUnits)
        {
            if (workflowUnitGrains.ContainsKey(unit.GrainId))
            {
                continue;
            }

            var grainId = GrainId.Parse(unit.GrainId);
            var agent = GrainFactory.GetGrain<IGAgent>(grainId);

            var agentParent = await agent.GetParentAsync();
            if (agentParent != default && agentParent != this.GetGrainId())
            {
                Logger.LogError("[WorkflowCoordinatorGAgent] GAgent {UnitGrainId} already has a parent GAgent.", unit.GrainId);
                throw new ArgumentException($"GAgent {unit.GrainId} already has a parent GAgent.");
            }

            workflowUnitGrains.Add(unit.GrainId, agent);
        }

        foreach (var item in workflowUnitGrains.Values)
        {
            await RegisterAsync(item);
        }

        Logger.LogDebug("[WorkflowCoordinatorGAgent] TryRegisterWorkUnitsAsync end");
    }

    public bool IsAllPathsCanReachTerminal(List<WorkflowUnitDto> workflowUnits)
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

    private async Task UnregisterWorkUnitAsync(List<WorkUnitInfo> workUnitInfos)
    {
        Logger.LogDebug("[WorkflowCoordinatorGAgent] UnregisterWorkUnitAsync start, count: {Count}", workUnitInfos.Count);
        foreach (var agent in workUnitInfos.Select(workUnit => GrainId.Parse(workUnit.GrainId)).Select(grainId => GrainFactory.GetGrain<IGAgent>(grainId)))
        {
            await UnregisterAsync(agent);
        }

        Logger.LogDebug("[WorkflowCoordinatorGAgent] UnregisterWorkUnitAsync end");
    }

    private async Task<Guid> RegisterExecutionRecordAsync(string content)
    {
        if (!State.EnableRunRecord)
        {
            return Guid.Empty;
        }

        var id = Guid.NewGuid();
        var executionRecordAgent = GrainFactory.GetGrain<IWorkflowExecutionRecordGAgent>(id);
        await RegisterAsync(executionRecordAgent);

        await PublishAsync(new StartExecuteWorkflowEvent
        {
            WorkflowId = this.GetPrimaryKey(),
            RoundId = State.RoundId + 1,
            Content = content,
            WorkUnitInfos = State.CurrentWorkUnitInfos
        });
        
        return id;
    }
    
    private async Task UnregisterExecutionRecordAsync()
    {
        if (State.CurrentExecutionRecordId == Guid.Empty)
        {
            return;
        }
        
        var executionRecordAgent = GrainFactory.GetGrain<IWorkflowExecutionRecordGAgent>(State.CurrentExecutionRecordId);
        
        await PublishP2PAsync(executionRecordAgent.GetGrainId(), new GroupChatFinishEvent()
        {
            BlackboardId = State.BlackboardId
        });
        
        await UnregisterAsync(executionRecordAgent);
    }

    #endregion

    #region protected method

    protected async Task PublishP2PAsync<T>(GrainId grainId, T @event) where T : EventBase
    {
        Logger.LogDebug($"[WorkflowCoordinatorGAgent] PublishP2PAsync to {grainId}");
        var grainIdString = grainId.ToString();
        var streamId = StreamId.Create(AevatarOptions!.StreamNamespace,
            grainIdString);
        var stream = StreamProvider.GetStream<EventWrapperBase>(streamId);
        var eventWrapper = new EventWrapper<T>(@event, Guid.NewGuid(), this.GetGrainId());
        await stream.OnNextAsync(eventWrapper);
        Logger.LogDebug($"[WorkflowCoordinatorGAgent] PublishP2PAsync to {grainId} done");
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
}

[GenerateSerializer]
public class ResetWorkflowLogEvent : WorkflowCoordinatorLogEvent;

[GenerateSerializer]
public class WorkflowStartFailedLogEvent : WorkflowCoordinatorLogEvent;