// ABOUTME: This file defines the ITypeMetadataGrain interface for Orleans integration
// ABOUTME: Provides cluster-wide metadata persistence and sharing capabilities

using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Models;
using Orleans;

namespace Aevatar.Application.Grains
{
    public interface ITypeMetadataGrain : IGrainWithIntegerKey
    {
        Task SetMetadataAsync(List<AgentTypeMetadata> metadata);
        Task<List<AgentTypeMetadata>> GetAllMetadataAsync();
        Task<List<AgentTypeMetadata>> GetByCapabilityAsync(string capability);
        Task<AgentTypeMetadata> GetByTypeAsync(string agentType);
        Task ClearMetadataAsync();
    }
}