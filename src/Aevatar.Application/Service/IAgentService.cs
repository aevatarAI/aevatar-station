using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Agent;

namespace Aevatar.Service;

public interface IAgentService
{
    Task<List<AgentTypeDto>> GetAllAgents();

    Task<AgentDto> CreateAgentAsync(CreateAgentInputDto dto);
    Task<List<AgentInstanceDto>> GetAllAgentInstances(GetAllAgentInstancesQueryDto queryDto);
    Task<AgentDto> GetAgentAsync(Guid guid);
    Task<AgentDto> UpdateAgentAsync(Guid guid, UpdateAgentInputDto dto);
    Task<SubAgentDto> AddSubAgentAsync(Guid guid, AddSubAgentDto addSubAgentDto);
    Task<SubAgentDto> RemoveSubAgentAsync(Guid guid, RemoveSubAgentDto removeSubAgentDto);
    Task RemoveAllSubAgentAsync(Guid guid);
    Task<AgentRelationshipDto> GetAgentRelationshipAsync(Guid guid);
    Task DeleteAgentAsync(Guid guid);
}