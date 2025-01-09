using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.AtomicAgent;
using Aevatar.CombinationAgent;

namespace Aevatar.Service;

public interface IAgentService
{
    Task<AtomicAgentDto> GetAtomicAgentAsync(string id);
    Task<AtomicAgentDto> CreateAtomicAgentAsync(CreateAtomicAgentDto createDto);
    Task<AtomicAgentDto> UpdateAtomicAgentAsync(string id, UpdateAtomicAgentDto updateDto);
    Task<List<AtomicAgentDto>> GetAtomicAgentsAsync(string userAddress);

    Task DeleteAtomicAgentAsync(string id);
    Task<CombinationAgentDto> GetCombinationAsync(string id);
    Task<CombinationAgentDto> CombineAgentAsync(CombineAgentDto combineAgentDto);
    Task<CombinationAgentDto> UpdateCombinationAsync(string id, UpdateCombinationDto updateCombinationDto);
    Task DeleteCombinationAsync(string id);
}