using System.Threading.Tasks;
using Aevatar.AtomicAgent;
using Aevatar.CombinationAgent;
using Aevatar.Service;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    
    public AgentController(
        ILogger<AgentController> logger, 
        IAgentService agentService)
    {
        _logger = logger;
        _agentService = agentService;
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
}