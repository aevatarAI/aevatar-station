using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.AgentValidation;
using Aevatar.Controllers;
using Aevatar.CQRS.Dto;
using Aevatar.Permissions;
using Aevatar.Service;
using Aevatar.Subscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

[Route("api/agent")]
public class AgentController : AevatarController
{
    private readonly ILogger<AgentController> _logger;
    private readonly IAgentService _agentService;
    private readonly ISubscriptionAppService _subscriptionAppService;
    private readonly IAgentValidationService _agentValidationService;

    public AgentController(
        ILogger<AgentController> logger,
        ISubscriptionAppService subscriptionAppService,
        IAgentService agentService,
        IAgentValidationService agentValidationService)
    {
        _logger = logger;
        _agentService = agentService;
        _subscriptionAppService = subscriptionAppService;
        _agentValidationService = agentValidationService;
    }


    [HttpGet("agent-type-info-list")]
    // [Authorize(Policy = AevatarPermissions.Agent.ViewAllType)]
    [Authorize]
    public async Task<List<AgentTypeDto>> GetAllAgent()
    {
        return await _agentService.GetAllAgents();
    }

    [HttpGet("agent-list")]
    [Authorize]
    public async Task<List<AgentInstanceDto>> GetAllAgentInstance([FromQuery] GetAllAgentInstancesQueryDto queryDto)
    {
        return await _agentService.GetAllAgentInstances(queryDto);
    }

    [HttpPost]
    // [Authorize(Policy = AevatarPermissions.Agent.Create)]
    [Authorize]
    public async Task<AgentDto> CreateAgent([FromBody] CreateAgentInputDto createAgentInputDto)
    {
        _logger.LogInformation("Create Agent: {agent}", JsonConvert.SerializeObject(createAgentInputDto));
        var agentDto = await _agentService.CreateAgentAsync(createAgentInputDto);
        return agentDto;
    }

    [HttpGet("{guid}")]
    // [Authorize(Policy = AevatarPermissions.Agent.View)]
    [Authorize]
    public async Task<AgentDto> GetAgent(Guid guid)
    {
        _logger.LogInformation("Get Agent: {guid}", guid);
        var agentDto = await _agentService.GetAgentAsync(guid);
        return agentDto;
    }

    [HttpGet("{guid}/relationship")]
    // [Authorize(Policy = AevatarPermissions.Relationship.ViewRelationship)]
    [Authorize]
    public async Task<AgentRelationshipDto> GetAgentRelationship(Guid guid)
    {
        _logger.LogInformation("Get Agent Relationship");
        var agentRelationshipDto = await _agentService.GetAgentRelationshipAsync(guid);
        return agentRelationshipDto;
    }

    [HttpPost("{guid}/add-subagent")]
    // [Authorize(Policy = AevatarPermissions.Relationship.AddSubAgent)]
    [Authorize]
    public async Task<SubAgentDto> AddSubAgent(Guid guid, [FromBody] AddSubAgentDto addSubAgentDto)
    {
        _logger.LogInformation("Add sub Agent: {agent}", JsonConvert.SerializeObject(addSubAgentDto));
        var subAgentDto = await _agentService.AddSubAgentAsync(guid, addSubAgentDto);
        return subAgentDto;
    }

    [HttpPost("{guid}/remove-subagent")]
    // [Authorize(Policy = AevatarPermissions.Relationship.RemoveSubAgent)]
    [Authorize]
    public async Task<SubAgentDto> RemoveSubAgent(Guid guid, [FromBody] RemoveSubAgentDto removeSubAgentDto)
    {
        _logger.LogInformation("remove sub Agent: {agent}", JsonConvert.SerializeObject(removeSubAgentDto));
        var subAgentDto = await _agentService.RemoveSubAgentAsync(guid, removeSubAgentDto);
        return subAgentDto;
    }

    [HttpPost("{guid}/remove-all-subagent")]
    // [Authorize(Policy = AevatarPermissions.Relationship.RemoveAllSubAgents)]
    [Authorize]
    public async Task RemoveAllSubAgent(Guid guid)
    {
        _logger.LogInformation("remove sub Agent: {guid}", guid);
        await _agentService.RemoveAllSubAgentAsync(guid);
    }

    [HttpPut("{guid}")]
    // [Authorize(Policy = AevatarPermissions.Agent.Update)]
    [Authorize]
    public async Task<AgentDto> UpdateAgent(Guid guid, [FromBody] UpdateAgentInputDto updateAgentInputDto)
    {
        _logger.LogInformation("Update Agent--1: {agent}", JsonConvert.SerializeObject(updateAgentInputDto));
        var agentDto = await _agentService.UpdateAgentAsync(guid, updateAgentInputDto);
        return agentDto;
    }

    [HttpDelete("{guid}")]
    // [Authorize(Policy = AevatarPermissions.Agent.Delete)]
    [Authorize]
    public async Task DeleteAgent(Guid guid)
    {
        _logger.LogInformation("Delete Agent: {guid}", guid);
        await _agentService.DeleteAgentAsync(guid);
    }

    [HttpPost("publishEvent")]
    // [Authorize(Policy = AevatarPermissions.EventManagement.Publish)]
    [Authorize]
    public async Task PublishAsync([FromBody] PublishEventDto input)
    {
        await _subscriptionAppService.PublishEventAsync(input);
    }

    /// <summary>
    /// Validate agent configuration
    /// </summary>
    /// <param name="request">Validation request containing GAgent namespace and configuration JSON</param>
    /// <returns>Validation result with success status and error details</returns>
    [HttpPost("validation/validate-config")]
    [Authorize]
    public async Task<ConfigValidationResultDto> ValidateConfigAsync([FromBody] ValidationRequestDto request)
    {
        return await _agentValidationService.ValidateConfigAsync(request);
    }

    /// <summary>
    /// Health check endpoint for the validation service
    /// </summary>
    /// <returns>Service health status</returns>
    [HttpGet("validation/health")]
    public IActionResult ValidationHealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}