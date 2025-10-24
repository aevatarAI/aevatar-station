using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Placement;
using Aevatar.GAgents.Core;
using Aevatar.GAgents.Workflow.Core;
using Aevatar.GAgents.Workflow.Core.Configs;
using Aevatar.GAgents.Workflow.Core.Events;
using Aevatar.GAgents.Workflow.Core.States;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Aevatar.GAgents.Workflow;

[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent]
[SiloNamePatternPlacement("Projector")]
public class WorkflowViewGAgentPlus : GAgentBasePlus<WorkflowViewStatePlus, WorkflowViewLogEvent, EventBase,
    WorkflowViewConfigDto>, IWorkflowViewGAgentPlus
{
    public WorkflowViewGAgentPlus()
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Workflow View GAgent");
    }

    protected override async Task PerformConfigAsync(WorkflowViewConfigDto configuration)
    {
        await TrySaveWorkflowViewAsync(configuration);
    }

    /// <summary>
    /// Get current round ID (execution counter)
    /// </summary>
    public Task<int> GetCurrentRoundIdAsync()
    {
        return Task.FromResult(State.RoundId);
    }

    /// <summary>
    /// Execute workflow by creating and sending WorkflowEvent to StartAgent, and increment RoundId
    /// Execution name is automatically generated as: {WorkflowName}-Round{RoundId}
    /// </summary>
    public async Task<Guid> ExecuteWorkflowAsync()
    {
        // Validate prerequisites
        if (State.WorkflowStartAgentId == Guid.Empty)
        {
            Logger.LogError("[WorkflowViewGAgent] Cannot execute workflow: WorkflowStartAgentId is not initialized");
            throw new InvalidOperationException("WorkflowStartAgentId is not initialized. Please publish the workflow first.");
        }
        
        // Increment RoundId for each workflow execution
        RaiseEvent(new IncrementRoundIdLogEvent());
        await ConfirmEvents();
        
        // Generate execution name based on workflow name and RoundId
        var executionName = $"{State.Name}-Round{State.RoundId}";
        
        Logger.LogInformation("[WorkflowViewGAgent] Starting workflow execution '{ExecutionName}', RoundId: {RoundId}", 
            executionName, State.RoundId);
        
        // Ensure WorkflowStartAgent is registered as a child (parent-child relationship)
        // This is critical for stream-based event forwarding
        var startAgent = GrainFactory.GetGrain<IWorkflowStartAgent>(State.WorkflowStartAgentId);
        var startAgentGrainId = GrainId.Create(typeof(IWorkflowStartAgent).FullName!, State.WorkflowStartAgentId.ToString("N"));
        
        // Check if WorkflowStartAgent is already a child, if not, register it
        if (!State.Children.Contains(startAgentGrainId))
        {
            Logger.LogWarning("[WorkflowViewGAgent] WorkflowStartAgent not registered as child, registering now (this should have been done during publish)");
            await RegisterAsync(startAgent);
            Logger.LogDebug("[WorkflowViewGAgent] Successfully registered WorkflowStartAgent as child");
        }
        
        // Extract agent type name from GrainId string (format: "Type/Key")
        var startAgentGrainIdStr = startAgentGrainId.ToString();
        var agentTypeName = startAgentGrainIdStr.Contains('/') 
            ? startAgentGrainIdStr.Split('/')[0] 
            : typeof(IWorkflowStartAgent).FullName!;
        
        // Create simplified WorkflowEvent
        var workflowEvent = new WorkflowEvent
        {
            Direction = EventDirection.Down,
            WorkflowId = Guid.NewGuid(), // Temporary workflow ID, WorkflowCoordinator will create actual ExecutionRecord
            WorkUnitAgentId = startAgentGrainId.ToString(), // Full GrainId string for WorkflowCoordinator topology discovery
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            WorkflowAgentStatus = WorkflowAgentStatus.Pending,
            Message = $"Workflow '{executionName}' execution started",
            Metadata = new Dictionary<string, object>
            {
                { "ExecutionName", executionName },
                { "RoundId", State.RoundId },
                { "WorkflowName", State.Name }
            }
        };
        
        // Publish event to child agents (WorkflowStartAgent will receive it because it subscribed to this agent's stream)
        Logger.LogDebug("[WorkflowViewGAgent] Publishing WorkflowEvent to children (WorkflowStartAgent)");
        await PublishEventByDirectionAsync(workflowEvent);
        Logger.LogDebug("[WorkflowViewGAgent] Successfully published WorkflowEvent");
        
        return Guid.NewGuid(); // Return event ID for tracking
    }

    private async Task TrySaveWorkflowViewAsync(WorkflowViewConfigDto configuration)
    {
        if (configuration.WorkflowNodeList.IsNullOrEmpty() || configuration.Name.IsNullOrEmpty())
        {
            return;
        }

        foreach (var node in configuration.WorkflowNodeList)
        {
            if (node.NodeId == Guid.Empty || node.AgentType.IsNullOrEmpty() || node.Name.IsNullOrEmpty())
            {
                throw new ArgumentException("The workflow view node has invalid value.");
            }

            
            
        }

        if (State.WorkflowCoordinatorGAgentId != Guid.Empty && State.WorkflowCoordinatorGAgentId != configuration.WorkflowCoordinatorGAgentId)
        {
            throw new ArgumentException($"WorkflowCoordinatorGAgentId not support change");
        }

        var nodeIdList = configuration.WorkflowNodeList.Select(t => t.NodeId).ToList();
        var notExistedNodeId = configuration.WorkflowNodeUnitList.Where(t =>
            !nodeIdList.Contains(t.NodeId) || !nodeIdList.Contains(t.NextNodeId)).ToList();
        if (notExistedNodeId.Count > 0)
        {
            throw new ArgumentException("The workflow view invalid nodeId.");
        }

        // Detect cycle in edges before proceeding
        if (HasCycle(nodeIdList, configuration.WorkflowNodeUnitList))
        {
            Logger.LogError("[WorkflowViewGAgent] The workflow view contains a cycle.");
            throw new ArgumentException("The workflow view contains a cycle.");
        }

        var addNodeList = new List<WorkflowNodeDto>();
        var updateNodeList = new List<WorkflowNodeDto>();
        foreach (var node in configuration.WorkflowNodeList)
        {
            var stateNode = State.WorkflowNodeList.FirstOrDefault(t => t.NodeId == node.NodeId);
            if (stateNode == null)
            {
                addNodeList.Add(node);
                continue;
            }
            if (stateNode.AgentId != Guid.Empty && node.AgentId != stateNode.AgentId)
            {
                throw new ArgumentException("The workflow node agentId not support change.");
            }
            updateNodeList.Add(node);
        }

        var removeNodeIdList = State.WorkflowNodeList.Select(t => t.NodeId).Except(nodeIdList).ToList();
        
        RaiseEvent(new UpdateWorkflowViewLogEvent
        {
            AddNodeList = addNodeList,
            UpdateNodeList = updateNodeList,
            RemoveNodeIdList = removeNodeIdList,
            WorkflowNodeUnitList = configuration.WorkflowNodeUnitList,
            Name = configuration.Name,
            WorkflowStartAgentId = configuration.WorkflowStartAgentId,
            WorkflowEndAgentId = configuration.WorkflowEndAgentId
        });
        if (configuration.WorkflowCoordinatorGAgentId != Guid.Empty)
        {
            RaiseEvent(new UpdateWorkflowAgentIdLogEvent()
            {
                AgentId = configuration.WorkflowCoordinatorGAgentId
            });
        }

        await ConfirmEvents();
    }

    private static bool HasCycle(IReadOnlyCollection<Guid> nodeIds, IEnumerable<WorkflowNodeUnitDto> units)
    {
        // Build adjacency and in-degree maps
        var adjacency = new Dictionary<Guid, List<Guid>>();
        var inDegree = new Dictionary<Guid, int>();
        foreach (var id in nodeIds)
        {
            adjacency[id] = new List<Guid>();
            inDegree[id] = 0;
        }

        foreach (var unit in units)
        {
            // Self-loop is a cycle
            if (unit.NodeId == unit.NextNodeId)
            {
                return true;
            }
            if (!adjacency.ContainsKey(unit.NodeId) || !inDegree.ContainsKey(unit.NextNodeId))
            {
                // Should not happen due to prior validation, but guard anyway
                continue;
            }
            adjacency[unit.NodeId].Add(unit.NextNodeId);
            inDegree[unit.NextNodeId] = inDegree[unit.NextNodeId] + 1;
        }

        // Kahn's algorithm for cycle detection
        var queue = new Queue<Guid>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var visitedCount = 0;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            visitedCount++;
            foreach (var next in adjacency[current])
            {
                inDegree[next] = inDegree[next] - 1;
                if (inDegree[next] == 0)
                {
                    queue.Enqueue(next);
                }
            }
        }

        return visitedCount < nodeIds.Count;
    }

    protected override void GAgentTransitionState(WorkflowViewStatePlus state,
        StateLogEventBase<WorkflowViewLogEvent> @event)
    {
        switch (@event)
        {
            case UpdateWorkflowViewLogEvent updateWorkflowViewLogEvent:
                foreach (var removeNodeId in updateWorkflowViewLogEvent.RemoveNodeIdList)
                {
                    var removeNode = state.WorkflowNodeList.FirstOrDefault(t => t.NodeId == removeNodeId);
                    if (removeNode != null)
                    {
                        state.WorkflowNodeList.Remove(removeNode);
                    }
                }
                foreach (var node in updateWorkflowViewLogEvent.UpdateNodeList)
                {
                    var updateNode = state.WorkflowNodeList.FirstOrDefault(t => t.NodeId == node.NodeId);
                    if (updateNode != null)
                    {
                        updateNode.Name = node.Name;
                        updateNode.ExtendedData = node.ExtendedData;
                        updateNode.AgentId = node.AgentId;
                        updateNode.JsonProperties = node.JsonProperties;
                    }
                }
                state.WorkflowNodeList.AddRange(updateWorkflowViewLogEvent.AddNodeList);
                state.WorkflowNodeUnitList = updateWorkflowViewLogEvent.WorkflowNodeUnitList;
                state.Name = updateWorkflowViewLogEvent.Name;
                state.AgentId = this.GetPrimaryKey();
                state.WorkflowStartAgentId = updateWorkflowViewLogEvent.WorkflowStartAgentId;
                state.WorkflowEndAgentId = updateWorkflowViewLogEvent.WorkflowEndAgentId;
                break;
            case UpdateNodeAgentIdLogEvent nodeAgentIdLogEvent:
                var updateAgentIdNode = state.WorkflowNodeList.FirstOrDefault(t => t.NodeId == nodeAgentIdLogEvent.NodeId);
                if (updateAgentIdNode != null)
                {
                    updateAgentIdNode.AgentId = nodeAgentIdLogEvent.AgentId;
                }
                break;
            case UpdateWorkflowAgentIdLogEvent updateWorkflowAgentIdLogEvent:
                state.WorkflowCoordinatorGAgentId = updateWorkflowAgentIdLogEvent.AgentId;
                break;
            case IncrementRoundIdLogEvent:
                state.RoundId++;
                break;
        }

        base.GAgentTransitionState(state, @event);
    }
}