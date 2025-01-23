using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.AtomicAgent;
using Aevatar.CQRS.Dto;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Agent")]
[Route("api/agent")]
// [Authorize]
public class AgentController : AevatarController
{
    private readonly ILogger<AgentController> _logger;
    private readonly IAgentService  _agentService;
    
    public AgentController(
        ILogger<AgentController> logger, 
        IAgentService agentService)
    {
        _logger = logger;
        _agentService = agentService;
    }
    
    // [HttpGet("/atomic-agents")]
    // public async Task<List<AtomicAgentDto>> GetAtomicAgentList(string userAddress, int pageIndex, int pageSize)
    // {
    //     _logger.LogInformation("Get Atomic-Agent list: {address}", userAddress);
    //     var agentDto = await _agentService.GetAtomicAgentsAsync(userAddress, pageIndex, pageSize);
    //     return agentDto;
    // }
    //
    // [HttpGet("/combination-agents")]
    // public async Task<List<CombinationAgentDto>> GetCombinationAgentList(string userAddress, string? groupId, int pageIndex, int pageSize)
    // {
    //     _logger.LogInformation("Get Combination-Agent list: {address} {groupId} {pageIndex} {pageSize}", userAddress,groupId, pageIndex,pageSize);
    //     var agentDtoList = await _agentService.GetCombinationAgentsAsync(userAddress, groupId, pageIndex, pageSize);
    //     return agentDtoList;
    // }
    
    [HttpGet("/agent-logs")]
    public async Task<Tuple<long, List<AgentGEventIndex>>> GetAgentLogs(string agentId, int pageIndex, int pageSize)
    {
        _logger.LogInformation("Get Agent logs : {agentId} {pageIndex} {pageSize}",agentId, pageIndex,pageSize);
        var agentDtoList = await _agentService.GetAgentEventLogsAsync(agentId, pageIndex, pageSize);
        return agentDtoList;
    }
    
    [HttpGet("/all-agents")]
    public async Task<List<AgentTypeDto>> GetAllAgent()
    {
        return await _agentService.GetAllAgents();
    }
    
    [HttpPost("/agent")]
    public async Task<AgentDto> CreateAgent([FromBody] CreateAgentInputDto createAgentInputDto)
    {
        _logger.LogInformation("Create Agent: {agent}", JsonConvert.SerializeObject(createAgentInputDto));
        var agentDto = await _agentService.CreateAgentAsync(createAgentInputDto);
        return agentDto;
    }
    
    [HttpGet]
    public async Task<AgentDto> GetAgent(Guid guid)
    {
        _logger.LogInformation("Create Agent: {guid}", guid);
        var agentDto = await _agentService.GetAgentAsync(guid);
        return agentDto;
    }
    
    [HttpGet("{guid}/relationship")]
    public async Task<AgentRelationshipDto> GetAgentRelationship(Guid guid)
    {
        _logger.LogInformation("Get Agent Relationship");
        var agentRelationshipDto = await _agentService.GetAgentRelationshipAsync(guid);
        return agentRelationshipDto;
    }
    
    [HttpPost("{guid}/add-subagent")]
    public async Task<SubAgentDto> AddSubAgent(Guid guid, [FromBody] AddSubAgentDto addSubAgentDto)
    {
        _logger.LogInformation("Add sub Agent: {agent}", JsonConvert.SerializeObject(addSubAgentDto));
        var subAgentDto = await _agentService.AddSubAgentAsync(guid, addSubAgentDto);
        return subAgentDto;
    }
    
    [HttpPost("{guid}/remove-subagent")]
    public async Task<SubAgentDto> RemoveSubAgent(Guid guid, [FromBody] RemoveSubAgentDto removeSubAgentDto)
    {
        _logger.LogInformation("remove sub Agent: {agent}", JsonConvert.SerializeObject(removeSubAgentDto));
        var subAgentDto = await _agentService.RemoveSubAgentAsync(guid, removeSubAgentDto);
        return subAgentDto;
    }
    
    [HttpPost("{guid}/remove-all-subagent")]
    public async Task RemoveAllSubAgent(Guid guid)
    {
        _logger.LogInformation("remove sub Agent: {guid}", guid);
        await _agentService.RemoveAllSubAgentAsync(guid);
    }
    
    [HttpPut("{guid}")]
    public async Task<AgentDto> UpdateAgent(Guid guid, [FromBody] UpdateAgentInputDto updateAgentInputDto)
    {
        _logger.LogInformation("Update Agent: {agent}", JsonConvert.SerializeObject(updateAgentInputDto));
        var agentDto = await _agentService.UpdateAgentAsync(guid, updateAgentInputDto);
        return agentDto;
    }
   
    [HttpDelete("{guid}")]
    public async Task DeleteAgent(Guid guid)
    {
        _logger.LogInformation("Delete Agent: {agent}", guid);
        await _agentService.DeleteAgentAsync(guid);
    }
}