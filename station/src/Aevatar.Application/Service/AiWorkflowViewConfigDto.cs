using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Service
{
    /// <summary>
    /// AI workflow view configuration DTO matching frontend expected format
    /// </summary>
    public class AiWorkflowViewConfigDto
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
        public AiWorkflowPropertiesDto Properties { get; set; } = new();
    }

    /// <summary>
    /// AI workflow properties DTO
    /// </summary>
    public class AiWorkflowPropertiesDto
    {
        /// <summary>
        /// List of workflow nodes
        /// </summary>
        [Required]
        public List<AiWorkflowNodeDto> WorkflowNodeList { get; set; } = new();

        /// <summary>
        /// List of node connections/units
        /// </summary>
        [Required]
        public List<AiWorkflowNodeUnitDto> WorkflowNodeUnitList { get; set; } = new();

        /// <summary>
        /// Workflow name (in properties)
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// AI workflow node DTO
    /// </summary>
    public class AiWorkflowNodeDto
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
        public AiWorkflowNodeExtendedDataDto ExtendedData { get; set; } = new();

        /// <summary>
        /// Properties containing input parameters for the agent
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    /// <summary>
    /// AI workflow node extended data DTO
    /// </summary>
    public class AiWorkflowNodeExtendedDataDto
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
    /// AI workflow node connection/unit DTO
    /// </summary>
    public class AiWorkflowNodeUnitDto
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

        /// <summary>
        /// Connection type - defaults to Sequential
        /// </summary>
        public ConnectionType ConnectionType { get; set; } = ConnectionType.Sequential;
    }

    /// <summary>
    /// 工作流连接类型
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// 顺序连接 - 默认的数据流或顺序执行
        /// </summary>
        Sequential = 1,

        /// <summary>
        /// 条件连接 - 基于条件的分支
        /// </summary>
        Conditional = 2,

        /// <summary>
        /// 并行连接 - 并行执行分支
        /// </summary>
        Parallel = 3
    }
} 