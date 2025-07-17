using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Application.Contracts.WorkflowOrchestration
{
    /// <summary>
    /// Workflow view configuration DTO matching frontend expected format
    /// </summary>
    public class WorkflowViewConfigDto
    {
        /// <summary>
        /// List of workflow nodes
        /// </summary>
        [Required]
        public List<WorkflowNodeDto> WorkflowNodeList { get; set; } = new();

        /// <summary>
        /// List of node connections/units
        /// </summary>
        [Required]
        public List<WorkflowNodeUnitDto> WorkflowNodeUnitList { get; set; } = new();

        /// <summary>
        /// Workflow name
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Workflow node DTO
    /// </summary>
    public class WorkflowNodeDto
    {
        /// <summary>
        /// Agent type name (e.g., DataProcessorAgent)
        /// </summary>
        [Required]
        public string AgentType { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the node
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Extended data including position and size information
        /// </summary>
        [Required]
        public Dictionary<string, string> ExtendedData { get; set; } = new();

        /// <summary>
        /// Properties containing input parameters for the agent
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// Unique node identifier
        /// </summary>
        [Required]
        public string NodeId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Workflow node connection/unit DTO
    /// </summary>
    public class WorkflowNodeUnitDto
    {
        /// <summary>
        /// Current node ID
        /// </summary>
        [Required]
        public string NodeId { get; set; } = string.Empty;

        /// <summary>
        /// Next node ID to execute
        /// </summary>
        [Required]
        public string NextnodeId { get; set; } = string.Empty;
    }
} 