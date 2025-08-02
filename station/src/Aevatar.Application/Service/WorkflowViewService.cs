using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Common;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.GAgent.Coordinator.WorkflowView.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.ObjectMapping;

namespace Aevatar.Service;

[RemoteService(IsEnabled = false)]
public class WorkflowViewService : ApplicationService, IWorkflowViewService
{
    private readonly IAgentService _agentService;
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<WorkflowViewService> _logger;

    public WorkflowViewService(IAgentService agentService, IGAgentFactory gAgentFactory,  ILogger<WorkflowViewService> logger)
    {
        _agentService = agentService;
        _gAgentFactory = gAgentFactory;
        _logger = logger;
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
}

public interface IWorkflowViewService
{
    Task<AgentDto> PublishWorkflowAsync(Guid viewAgentId);
}