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
using Aevatar.Station.Feature.CreatorGAgent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Orleans;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Application.Services;
using ICreatorGAgent = Aevatar.Application.Grains.Agents.Creator.ICreatorGAgent;

namespace Aevatar.Service;

[RemoteService(IsEnabled = false)]
public class WorkflowViewServicePlus : ApplicationService, IWorkflowViewService
{
    private readonly IAgentService _agentService;
    private readonly IGAgentFactory<IGAgentPlus> _gAgentFactory;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<WorkflowViewServicePlus> _logger;
    private readonly DebugModeOptions _debugModeOptions;

    public WorkflowViewServicePlus(IAgentService agentService, IGAgentFactory<IGAgentPlus> gAgentFactory, IClusterClient clusterClient, ILogger<WorkflowViewServicePlus> logger,
        IOptionsSnapshot<DebugModeOptions> debugModeOptions)
    {
        _agentService = agentService;
        _gAgentFactory = gAgentFactory;
        _clusterClient = clusterClient;
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

    // Config workflow agent IDs: query state, supplement viewConfigDto, and persist
    await WorkflowViewAgentConfigAsync(viewAgentId, viewConfigDto);
    
    // Setup workflow relationships (Start/End/Coordinator + business agents)
    await SetupWorkflowRelationshipsAsync(viewAgentId, viewConfigDto);
    
    // Update agentDto.Properties with the updated viewConfigDto (which now has the correct IDs)
    var jsonSerializerSettings = new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore
    };
    var serializedConfig = JsonConvert.SerializeObject(viewConfigDto, jsonSerializerSettings);
    agentDto.Properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedConfig);
    
    _logger.LogInformation("[PublishWorkflow] Updated agentDto.Properties - StartId={StartId}, EndId={EndId}, CoordinatorId={CoordinatorId}",
        viewConfigDto.WorkflowStartAgentId, viewConfigDto.WorkflowEndAgentId, viewConfigDto.WorkflowCoordinatorGAgentId);
        
    // Persist updated properties to CreatorGAgent
    var creatorGAgent = _clusterClient.GetGrain<ICreatorGAgent>(viewAgentId);
    await creatorGAgent.UpdateAgentAsync(new UpdateAgentInput
    {
        Name = agentDto.Name,
        Properties = serializedConfig
    });
    
    _logger.LogInformation("[PublishWorkflow] Persisted updated configuration to CreatorGAgent");
        
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
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] START - Workflow: {WorkflowName}, ViewAgentId: {ViewAgentId}", 
            viewConfigDto.Name, viewAgentId);
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] Config - NodeCount: {NodeCount}, UnitCount: {UnitCount}", 
            viewConfigDto.WorkflowNodeList.Count, viewConfigDto.WorkflowNodeUnitList.Count);
        
        // Step 1: Get all agent references
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] Step 1: Getting agent references...");
        var agents = await GetWorkflowAgentsAsync(viewAgentId, viewConfigDto);
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] Step 1: Got agents - Start: {StartId}, End: {EndId}, Coordinator: {CoordId}", 
            agents.StartAgent.GetGrainId(), agents.EndAgent.GetGrainId(), agents.CoordinatorAgent.GetGrainId());
        
        // Step 2: Analyze workflow topology
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] Step 2: Analyzing topology...");
        var topology = AnalyzeWorkflowTopology(viewConfigDto);
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] Step 2: Topology - BusinessNodes: {BusinessCount}, LeafNodes: {LeafCount}", 
            topology.NodeMap.Count, topology.LeafNodes.Count);
        
        // Step 3: Setup business agent relationships (includes Coordinator relationships, incremental update)
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] Step 3: Setting up business agent topology...");
        await SetupBusinessAgentTopologyAsync(agents, topology, viewConfigDto);
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] Step 3: ✅ Business agent topology setup completed");
        
        // Step 3.5: Setup WorkflowViewAgent → WorkflowStartAgent relationship
        // This is critical - WorkflowStartAgent must subscribe to WorkflowViewAgent's stream to receive ExecuteWorkflow events
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] Step 3.5: Setting up ViewAgent → StartAgent...");
        await UpdateAgentChildrenAsync(agents.WorkflowViewAgent, new List<GrainId> { agents.StartAgent.GetGrainId() }, 
            "WorkflowViewAgent", "WorkflowStartAgent");
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] Step 3.5: ✅ ViewAgent → StartAgent completed");
        
        // Step 4: Setup Coordinator → WorkflowViewAgent relationship
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] Step 4: Setting up Coordinator → ViewAgent...");
        await UpdateAgentChildrenAsync(agents.CoordinatorAgent, new List<GrainId> { agents.WorkflowViewAgent.GetGrainId() }, 
            "Coordinator", "WorkflowViewAgent");
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] Step 4: ✅ Coordinator → ViewAgent completed");
        
        _logger.LogInformation("🔧 [SetupWorkflowRelationships] ✅ COMPLETED - Workflow: {WorkflowName}", viewConfigDto.Name);
    }
    
    /// <summary>
    /// Config workflow agent IDs: query state, supplement viewConfigDto, and persist to WorkflowViewGAgent.State
    /// </summary>
    private async Task WorkflowViewAgentConfigAsync(Guid viewAgentId, WorkflowViewConfigDto viewConfigDto)
    {
        _logger.LogInformation("[WorkflowViewAgentConfig] Starting for viewAgentId={ViewId}, Input: StartId={StartId}, EndId={EndId}, CoordinatorId={CoordinatorId}",
            viewAgentId, viewConfigDto.WorkflowStartAgentId, viewConfigDto.WorkflowEndAgentId, viewConfigDto.WorkflowCoordinatorGAgentId);
        
        var agentDto = await _agentService.GetAgentAsync(viewAgentId);
        var workflowViewAgent = await _gAgentFactory.GetGAgentAsync<IWorkflowViewGAgentPlus>(agentDto.GrainId);
        var state = await workflowViewAgent.GetStateAsync();
        
        _logger.LogInformation("[WorkflowViewAgentConfig] Current State: StartId={StartId}, EndId={EndId}, CoordinatorId={CoordinatorId}",
            state.WorkflowStartAgentId, state.WorkflowEndAgentId, state.WorkflowCoordinatorGAgentId);
        
        // Supplement viewConfigDto with State values (if State has valid IDs)
        if (state.WorkflowStartAgentId != Guid.Empty)
        {
            viewConfigDto.WorkflowStartAgentId = state.WorkflowStartAgentId;
            _logger.LogInformation("[WorkflowViewAgentConfig] Using existing StartAgentId from State: {Id}", state.WorkflowStartAgentId);
        }
        else if (viewConfigDto.WorkflowStartAgentId == Guid.Empty)
        {
            viewConfigDto.WorkflowStartAgentId = Guid.NewGuid();
            _logger.LogInformation("[WorkflowViewAgentConfig] Generated new StartAgentId: {Id}", viewConfigDto.WorkflowStartAgentId);
        }
        
        if (state.WorkflowEndAgentId != Guid.Empty)
        {
            viewConfigDto.WorkflowEndAgentId = state.WorkflowEndAgentId;
            _logger.LogInformation("[WorkflowViewAgentConfig] Using existing EndAgentId from State: {Id}", state.WorkflowEndAgentId);
        }
        else if (viewConfigDto.WorkflowEndAgentId == Guid.Empty)
        {
            viewConfigDto.WorkflowEndAgentId = Guid.NewGuid();
            _logger.LogInformation("[WorkflowViewAgentConfig] Generated new EndAgentId: {Id}", viewConfigDto.WorkflowEndAgentId);
        }
        
        if (state.WorkflowCoordinatorGAgentId != Guid.Empty)
        {
            viewConfigDto.WorkflowCoordinatorGAgentId = state.WorkflowCoordinatorGAgentId;
            _logger.LogInformation("[WorkflowViewAgentConfig] Using existing CoordinatorId from State: {Id}", state.WorkflowCoordinatorGAgentId);
        }
        
        _logger.LogInformation("[WorkflowViewAgentConfig] Before ConfigAsync: StartId={StartId}, EndId={EndId}, CoordinatorId={CoordinatorId}",
            viewConfigDto.WorkflowStartAgentId, viewConfigDto.WorkflowEndAgentId, viewConfigDto.WorkflowCoordinatorGAgentId);
        
        // Persist to WorkflowViewGAgent.State only
        await workflowViewAgent.ConfigAsync(viewConfigDto);
        
        _logger.LogInformation("[WorkflowViewAgentConfig] Completed ConfigAsync");
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
    /// ✅ SIMPLE: Clean up orphaned parent relationships
    /// If a node's parent no longer exists in the topology, remove that parent relationship
    /// </summary>
    private async Task CleanupStaleParentRelationshipsAsync(WorkflowTopology topology, WorkflowViewConfigDto viewConfigDto)
    {
        // Get all valid business node GrainIds in current topology
        var validNodeGrainIds = topology.NodeMap.Values
            .Select(n => GrainId.Create(n.AgentType, GuidUtil.GuidToGrainKey(n.AgentId)))
            .ToHashSet();
        
        // For each node, clean up parents that no longer exist in topology
        foreach (var node in topology.NodeMap.Values)
        {
            var nodeGrainId = GrainId.Create(node.AgentType, GuidUtil.GuidToGrainKey(node.AgentId));
            var nodeAgent = await _gAgentFactory.GetGAgentAsync(nodeGrainId);
            var currentParents = await nodeAgent.GetParentsAsync();
            
            // Find orphaned parents (parents that no longer exist in topology)
            var orphanedParents = currentParents.Where(p => !validNodeGrainIds.Contains(p)).ToList();
            
            if (orphanedParents.Count > 0)
            {
                _logger.LogInformation("🧹 [CleanupOrphanedParents] {NodeName} has {Count} orphaned parent(s), cleaning up...", 
                    node.Name, orphanedParents.Count);
                
                foreach (var orphanedParentGrainId in orphanedParents)
                {
                    try
                    {
                        var orphanedParentAgent = await _gAgentFactory.GetGAgentAsync(orphanedParentGrainId);
                        await orphanedParentAgent.UnregisterAsync(nodeAgent);
                        _logger.LogInformation("🧹 [CleanupOrphanedParents]   ✅ Removed orphaned parent: {Parent}", orphanedParentGrainId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "🧹 [CleanupOrphanedParents]   ⚠️ Failed to remove orphaned parent {Parent}", orphanedParentGrainId);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Setup business agent topology with incremental updates
    /// </summary>
    private async Task SetupBusinessAgentTopologyAsync(WorkflowAgents agents, WorkflowTopology topology, WorkflowViewConfigDto viewConfigDto)
    {
        var coordinatorGrainId = agents.CoordinatorAgent.GetGrainId();
        
        // ✅ STEP 0: Clean up stale parent relationships for all business nodes
        _logger.LogInformation("🧹 [SetupBusinessAgentTopology] Step 0: Cleaning up stale parent relationships...");
        await CleanupStaleParentRelationshipsAsync(topology, viewConfigDto);
        _logger.LogInformation("🧹 [SetupBusinessAgentTopology] Step 0: ✅ Cleanup completed");
        
        // Update StartAgent → Top-level business agents + Coordinator
        var expectedStartChildren = topology.TopLevelNodes
            .Select(n => GrainId.Create(n.AgentType, GuidUtil.GuidToGrainKey(n.AgentId)))
            .ToList();
        expectedStartChildren.Add(coordinatorGrainId);
        await UpdateAgentChildrenAsync(agents.StartAgent, expectedStartChildren, "StartAgent", "top-level agents + Coordinator");
        
        // Update business agent topology (business agent → next agents + Coordinator)
        // Group by source node to handle parallel connections
        var groupedConnections = viewConfigDto.WorkflowNodeUnitList
            .GroupBy(u => u.NodeId)
            .ToList();
        
        _logger.LogInformation("📊 [SetupBusinessAgentTopology] Processing {Count} source nodes with connections", groupedConnections.Count);
        _logger.LogInformation("📊 [SetupBusinessAgentTopology] Total units in config: {TotalUnits}", viewConfigDto.WorkflowNodeUnitList.Count);
        
        foreach (var group in groupedConnections)
        {
            var sourceNodeId = group.Key;
            var sourceNode = topology.NodeMap[sourceNodeId];
            var sourceGrainId = GrainId.Create(sourceNode.AgentType, GuidUtil.GuidToGrainKey(sourceNode.AgentId));
            var sourceAgent = await _gAgentFactory.GetGAgentAsync(sourceGrainId);
            
            _logger.LogInformation("🔗 [SetupBusinessAgentTopology] Processing source: {SourceNodeName} (AgentId: {SourceAgentId}, GrainId: {SourceGrainId})", 
                sourceNode.Name, sourceNode.AgentId, sourceGrainId);
            _logger.LogInformation("🔗 [SetupBusinessAgentTopology]   → Has {TargetCount} target nodes", group.Count());
            
            // Collect all target agents for this source node (handles parallel connections)
            var targetGrainIds = new List<GrainId>();
            foreach (var unit in group)
            {
                var targetNode = topology.NodeMap[unit.NextNodeId];
                var targetGrainId = GrainId.Create(targetNode.AgentType, GuidUtil.GuidToGrainKey(targetNode.AgentId));
                targetGrainIds.Add(targetGrainId);
                _logger.LogInformation("🔗 [SetupBusinessAgentTopology]     → Target: {TargetNodeName} (AgentId: {TargetAgentId}, GrainId: {TargetGrainId})", 
                    targetNode.Name, targetNode.AgentId, targetGrainId);
            }
            
            // Add Coordinator to the children list
            targetGrainIds.Add(coordinatorGrainId);
            _logger.LogInformation("🔗 [SetupBusinessAgentTopology]     → Added Coordinator (GrainId: {CoordinatorGrainId})", coordinatorGrainId);
            
            _logger.LogInformation("🔗 [SetupBusinessAgentTopology]   → Total children to set: {ChildCount}", targetGrainIds.Count);
            
            var targetNames = string.Join(", ", group.Select(u => topology.NodeMap[u.NextNodeId].Name));
            await UpdateAgentChildrenAsync(sourceAgent, targetGrainIds, 
                $"BusinessAgent({sourceNode.Name})", $"{targetNames} + Coordinator");
            
            _logger.LogInformation("🔗 [SetupBusinessAgentTopology]   ✅ Children set for {SourceNodeName}", sourceNode.Name);
        }
        
        // Update Leaf agents → EndAgent + Coordinator
        foreach (var leafNode in topology.LeafNodes)
        {
            var leafGrainId = GrainId.Create(leafNode.AgentType, GuidUtil.GuidToGrainKey(leafNode.AgentId));
            var leafAgent = await _gAgentFactory.GetGAgentAsync(leafGrainId);
            await UpdateAgentChildrenAsync(leafAgent, new List<GrainId> { agents.EndAgent.GetGrainId(), coordinatorGrainId }, 
                $"LeafAgent({leafNode.Name})", "EndAgent + Coordinator");
            
            // ✅ Verify children were correctly set
            await Task.Delay(50); // Allow time for event sourcing to persist
            var verifyChildren = await leafAgent.GetChildrenAsync();
            _logger.LogInformation("✅ [SetupBusinessAgentTopology] Verified {LeafName} children: {ChildCount}", 
                leafNode.Name, verifyChildren.Count);
            foreach (var child in verifyChildren)
            {
                _logger.LogInformation("✅ [SetupBusinessAgentTopology]   - Child: {ChildId}", child);
            }
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
        _logger.LogInformation("👥 [UpdateAgentChildren] START - Parent: {ParentName} → Children: {ChildrenDesc}", parentName, childrenDesc);
        
        var currentChildren = await parentAgent.GetChildrenAsync();
        var currentChildIdSet = currentChildren.ToHashSet();
        var expectedChildIdSet = expectedChildGrainIds.ToHashSet();
        
        _logger.LogInformation("👥 [UpdateAgentChildren] Current children count: {CurrentCount}", currentChildren.Count);
        foreach (var currentChild in currentChildren)
        {
            _logger.LogInformation("👥 [UpdateAgentChildren]   - Current: {ChildGrainId}", currentChild);
        }
        
        _logger.LogInformation("👥 [UpdateAgentChildren] Expected children count: {ExpectedCount}", expectedChildGrainIds.Count);
        foreach (var expectedChild in expectedChildGrainIds)
        {
            _logger.LogInformation("👥 [UpdateAgentChildren]   + Expected: {ChildGrainId}", expectedChild);
        }
        
        // Find children to add (in expected but not in current)
        var toAdd = expectedChildGrainIds.Where(id => !currentChildIdSet.Contains(id)).ToList();
        
        // Find children to remove (in current but not in expected)
        var toRemove = currentChildren.Where(id => !expectedChildIdSet.Contains(id)).ToList();
        
        _logger.LogInformation("👥 [UpdateAgentChildren] Changes - ToAdd: {AddCount}, ToRemove: {RemoveCount}", toAdd.Count, toRemove.Count);
        
        // Skip if no changes
        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            _logger.LogInformation("👥 [UpdateAgentChildren] ✅ No changes needed for {ParentName}", parentName);
            return;
        }
        
        // Remove outdated children
        foreach (var childGrainIdToRemove in toRemove)
        {
            _logger.LogInformation("👥 [UpdateAgentChildren]   🗑️ Removing child: {ChildGrainId}", childGrainIdToRemove);
            var childAgent = await _gAgentFactory.GetGAgentAsync(childGrainIdToRemove);
            await parentAgent.UnregisterAsync(childAgent);
            _logger.LogInformation("👥 [UpdateAgentChildren]   ✅ Removed child: {ChildGrainId}", childGrainIdToRemove);
        }
        
        // Add new children
        foreach (var childGrainIdToAdd in toAdd)
        {
            _logger.LogInformation("👥 [UpdateAgentChildren]   ➕ Adding child: {ChildGrainId}", childGrainIdToAdd);
            var childAgent = await _gAgentFactory.GetGAgentAsync(childGrainIdToAdd);
            await parentAgent.RegisterAsync(childAgent);
            _logger.LogInformation("👥 [UpdateAgentChildren]   ✅ Added child: {ChildGrainId}", childGrainIdToAdd);
        }
        
        _logger.LogInformation("👥 [UpdateAgentChildren] ✅ COMPLETED - Parent: {ParentName}, Added: {AddCount}, Removed: {RemoveCount}", 
            parentName, toAdd.Count, toRemove.Count);
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