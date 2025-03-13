using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Controllers;
using Aevatar.CQRS.Dto;
using Aevatar.Permissions;
using Aevatar.Service;
using Aevatar.Subscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.PermissionManagement;

[Route("api/agent")]
public class AgentController : AevatarController
{
    private readonly ILogger<AgentController> _logger;
    private readonly IAgentService _agentService;
    private readonly SubscriptionAppService _subscriptionAppService;
    private readonly IPermissionManager _permissionManager;

    public AgentController(
        ILogger<AgentController> logger,
        SubscriptionAppService subscriptionAppService,
        IAgentService agentService,
        IPermissionManager permissionManager)
    {
        _logger = logger;
        _agentService = agentService;
        _subscriptionAppService = subscriptionAppService;
        _permissionManager = permissionManager;
    }

    [HttpGet("agent-logs")]
    [Authorize(Policy = AevatarPermissions.Agent.ViewLogs)] 
    public async Task<Tuple<long, List<AgentGEventIndex>>> GetAgentLogs(string agentId, int pageIndex, int pageSize)
    {
        _logger.LogInformation("Get Agent logs : {agentId} {pageIndex} {pageSize}", agentId, pageIndex, pageSize);
        var agentDtoList = await _agentService.GetAgentEventLogsAsync(agentId, pageIndex, pageSize);
        return agentDtoList;
    }

    [HttpGet("agent-type-info-list")]
    [Authorize]
    public async Task<List<AgentTypeDto>> GetAllAgent()
    {
        var permission = await _permissionManager.GetForClientAsync("Test","AgentManagement.ViewAllType");
        _logger.LogInformation("permission " + permission.IsGranted);
        return await _agentService.GetAllAgents();
    }

    [HttpGet("agent-list")]
    [Authorize(Policy = AevatarPermissions.Agent.ViewList)]
    public async Task<List<AgentInstanceDto>> GetAllAgentInstance(int pageIndex = 0, int pageSize = 20)
    {
        return await _agentService.GetAllAgentInstances(pageIndex, pageSize);
    }

    [HttpPost]
    [Authorize(Policy = AevatarPermissions.Agent.Create)]
    public async Task<AgentDto> CreateAgent([FromBody] CreateAgentInputDto createAgentInputDto)
    {
        _logger.LogInformation("Create Agent: {agent}", JsonConvert.SerializeObject(createAgentInputDto));
        var agentDto = await _agentService.CreateAgentAsync(createAgentInputDto);
        return agentDto;
    }

    [HttpGet("{guid}")]
    [Authorize(Policy = AevatarPermissions.Agent.View)]
    public async Task<AgentDto> GetAgent(Guid guid)
    {
        _logger.LogInformation("Get Agent: {guid}", guid);
        var agentDto = await _agentService.GetAgentAsync(guid);
        return agentDto;
    }

    [HttpGet("{guid}/relationship")]
    [Authorize(Policy = AevatarPermissions.Relationship.ViewRelationship)]
    public async Task<AgentRelationshipDto> GetAgentRelationship(Guid guid)
    {
        _logger.LogInformation("Get Agent Relationship");
        var agentRelationshipDto = await _agentService.GetAgentRelationshipAsync(guid);
        return agentRelationshipDto;
    }

    [HttpPost("{guid}/add-subagent")]
    [Authorize(Policy = AevatarPermissions.Relationship.AddSubAgent)]
    public async Task<SubAgentDto> AddSubAgent(Guid guid, [FromBody] AddSubAgentDto addSubAgentDto)
    {
        _logger.LogInformation("Add sub Agent: {agent}", JsonConvert.SerializeObject(addSubAgentDto));
        var subAgentDto = await _agentService.AddSubAgentAsync(guid, addSubAgentDto);
        return subAgentDto;
    }

    [HttpPost("{guid}/remove-subagent")]
    [Authorize(Policy = AevatarPermissions.Relationship.RemoveSubAgent)]
    public async Task<SubAgentDto> RemoveSubAgent(Guid guid, [FromBody] RemoveSubAgentDto removeSubAgentDto)
    {
        _logger.LogInformation("remove sub Agent: {agent}", JsonConvert.SerializeObject(removeSubAgentDto));
        var subAgentDto = await _agentService.RemoveSubAgentAsync(guid, removeSubAgentDto);
        return subAgentDto;
    }

    [HttpPost("{guid}/remove-all-subagent")]
    [Authorize(Policy = AevatarPermissions.Relationship.RemoveAllSubAgents)]
    public async Task RemoveAllSubAgent(Guid guid)
    {
        _logger.LogInformation("remove sub Agent: {guid}", guid);
        await _agentService.RemoveAllSubAgentAsync(guid);
    }

    [HttpPut("{guid}")]
    [Authorize(Policy = AevatarPermissions.Agent.Update)]
    public async Task<AgentDto> UpdateAgent(Guid guid, [FromBody] UpdateAgentInputDto updateAgentInputDto)
    {
        _logger.LogInformation("Update Agent--1: {agent}", JsonConvert.SerializeObject(updateAgentInputDto));
        var agentDto = await _agentService.UpdateAgentAsync(guid, updateAgentInputDto);
        return agentDto;
    }

    [HttpDelete("{guid}")]
    [Authorize(Policy = AevatarPermissions.Agent.Delete)]
    public async Task DeleteAgent(Guid guid)
    {
        _logger.LogInformation("Delete Agent: {guid}", guid);
        await _agentService.DeleteAgentAsync(guid);
    }

    [HttpPost("publishEvent")]
    [Authorize(Policy = AevatarPermissions.EventManagement.Publish)]
    public async Task PublishAsync([FromBody] PublishEventDto input)
    {
        await _subscriptionAppService.PublishEventAsync(input);
    }
}