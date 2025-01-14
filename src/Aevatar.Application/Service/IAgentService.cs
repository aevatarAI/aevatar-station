using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AtomicAgent;
using Aevatar.CombinationAgent;
using Aevatar.CQRS.Dto;

namespace Aevatar.Service;

public interface IAgentService
{
    Task<AtomicAgentDto> GetAtomicAgentAsync(string id);
    Task<AtomicAgentDto> CreateAtomicAgentAsync(CreateAtomicAgentDto createDto);
    Task<AtomicAgentDto> UpdateAtomicAgentAsync(string id, UpdateAtomicAgentDto updateDto);
    Task<List<AtomicAgentDto>> GetAtomicAgentsAsync(string userAddress, int pageIndex, int pageSize);

    Task DeleteAtomicAgentAsync(string id);
    Task<CombinationAgentDto> GetCombinationAsync(string id);
    Task<CombinationAgentDto> CombineAgentAsync(CombineAgentDto combineAgentDto);
    Task<CombinationAgentDto> UpdateCombinationAsync(string id, UpdateCombinationDto updateCombinationDto);
    Task DeleteCombinationAsync(string id);
    Task<List<CombinationAgentDto>> GetCombinationAgentsAsync(string userAddress, string groupId, int pageIndex, int pageSize);
    Task<Tuple<long, List<AgentGEventIndex>>> GetAgentEventLogsAsync(string agentId, int pageIndex, int pageSize);

    Task RunAgentAsync(string agentId);
    Task<List<AgentParamDto>> GetAllAgents();
}