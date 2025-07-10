// ABOUTME: This file defines the ITypeMetadataGrain interface for Orleans cluster-wide metadata operations
// ABOUTME: Provides contracts for type metadata CRUD operations and size monitoring across Orleans silos

using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Models;
using Orleans;

namespace Aevatar.Application.Grains
{
    /// <summary>
    /// Orleans grain interface for cluster-wide agent type metadata management.
    /// Provides persistent storage and retrieval of agent type metadata with size monitoring.
    /// </summary>
    public interface ITypeMetadataGrain : IGrainWithIntegerKey
    {
        /// <summary>
        /// Sets the complete metadata collection for all agent types.
        /// </summary>
        /// <param name="metadata">List of agent type metadata</param>
        /// <returns>Task representing the async operation</returns>
        Task SetMetadataAsync(List<AgentTypeMetadata> metadata);
        
        /// <summary>
        /// Gets all agent type metadata from the grain.
        /// </summary>
        /// <returns>List of all agent type metadata</returns>
        Task<List<AgentTypeMetadata>> GetAllMetadataAsync();
        
        /// <summary>
        /// Gets agent types that support the specified capability.
        /// </summary>
        /// <param name="capability">The capability to search for</param>
        /// <returns>List of agent types supporting the capability</returns>
        Task<List<AgentTypeMetadata>> GetByCapabilityAsync(string capability);
        
        /// <summary>
        /// Gets metadata for a specific agent type.
        /// </summary>
        /// <param name="agentType">The agent type name</param>
        /// <returns>Agent type metadata or null if not found</returns>
        Task<AgentTypeMetadata> GetByTypeAsync(string agentType);
        
        /// <summary>
        /// Clears all metadata from the grain.
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task ClearMetadataAsync();
        
        /// <summary>
        /// Gets statistics about the metadata storage including size and capacity usage.
        /// </summary>
        /// <returns>Metadata statistics including size and percentage of 16MB limit</returns>
        Task<MetadataStats> GetStatsAsync();
    }
}