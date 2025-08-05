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
using Aevatar.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        var viewConfigDto = JsonConvert.DeserializeObject<WorkflowViewConfigDto>(configJson);
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
                var subAgentDto = await _agentService.CreateAgentAsync(new CreateAgentInputDto()
                {
                    AgentId = workflowNode.AgentId == Guid.Empty ? null : workflowNode.AgentId,
                    Name = workflowNode.Name,
                    Properties = nodeAgentProperties,
                    AgentType = workflowNode.AgentType
                });
                workflowNode.AgentId = subAgentDto.AgentGuid;
            }
            else
            {
                await _agentService.UpdateAgentAsync(workflowNode.AgentId, new UpdateAgentInputDto()
                {
                    Name = workflowNode.Name,
                    Properties = nodeAgentProperties
                });
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

        var workflowCoordinatorGAgentId = viewConfigDto.WorkflowCoordinatorGAgentId;
        if (workflowCoordinatorGAgentId == Guid.Empty)
        {
            var emptyWorkflowCoordinatorGAgent = await _gAgentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(Guid.Empty);
            var workflowCoordinatorGAgentDto = await _agentService.CreateAgentAsync(new CreateAgentInputDto()
            {
                Name = agentDto.Name,
                AgentType = emptyWorkflowCoordinatorGAgent.GetGrainId().Type.ToString(),
                Properties = workflowConfigProperties
            });
            viewConfigDto.WorkflowCoordinatorGAgentId = workflowCoordinatorGAgentDto.AgentGuid;

            await _agentService.AddSubAgentAsync(workflowCoordinatorGAgentDto.AgentGuid, new AddSubAgentDto());
        }
        else
        {
            await _agentService.UpdateAgentAsync(workflowCoordinatorGAgentId, new UpdateAgentInputDto()
            {
                Name = agentDto.Name,
                Properties = workflowConfigProperties
            });
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