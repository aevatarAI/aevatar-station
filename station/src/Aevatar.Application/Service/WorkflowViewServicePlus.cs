using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Workflow;
using Aevatar.GAgents.Workflow.Core;
using Aevatar.GAgents.Workflow.Core.Configs;
using Aevatar.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;

[RemoteService(IsEnabled = false)]
public class WorkflowViewServicePlus : ApplicationService, IWorkflowViewService
{
    private readonly IAgentService _agentService;
    private readonly IGAgentFactory<IGAgentPlus> _gAgentFactory;
    private readonly ILogger<WorkflowViewServicePlus> _logger;
    private readonly DebugModeOptions _debugModeOptions;

    public WorkflowViewServicePlus(IAgentService agentService, IGAgentFactory<IGAgentPlus> gAgentFactory,  ILogger<WorkflowViewServicePlus> logger,
        IOptionsSnapshot<DebugModeOptions> debugModeOptions)
    {
        _agentService = agentService;
        _gAgentFactory = gAgentFactory;
        _logger = logger;
        _debugModeOptions = debugModeOptions == null ? new DebugModeOptions() : debugModeOptions.Value;
    }
    
    public async Task<AgentDto> PublishWorkflowAsync(Guid viewAgentId)
    {
        var agentDto = await _agentService.GetAgentAsync(viewAgentId);
        var configJson = JsonConvert.SerializeObject(agentDto.Properties);
        WorkflowViewConfigDto? viewConfigDto;
        try
        {
            viewConfigDto = JsonConvert.DeserializeObject<WorkflowViewConfigDto>(configJson);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Invalid WorkflowView Properties: {configJson}", configJson);
            throw new UserFriendlyException("Invalid WorkflowView Properties");
        }
            
        if (viewConfigDto == null)
        {
            return new AgentDto();
        }

        // create or update subAgent
        foreach (var workflowNode in viewConfigDto.WorkflowNodeList)
        {
            var nodeAgentProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowNode.JsonProperties);
            if (workflowNode.AgentId == Guid.Empty || (await _agentService.GetAgentAsync(workflowNode.AgentId)).AgentType.IsNullOrEmpty())
            {
                _logger.LogInformation("workflowViewGAgent {viewAgentId} create {agentType} begin.", viewAgentId, workflowNode.AgentType);
                try
                {
                    var subAgentDto = await _agentService.CreateAgentAsync(new CreateAgentInputDto()
                    {
                        AgentId = workflowNode.AgentId == Guid.Empty ? null : workflowNode.AgentId,
                        Name = workflowNode.Name,
                        Properties = nodeAgentProperties,
                        AgentType = workflowNode.AgentType
                    });
                    workflowNode.AgentId = subAgentDto.AgentGuid;
                }
                catch (Exception e)
                {
                   _logger.LogError(e, "workflowViewGAgent {viewAgentId} create {agentType} fail: {message}", viewAgentId, workflowNode.AgentType, e.Message);
                   throw new UserFriendlyException($"Create {workflowNode.AgentType} fail: {e.Message}");
                }
                
                _logger.LogInformation("workflowViewGAgent {viewAgentId} create {agentType} agentId {agentId} success.", 
                    viewAgentId, workflowNode.AgentType, workflowNode.AgentId);
            }
            else
            {
                _logger.LogInformation("workflowViewGAgent {viewAgentId} update {agentType} {agentId} begin.", 
                    viewAgentId, workflowNode.AgentType, workflowNode.AgentId);
                try
                {
                    await _agentService.UpdateAgentAsync(workflowNode.AgentId, new UpdateAgentInputDto()
                    {
                        Name = workflowNode.Name,
                        Properties = nodeAgentProperties
                    });
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "workflowViewGAgent {viewAgentId} update {agentType} {agentId} fail: {message}", viewAgentId, workflowNode.AgentType, workflowNode.AgentId,  e.Message);
                    throw new UserFriendlyException($"Update {workflowNode.AgentType} {workflowNode.AgentId} fail: {e.Message}");
                }
                _logger.LogInformation("workflowViewGAgent {viewAgentId} update {agentType} {agentId} success.", 
                    viewAgentId, workflowNode.AgentType, workflowNode.AgentId);
            }
        }
        
        // create or update workflowCoordinatorGAgent
        var workflowConfig = new WorkflowCoordinatorConfigDto();
        // ✅ FIXED: Always enable execution recording for Plus system
        workflowConfig.EnableExecutionRecord = true;
        var nodeMap = viewConfigDto.WorkflowNodeList.ToDictionary(r => r.NodeId, r => r);
        foreach (var node in viewConfigDto.WorkflowNodeList)
        {
            var nodeUnitList = viewConfigDto.WorkflowNodeUnitList.Where(t => t.NodeId == node.NodeId).ToList();
            if (nodeUnitList.IsNullOrEmpty())
            {
                workflowConfig.WorkflowUnitList.Add(new WorkflowUnitDto()
                {
                    ExtendedData = node.ExtendedData,
                    GrainId = GrainId.Create(node.AgentType, GuidUtil.GuidToGrainKey(node.AgentId)).ToString(),
                    NextGrainId = "",
                    AgentName = node.Name
                });
                continue;
            }
            foreach (var nodeUnit in nodeUnitList)
            {
                var nextNode = nodeMap[nodeUnit.NextNodeId];
                workflowConfig.WorkflowUnitList.Add(new WorkflowUnitDto()
                {
                    ExtendedData = node.ExtendedData,
                    GrainId = GrainId.Create(node.AgentType, GuidUtil.GuidToGrainKey(node.AgentId)).ToString(),
                    NextGrainId = GrainId.Create(nextNode.AgentType, GuidUtil.GuidToGrainKey(nextNode.AgentId)).ToString(),
                    AgentName = node.Name
                });
            }
        }
        
        var workflowConfigJson = JsonConvert.SerializeObject(workflowConfig);
        var workflowConfigProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(workflowConfigJson);
        workflowConfigProperties.Remove("PublisherGrainId");
        workflowConfigProperties.Remove("CorrelationId");

        _logger.LogInformation("workflowViewGAgent {viewAgentId}  workflowCoordinatorGAgent properties {workflowConfigJson}.", 
            viewAgentId, workflowConfigJson);
        
        var workflowCoordinatorGAgentId = viewConfigDto.WorkflowCoordinatorGAgentId;
        if (workflowCoordinatorGAgentId == Guid.Empty)
        {
            _logger.LogInformation("workflowViewGAgent {viewAgentId} create workflowCoordinatorGAgent begin.", 
                viewAgentId);
            var emptyWorkflowCoordinatorGAgent = await _gAgentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgentPlus>(Guid.Empty);
            AgentDto workflowCoordinatorGAgentDto;
            try
            {
                workflowCoordinatorGAgentDto = await _agentService.CreateAgentAsync(new CreateAgentInputDto()
                {
                    Name = agentDto.Name,
                    AgentType = emptyWorkflowCoordinatorGAgent.GetGrainId().Type.ToString(),
                    Properties = workflowConfigProperties
                });
                viewConfigDto.WorkflowCoordinatorGAgentId = workflowCoordinatorGAgentDto.AgentGuid;
                _logger.LogInformation("workflowViewGAgent {viewAgentId} create workflowCoordinatorGAgent {AgentId} success.", 
                    viewAgentId, viewConfigDto.WorkflowCoordinatorGAgentId);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "workflowViewGAgent {viewAgentId} create workflowCoordinatorGAgent fail: {message}", viewAgentId, e.Message);
                throw new UserFriendlyException($"create workflowCoordinatorGAgent fail: {e.Message}");
            }

           // await _agentService.AddSubAgentAsync(workflowCoordinatorGAgentDto.AgentGuid, new AddSubAgentDto());
        }
        else
        {
            _logger.LogInformation("workflowViewGAgent {viewAgentId} update workflowCoordinatorGAgent {AgentId} success.", 
                viewAgentId, viewConfigDto.WorkflowCoordinatorGAgentId);
            try
            {
                await _agentService.UpdateAgentAsync(workflowCoordinatorGAgentId, new UpdateAgentInputDto()
                {
                    Name = agentDto.Name,
                    Properties = workflowConfigProperties
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "workflowViewGAgent {viewAgentId} update workflowCoordinatorGAgent {agentId} fail: {message}", viewAgentId, workflowCoordinatorGAgentId, e.Message);
                throw new UserFriendlyException($"update workflowCoordinatorGAgent {viewConfigDto.WorkflowCoordinatorGAgentId} fail: {e.Message}");
            }
            _logger.LogInformation("workflowViewGAgent {viewAgentId} update workflowCoordinatorGAgent {AgentId} success.", 
                viewAgentId, viewConfigDto.WorkflowCoordinatorGAgentId);
        }

        // Initialize workflow agent IDs (WorkflowStartAgentId, WorkflowEndAgentId) before updating
        InitializeWorkflowAgentIds(viewConfigDto);
        
        // update workflowViewAgent with initialized IDs
        configJson = JsonConvert.SerializeObject(viewConfigDto);
        var viewConfigProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);
        viewConfigProperties.Remove("PublisherGrainId");
        viewConfigProperties.Remove("CorrelationId");
        agentDto = await _agentService.UpdateAgentAsync(viewAgentId, new UpdateAgentInputDto()
        {
            Properties = viewConfigProperties,
            Name = agentDto.Name
        });
        
        // Setup workflow relationships (Start/End/Coordinator + business agents)
        await SetupWorkflowRelationshipsAsync(viewAgentId, viewConfigDto);
        
        return agentDto;
    }

    public async Task<AgentDto> CreateDefaultWorkflowAsync()
    {
        var emptyWorkflowViewGAgent = await _gAgentFactory.GetGAgentAsync<WorkflowViewGAgentPlus>(Guid.Empty);
        string workflowAgentType;
        try
        {
            workflowAgentType = emptyWorkflowViewGAgent.GetGrainId().Type.ToString();
        }
        catch (Exception)
        {
            _logger.LogWarning("Failed to get GrainId.Type for WorkflowViewGAgent; falling back to type name.");
            workflowAgentType = typeof(WorkflowViewGAgentPlus).FullName ?? typeof(WorkflowViewGAgentPlus).Name;
        }
        var workflowViewList = await _agentService.GetAllAgentInstances(new GetAllAgentInstancesQueryDto()
        {
            AgentType = workflowAgentType,
            PageSize = 1
        });
        if (workflowViewList.Count > 0)
        {
            throw new UserFriendlyException("User have workflow already.");
        }
        var properties = JsonConvert.DeserializeObject<Dictionary<string, object>?>(DefaultWorkflowProperties);
        var agentDto = await _agentService.CreateAgentAsync(new CreateAgentInputDto()
        {
            Name = properties!["name"].ToString()!,
            AgentType = workflowAgentType!,
            Properties = properties
        });
        return agentDto;
    }

    /// <summary>
    /// Setup workflow relationships with incremental updates
    /// Only updates relationships that have changed to improve performance
    /// </summary>
    private async Task SetupWorkflowRelationshipsAsync(Guid viewAgentId, WorkflowViewConfigDto viewConfigDto)
    {
        _logger.LogInformation("Setting up workflow relationships for workflow {WorkflowName}", viewConfigDto.Name);
        
        // Step 1: Get all agent references
        var agents = await GetWorkflowAgentsAsync(viewAgentId, viewConfigDto);
        
        // Step 2: Analyze workflow topology
        var topology = AnalyzeWorkflowTopology(viewConfigDto);
        
        // Step 3: Setup business agent relationships (includes Coordinator relationships, incremental update)
        await SetupBusinessAgentTopologyAsync(agents, topology, viewConfigDto);
        
        // Step 3.5: Setup WorkflowViewAgent → WorkflowStartAgent relationship
        // This is critical - WorkflowStartAgent must subscribe to WorkflowViewAgent's stream to receive ExecuteWorkflow events
        await UpdateAgentChildrenAsync(agents.WorkflowViewAgent, new List<GrainId> { agents.StartAgent.GetGrainId() }, 
            "WorkflowViewAgent", "WorkflowStartAgent");
        
        // Step 4: Setup Coordinator → WorkflowViewAgent relationship
        await UpdateAgentChildrenAsync(agents.CoordinatorAgent, new List<GrainId> { agents.WorkflowViewAgent.GetGrainId() }, 
            "Coordinator", "WorkflowViewAgent");
        
        _logger.LogInformation("Workflow relationships setup completed successfully for workflow {WorkflowName}", viewConfigDto.Name);
    }
    
    /// <summary>
    /// Initialize workflow agent IDs if needed
    /// </summary>
    private void InitializeWorkflowAgentIds(WorkflowViewConfigDto viewConfigDto)
    {
        if (viewConfigDto.WorkflowStartAgentId == Guid.Empty)
        {
            viewConfigDto.WorkflowStartAgentId = Guid.NewGuid();
            _logger.LogInformation("Created new WorkflowStartAgent ID: {AgentId}", viewConfigDto.WorkflowStartAgentId);
        }
        
        if (viewConfigDto.WorkflowEndAgentId == Guid.Empty)
        {
            viewConfigDto.WorkflowEndAgentId = Guid.NewGuid();
            _logger.LogInformation("Generated new WorkflowEndAgent ID: {AgentId}", viewConfigDto.WorkflowEndAgentId);
        }
        
        if (viewConfigDto.WorkflowCoordinatorGAgentId == Guid.Empty)
        {
            _logger.LogError("WorkflowCoordinatorGAgentId is not initialized");
            throw new UserFriendlyException("WorkflowCoordinatorGAgentId is not initialized");
        }
    }
    
    /// <summary>
    /// Get all workflow agent references
    /// </summary>
    private async Task<WorkflowAgents> GetWorkflowAgentsAsync(Guid viewAgentId, WorkflowViewConfigDto viewConfigDto)
    {
        return new WorkflowAgents
        {
            StartAgent = await _gAgentFactory.GetGAgentAsync<IWorkflowStartAgent>(viewConfigDto.WorkflowStartAgentId),
            EndAgent = await _gAgentFactory.GetGAgentAsync<IWorkflowEndAgent>(viewConfigDto.WorkflowEndAgentId),
            CoordinatorAgent = await _gAgentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgentPlus>(viewConfigDto.WorkflowCoordinatorGAgentId),
            WorkflowViewAgent = await _gAgentFactory.GetGAgentAsync<IWorkflowViewGAgentPlus>(viewAgentId)
        };
    }
    
    /// <summary>
    /// Analyze workflow topology to find top-level and leaf nodes
    /// </summary>
    private WorkflowTopology AnalyzeWorkflowTopology(WorkflowViewConfigDto viewConfigDto)
    {
        var nodeMap = viewConfigDto.WorkflowNodeList.ToDictionary(r => r.NodeId, r => r);
        var downstreamNodeIds = viewConfigDto.WorkflowNodeUnitList.Select(u => u.NextNodeId).ToHashSet();
        var upstreamNodeIds = viewConfigDto.WorkflowNodeUnitList.Select(u => u.NodeId).ToHashSet();
        
        var topLevelNodes = viewConfigDto.WorkflowNodeList
            .Where(n => !downstreamNodeIds.Contains(n.NodeId))
            .ToList();
        
        var leafNodes = viewConfigDto.WorkflowNodeList
            .Where(n => !upstreamNodeIds.Contains(n.NodeId))
            .ToList();
        
        _logger.LogInformation("Found {TopCount} top-level nodes and {LeafCount} leaf nodes", 
            topLevelNodes.Count, leafNodes.Count);
        
        return new WorkflowTopology
        {
            NodeMap = nodeMap,
            TopLevelNodes = topLevelNodes,
            LeafNodes = leafNodes
        };
    }
    
    /// <summary>
    /// Setup business agent topology with incremental updates
    /// </summary>
    private async Task SetupBusinessAgentTopologyAsync(WorkflowAgents agents, WorkflowTopology topology, WorkflowViewConfigDto viewConfigDto)
    {
        var coordinatorGrainId = agents.CoordinatorAgent.GetGrainId();
        
        // Update StartAgent → Top-level business agents + Coordinator
        var expectedStartChildren = topology.TopLevelNodes
            .Select(n => GrainId.Create(n.AgentType, GuidUtil.GuidToGrainKey(n.AgentId)))
            .ToList();
        expectedStartChildren.Add(coordinatorGrainId);
        await UpdateAgentChildrenAsync(agents.StartAgent, expectedStartChildren, "StartAgent", "top-level agents + Coordinator");
        
        // Update business agent topology (business agent → next agent + Coordinator)
        foreach (var nodeUnit in viewConfigDto.WorkflowNodeUnitList)
        {
            var sourceNode = topology.NodeMap[nodeUnit.NodeId];
            var targetNode = topology.NodeMap[nodeUnit.NextNodeId];
            
            var sourceGrainId = GrainId.Create(sourceNode.AgentType, GuidUtil.GuidToGrainKey(sourceNode.AgentId));
            var targetGrainId = GrainId.Create(targetNode.AgentType, GuidUtil.GuidToGrainKey(targetNode.AgentId));
            
            var sourceAgent = await _gAgentFactory.GetGAgentAsync(sourceGrainId);
            
            await UpdateAgentChildrenAsync(sourceAgent, new List<GrainId> { targetGrainId, coordinatorGrainId }, 
                $"BusinessAgent({sourceNode.Name})", $"{targetNode.Name} + Coordinator");
        }
        
        // Update Leaf agents → EndAgent + Coordinator
        foreach (var leafNode in topology.LeafNodes)
        {
            var leafGrainId = GrainId.Create(leafNode.AgentType, GuidUtil.GuidToGrainKey(leafNode.AgentId));
            var leafAgent = await _gAgentFactory.GetGAgentAsync(leafGrainId);
            await UpdateAgentChildrenAsync(leafAgent, new List<GrainId> { agents.EndAgent.GetGrainId(), coordinatorGrainId }, 
                $"LeafAgent({leafNode.Name})", "EndAgent + Coordinator");
        }
        
        // Update EndAgent → Coordinator
        await UpdateAgentChildrenAsync(agents.EndAgent, new List<GrainId> { coordinatorGrainId }, 
            "EndAgent", "Coordinator");
    }
    
    /// <summary>
    /// Update agent children with incremental logic (only add/remove changes)
    /// </summary>
    private async Task UpdateAgentChildrenAsync(IGAgentPlus parentAgent, List<GrainId> expectedChildGrainIds, 
        string parentName, string childrenDesc)
    {
        var currentChildren = await parentAgent.GetChildrenAsync();
        var currentChildIdSet = currentChildren.ToHashSet();
        var expectedChildIdSet = expectedChildGrainIds.ToHashSet();
        
        // Find children to add (in expected but not in current)
        var toAdd = expectedChildGrainIds.Where(id => !currentChildIdSet.Contains(id)).ToList();
        
        // Find children to remove (in current but not in expected)
        var toRemove = currentChildren.Where(id => !expectedChildIdSet.Contains(id)).ToList();
        
        // Skip if no changes
        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            _logger.LogDebug("{ParentName} → {ChildrenDesc}: No changes needed", parentName, childrenDesc);
            return;
        }
        
        // Remove outdated children
        foreach (var childGrainIdToRemove in toRemove)
        {
            var childAgent = await _gAgentFactory.GetGAgentAsync(childGrainIdToRemove);
            await parentAgent.UnregisterAsync(childAgent);
            _logger.LogInformation("Unregistered {ParentName} → {ChildId}", parentName, childGrainIdToRemove);
        }
        
        // Add new children
        foreach (var childGrainIdToAdd in toAdd)
        {
            var childAgent = await _gAgentFactory.GetGAgentAsync(childGrainIdToAdd);
            await parentAgent.RegisterAsync(childAgent);
            _logger.LogInformation("Registered {ParentName} → {ChildrenDesc}", parentName, childrenDesc);
        }
        
        _logger.LogInformation("{ParentName} → {ChildrenDesc}: Added {AddCount}, Removed {RemoveCount}", 
            parentName, childrenDesc, toAdd.Count, toRemove.Count);
    }
    
    /// <summary>
    /// Helper class to hold workflow agent references
    /// </summary>
    private class WorkflowAgents
    {
        public IWorkflowStartAgent StartAgent { get; init; }
        public IWorkflowEndAgent EndAgent { get; init; }
        public IWorkflowCoordinatorGAgentPlus CoordinatorAgent { get; init; }
        public IWorkflowViewGAgentPlus WorkflowViewAgent { get; init; }
    }
    
    /// <summary>
    /// Helper class to hold workflow topology analysis results
    /// </summary>
    private class WorkflowTopology
    {
        public Dictionary<Guid, WorkflowNodeDto> NodeMap { get; init; }
        public List<WorkflowNodeDto> TopLevelNodes { get; init; }
        public List<WorkflowNodeDto> LeafNodes { get; init; }
    }

    private const string DefaultWorkflowProperties =
        "{\"workflowNodeList\":[{\"agentType\":\"Aevatar.GAgents.InputGAgent.GAgent.InputGAgent\",\"name\":\"MyInputGAgent\",\"extendedData\":{\"xPosition\":\"2\",\"yPosition\":\"16\"},\"jsonProperties\":\"{\\\"memberName\\\":\\\"inputGAgent1\\\",\\\"input\\\":\\\"I want to eat, get me a choose.\\\"}\",\"nodeId\":\"45dc7d32-1002-4479-8616-b12cbc112bb4\"},{\"agentType\":\"Aevatar.GAgents.Twitter.GAgents.ChatAIAgent.ChatAIGAgent\",\"name\":\"ai\",\"extendedData\":{\"xPosition\":\"365.1172008973645\",\"yPosition\":\"-12.62092346330003\"},\"jsonProperties\":\"{\\\"memberName\\\":\\\"ai\\\",\\\"instructions\\\":\\\"You are a helpful AI assistant\\\",\\\"systemLLM\\\":\\\"OpenAI\\\",\\\"mcpServers\\\":[],\\\"toolGAgentTypes\\\":[],\\\"toolGAgents\\\":[]}\",\"nodeId\":\"6c15ac63-ce9b-4ef2-a982-171e4ed94bdb\"}],\"workflowNodeUnitList\":[{\"nodeId\":\"45dc7d32-1002-4479-8616-b12cbc112bb4\",\"nextNodeId\":\"6c15ac63-ce9b-4ef2-a982-171e4ed94bdb\"}],\"name\":\"default workflow\"}";
}