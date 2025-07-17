using System;
using System.Collections.Generic;

namespace Aevatar.Domain.WorkflowOrchestration
{
    /// <summary>
    /// Agent scan operation statistics
    /// </summary>
    public class AgentScanStatistics
    {
        /// <summary>
        /// Timestamp of the last scan operation
        /// </summary>
        public DateTime LastScanTime { get; set; }

        /// <summary>
        /// Total number of assemblies scanned
        /// </summary>
        public int AssembliesScanned { get; set; }

        /// <summary>
        /// Total number of types examined
        /// </summary>
        public int TypesScanned { get; set; }

        /// <summary>
        /// Total number of valid agents found
        /// </summary>
        public int TotalAgentsFound { get; set; }

        /// <summary>
        /// Duration of the scan operation
        /// </summary>
        public TimeSpan ScanDuration { get; set; }

        /// <summary>
        /// Distribution of agents by category
        /// </summary>
        public Dictionary<string, int> AgentsByCategory { get; set; } = new();

        /// <summary>
        /// Error messages encountered during scanning
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new();
    }
} 