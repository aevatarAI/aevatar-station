using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.CQRS.Dto;

namespace Aevatar.Service;

public interface IAgentService
{
    Task<Tuple<long, List<AgentGEventIndex>>> GetAgentEventLogsAsync(string agentId, int pageIndex, int pageSize);
    
    Task<List<AgentTypeDto>> GetAllAgents();
    
    Task<AgentDto> CreateAgentAsync(CreateAgentInputDto dto);
    Task<List<AgentInstanceDto>> GetAllAgentInstances(int pageIndex, int pageSize);
    Task<AgentDto> GetAgentAsync(Guid guid);
    Task<AgentDto> UpdateAgentAsync(Guid guid, UpdateAgentInputDto dto);
    Task<SubAgentDto> AddSubAgentAsync(Guid guid, AddSubAgentDto addSubAgentDto);
    Task<SubAgentDto> RemoveSubAgentAsync(Guid guid, RemoveSubAgentDto removeSubAgentDto);
    Task RemoveAllSubAgentAsync(Guid guid);
    Task<AgentRelationshipDto> GetAgentRelationshipAsync(Guid guid);
    Task DeleteAgentAsync(Guid guid);
}