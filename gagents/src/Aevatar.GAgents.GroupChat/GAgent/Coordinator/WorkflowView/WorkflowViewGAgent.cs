using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.Dto;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.LogEvent;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView;

[GAgent]
public class WorkflowViewGAgent : GAgentBase<WorkflowViewState, WorkflowViewLogEvent, EventBase,
    WorkflowViewConfigDto>, IWorkflowViewGAgent
{
    public WorkflowViewGAgent()
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

            if (node.AgentId != Guid.Empty)
            {
                var grainId = GrainId.Create(node.AgentType, node.AgentId.ToString("N"));
                var agent = GrainFactory.GetGrain<IGAgent>(grainId);
                var agentParent = await agent.GetParentAsync();
                if (agentParent != default && State.WorkflowCoordinatorGAgentId != Guid.Empty && State.WorkflowCoordinatorGAgentId != agentParent.GetGuidKey())
                {
                    Logger.LogError($"[WorkflowViewGAgent] GAgent {grainId} already has a parent GAgent.");
                    throw new ArgumentException($"GAgent {grainId} already has a parent GAgent.");
                }
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
            Name = configuration.Name
        });
        if (configuration.WorkflowCoordinatorGAgentId != Guid.Empty)
        {
            RaiseEvent(new UpdateWorkflowAgentIdLogEvent()
            {
                AgentId = configuration.WorkflowCoordinatorGAgentId
            });
        }
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

    protected override void GAgentTransitionState(WorkflowViewState state,
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
        }

        base.GAgentTransitionState(state, @event);
    }
}

public interface IWorkflowViewGAgent : IStateGAgent<WorkflowViewState>
{
}