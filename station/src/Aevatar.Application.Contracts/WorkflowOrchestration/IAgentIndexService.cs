using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Domain.WorkflowOrchestration;

namespace Aevatar.Application.Contracts.WorkflowOrchestration
{
    /// <summary>
    /// Agent index service - unified service for Agent discovery, indexing, and retrieval
    /// </summary>
    public interface IAgentIndexService
    {
        /// <summary>
        /// Get all available Agent information
        /// </summary>
        Task<IEnumerable<AgentIndexInfo>> GetAllAgentsAsync();

        /// <summary>
        /// Get specific Agent information by ID
        /// </summary>
        Task<AgentIndexInfo?> GetAgentByIdAsync(string agentId);

        /// <summary>
        /// Search Agents (supports keyword and category filtering)
        /// </summary>
        Task<IEnumerable<AgentIndexInfo>> SearchAgentsAsync(string? query = null, string[]? categories = null, int limit = 50);

        /// <summary>
        /// Search Agents with extended filtering options
        /// </summary>
        Task<List<AgentIndexInfo>> SearchAgentsAsync(string searchTerm = null, string category = null);

        /// <summary>
        /// Refresh Agent index manually
        /// </summary>
        Task<AgentIndexRefreshResult> RefreshIndexAsync();

        /// <summary>
        /// Get scan statistics
        /// </summary>
        AgentScanStatistics GetScanStatistics();
    }
} 