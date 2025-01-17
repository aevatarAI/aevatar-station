using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Agents.Atomic.Models;
using Aevatar.AtomicAgent;
using Aevatar.CombinationAgent;
using Aevatar.CQRS.Dto;
using Aevatar.CQRS.Provider;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Agent")]
[Route("api/agent")]
public class AgentController : AevatarController
{
    private readonly ILogger<AgentController> _logger;
    private readonly IAgentService  _agentService;
    private readonly ICQRSProvider _cqrsProvider;

    public AgentController(
        ILogger<AgentController> logger, 
        ICQRSProvider cqrsProvider, 

        IAgentService agentService)
    {
        _logger = logger;
        _agentService = agentService;
        _cqrsProvider = cqrsProvider;

    }
    
    [HttpPost("/atomic-agent")]
    public async Task<AtomicAgentDto> CreateAgent([FromBody] CreateAtomicAgentDto createAtomicAgentDto)
    {
        _logger.LogInformation("Create Atomic-Agent: {agent}", JsonConvert.SerializeObject(createAtomicAgentDto));
        var agentDto = await _agentService.CreateAtomicAgentAsync(createAtomicAgentDto);
        return agentDto;
    }
    
    [HttpGet("/atomic-agent/{id}")]
    public async Task<AtomicAgentDto> GetAgent(string id)
    {
        _logger.LogInformation("Get Atomic-Agent: {agent}", id);
        var agentDto = await _agentService.GetAtomicAgentAsync(id);
        return agentDto;
    }
    
    [HttpPut("/atomic-agent/{id}")]
    public async Task<AtomicAgentDto> UpdateAgent(string id, [FromBody] UpdateAtomicAgentDto updateAtomicAgentDto)
    {
        _logger.LogInformation("Update Atomic-Agent: {agent}", JsonConvert.SerializeObject(updateAtomicAgentDto));
        var agentDto = await _agentService.UpdateAtomicAgentAsync(id, updateAtomicAgentDto);
        return agentDto;
    }
   
    [HttpDelete("/atomic-agent/{id}")]
    public async Task DeleteAgent(string id)
    {
        _logger.LogInformation("Delete Atomic-Agent: {agent}", id);
        await _agentService.DeleteAtomicAgentAsync(id);
    }
    
    [HttpPost("/combination-agent")]
    public async Task<CombinationAgentDto> CombineAgent([FromBody] CombineAgentDto combineAgentDto)
    {
        _logger.LogInformation("Combine Atomic-Agent: {agent}", JsonConvert.SerializeObject(combineAgentDto));
        var agentDto = await _agentService.CombineAgentAsync(combineAgentDto);
        return agentDto;
    }
    
    [HttpPut("/combination-agent/{id}")]
    public async Task<CombinationAgentDto> UpdateCombination(string id, [FromBody] UpdateCombinationDto updateCombinationDto)
    {
        _logger.LogInformation("Update Combination: {agent}", JsonConvert.SerializeObject(updateCombinationDto));
        var agentDto = await _agentService.UpdateCombinationAsync(id, updateCombinationDto);
        return agentDto;
    }
    
    [HttpDelete("/combination-agent/{id}")]
    public async Task DeleteCombination(string id)
    {
        _logger.LogInformation("Delete Combination: {agent}", id);
        await _agentService.DeleteCombinationAsync(id);
    }
    
    [HttpGet("/combination-agent/{id}")]
    public async Task<CombinationAgentDto> GetCombination(string id)
    {
        _logger.LogInformation("Get Combination: {agent}", id);
        var agentDto = await _agentService.GetCombinationAsync(id);
        return agentDto;
    }
    [HttpGet("/atomic-agents")]
    public async Task<List<AtomicAgentDto>> GetAtomicAgentList(string userAddress, int pageIndex, int pageSize)
    {
        _logger.LogInformation("Get Atomic-Agent list: {address}", userAddress);
        var agentDto = await _agentService.GetAtomicAgentsAsync(userAddress, pageIndex, pageSize);
        return agentDto;
    }
    
    [HttpGet("/combination-agents")]
    public async Task<List<CombinationAgentDto>> GetCombinationAgentList(string userAddress, string? groupId, int pageIndex, int pageSize)
    {
        _logger.LogInformation("Get Combination-Agent list: {address} {groupId} {pageIndex} {pageSize}", userAddress,groupId, pageIndex,pageSize);
        var agentDtoList = await _agentService.GetCombinationAgentsAsync(userAddress, groupId, pageIndex, pageSize);
        return agentDtoList;
    }
    
    [HttpGet("/agent-logs")]
    public async Task<Tuple<long, List<AgentGEventIndex>>> GetAgentLogs(string agentId, int pageIndex, int pageSize)
    {
        _logger.LogInformation("Get Agent logs : {agentId} {pageIndex} {pageSize}",agentId, pageIndex,pageSize);
        var agentDtoList = await _agentService.GetAgentEventLogsAsync(agentId, pageIndex, pageSize);
        return agentDtoList;
    }
    
    [HttpPut("/chat-logs")]
    public async Task<AIChatLogIndex> AddChatLogs(string? groupId)
    {
        _logger.LogInformation("AddChatLogs");
        if (groupId.IsNullOrEmpty())
        {
            groupId = Guid.NewGuid().ToString();
        }

        var chatIndex = new AIChatLogIndex
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            AgentName = "JudgeAgent",
            RoleType = "Judge",
            AgentResponsibility ="",
            Request = "give a name",
            Response = "GPT"
        };

        await _cqrsProvider.SendSaveDataCommandAsync(chatIndex, chatIndex.Id);
        return chatIndex;
    }
    [HttpGet("/chat-logs")]
    public async Task<List<AIChatLogIndexDto>> GetChatLogs(string? agentId, string? groupId, int pageIndex, int pageSize)
    {
        _logger.LogInformation("GetChatLogs");
        var index = "aevatar" + nameof(AIChatLogIndex).ToLower();
        var filters = new List<Func<QueryContainerDescriptor<object>, QueryContainer>>();
        if (!agentId.IsNullOrEmpty())
        {
            filters.Add(m => m.Term(t => t.Field("id").Value(agentId)) );
        }
        
        if (!groupId.IsNullOrEmpty())
        {
            filters.Add(m => m.Term(t => t.Field("groupId").Value(groupId)));
        }

        var result = await _cqrsProvider.QueryDataListAsync(index,
            q => q.Bool(b => b.Must(filters)),
            (pageIndex-1)*pageSize,
            pageSize
        );
        if (result == null)
        {
            return null;
        }
        
        var chatLogs = JsonConvert.DeserializeObject<List<AIChatLogIndexDto>>(result);
        return chatLogs;
    }
}