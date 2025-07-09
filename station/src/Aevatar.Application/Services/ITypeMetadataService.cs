// ABOUTME: This file defines the ITypeMetadataService interface for type metadata operations
// ABOUTME: Provides contract for assembly scanning, capability queries, and metadata caching

using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Models;

namespace Aevatar.Application.Services
{
    public interface ITypeMetadataService
    {
        Task<List<AgentTypeMetadata>> GetTypesByCapabilityAsync(string capability);
        Task<AgentTypeMetadata> GetTypeMetadataAsync(string agentType);
        Task<List<AgentTypeMetadata>> GetAllTypesAsync();
        Task RefreshMetadataAsync();
    }
}