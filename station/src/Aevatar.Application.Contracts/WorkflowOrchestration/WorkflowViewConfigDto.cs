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
        /// Workflow name (top level)
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Properties containing workflow configuration
        /// </summary>
        [Required]
        public WorkflowPropertiesDto Properties { get; set; } = new();
    }

    /// <summary>
    /// Workflow properties DTO
    /// </summary>
    public class WorkflowPropertiesDto
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
        /// Workflow name (in properties)
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
        /// Unique node identifier
        /// </summary>
        [Required]
        public string NodeId { get; set; } = string.Empty;

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
        public WorkflowNodeExtendedDataDto ExtendedData { get; set; } = new();

        /// <summary>
        /// Properties containing input parameters for the agent
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// Workflow node extended data DTO
    /// </summary>
    public class WorkflowNodeExtendedDataDto
    {
        /// <summary>
        /// X position coordinate
        /// </summary>
        [Required]
        public string XPosition { get; set; } = "0";

        /// <summary>
        /// Y position coordinate
        /// </summary>
        [Required]
        public string YPosition { get; set; } = "0";
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
        public string NextNodeId { get; set; } = string.Empty;
    }
} 