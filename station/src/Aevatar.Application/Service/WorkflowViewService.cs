using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
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
public class WorkflowViewService : ApplicationService, IWorkflowViewService
{
    private readonly IAgentService _agentService;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<WorkflowViewService> _logger;
    private readonly DebugModeOptions _debugModeOptions;

    public WorkflowViewService(IAgentService agentService, IGAgentFactory gAgentFactory,  ILogger<WorkflowViewService> logger,
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
        workflowConfig.EnableExecutionRecord = _debugModeOptions.ExecuteRecordMode;
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
                    NextGrainId = ""
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
                    NextGrainId = GrainId.Create(nextNode.AgentType, GuidUtil.GuidToGrainKey(nextNode.AgentId)).ToString()
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
            var emptyWorkflowCoordinatorGAgent = await _gAgentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.Empty);
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

            await _agentService.AddSubAgentAsync(workflowCoordinatorGAgentDto.AgentGuid, new AddSubAgentDto());
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

        // update workflowViewAgent
        configJson = JsonConvert.SerializeObject(viewConfigDto);
        var viewConfigProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(configJson);
        viewConfigProperties.Remove("PublisherGrainId");
        viewConfigProperties.Remove("CorrelationId");
        agentDto = await _agentService.UpdateAgentAsync(viewAgentId, new UpdateAgentInputDto()
        {
            Properties = viewConfigProperties,
            Name = agentDto.Name
        });
        return agentDto;
    }

    public async Task<AgentDto> CreateDefaultWorkflowAsync()
    {
        var emptyWorkflowViewGAgent = await _gAgentFactory.GetGAgentAsync<IWorkflowViewGAgent>(Guid.Empty);
        string workflowAgentType;
        try
        {
            workflowAgentType = emptyWorkflowViewGAgent.GetGrainId().Type.ToString();
        }
        catch (Exception)
        {
            _logger.LogWarning("Failed to get GrainId.Type for WorkflowViewGAgent; falling back to type name.");
            workflowAgentType = typeof(WorkflowViewGAgent).FullName ?? typeof(WorkflowViewGAgent).Name;
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

        var configProperties = "{\"workflowNodeList\":[{\"agentType\":\"Aevatar.GAgents.InputGAgent.GAgent.InputGAgent\",\"name\":\"MyInputGAgent\",\"extendedData\":{\"xPosition\":\"2\",\"yPosition\":\"16\"},\"jsonProperties\":\"{\\\"memberName\\\":\\\"inputGAgent1\\\",\\\"input\\\":\\\"I want to eat, get me a choose.\\\"}\",\"nodeId\":\"45dc7d32-1002-4479-8616-b12cbc112bb4\"},{\"agentType\":\"Aevatar.GAgents.Twitter.GAgents.ChatAIAgent.ChatAIGAgent\",\"name\":\"ai\",\"extendedData\":{\"xPosition\":\"365.1172008973645\",\"yPosition\":\"-12.62092346330003\"},\"jsonProperties\":\"{\\\"memberName\\\":\\\"ai\\\",\\\"instructions\\\":\\\"You are a helpful AI assistant\\\",\\\"systemLLM\\\":\\\"OpenAI\\\",\\\"mcpServers\\\":[],\\\"toolGAgentTypes\\\":[],\\\"toolGAgents\\\":[]}\",\"nodeId\":\"6c15ac63-ce9b-4ef2-a982-171e4ed94bdb\"}],\"workflowNodeUnitList\":[{\"nodeId\":\"45dc7d32-1002-4479-8616-b12cbc112bb4\",\"nextNodeId\":\"6c15ac63-ce9b-4ef2-a982-171e4ed94bdb\"}],\"name\":\"default workflow\"}";
        var properties = JsonConvert.DeserializeObject<Dictionary<string, object>?>(configProperties);
        var agentDto = await _agentService.CreateAgentAsync(new CreateAgentInputDto()
        {
            Name = properties!["name"].ToString()!,
            AgentType = workflowAgentType!,
            Properties = properties
        });
        return agentDto;
    }
}

public interface IWorkflowViewService
{
    Task<AgentDto> PublishWorkflowAsync(Guid viewAgentId);
    Task<AgentDto> CreateDefaultWorkflowAsync();
}