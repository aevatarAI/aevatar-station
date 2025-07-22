using System;

namespace Aevatar.Domain.WorkflowOrchestration
{
    /// <summary>
    /// Agent index refresh operation result
    /// </summary>
    public class AgentIndexRefreshResult
    {
        /// <summary>
        /// Whether the refresh operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Total number of agents scanned
        /// </summary>
        public int TotalScanned { get; set; }

        /// <summary>
        /// Number of new agents discovered
        /// </summary>
        public int NewAgents { get; set; }

        /// <summary>
        /// Number of existing agents updated
        /// </summary>
        public int UpdatedAgents { get; set; }

        /// <summary>
        /// Refresh operation duration in milliseconds
        /// </summary>
        public long RefreshDuration { get; set; }

        /// <summary>
        /// Error message if refresh failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp of the refresh operation
        /// </summary>
        public DateTime RefreshTime { get; set; }
    }
} 