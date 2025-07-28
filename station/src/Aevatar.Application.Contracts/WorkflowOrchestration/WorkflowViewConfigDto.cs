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

        /// <summary>
        /// Generation status (success/partial/template_recommendation/manual_guidance/system_fallback)
        /// </summary>
        public string? GenerationStatus { get; set; }

        /// <summary>
        /// Clarity score of user goal (1-5, where 5 is very clear)
        /// </summary>
        public int? ClarityScore { get; set; }

        /// <summary>
        /// Error information if generation failed or needs user attention
        /// </summary>
        public WorkflowErrorInfoDto? ErrorInfo { get; set; }

        /// <summary>
        /// Completion percentage for partial generation (0-100)
        /// </summary>
        public int? CompletionPercentage { get; set; }

        /// <summary>
        /// Guidance for completing partial workflows
        /// </summary>
        public WorkflowCompletionGuidanceDto? CompletionGuidance { get; set; }

        /// <summary>
        /// System information for fallback scenarios
        /// </summary>
        public WorkflowSystemInfoDto? SystemInfo { get; set; }
    }

    /// <summary>
    /// Workflow error information DTO
    /// </summary>
    public class WorkflowErrorInfoDto
    {
        /// <summary>
        /// Type of error (prompt_ambiguity/insufficient_information/technical_limitation/system_error/unsupported_requirement)
        /// </summary>
        public string? ErrorType { get; set; }

        /// <summary>
        /// Human-readable error message
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// List of actionable steps user can take to resolve the issue
        /// </summary>
        public List<string> ActionableSteps { get; set; } = new();
    }

    /// <summary>
    /// Workflow completion guidance DTO for partial generation
    /// </summary>
    public class WorkflowCompletionGuidanceDto
    {
        /// <summary>
        /// Suggested nodes to complete the workflow
        /// </summary>
        public List<string> SuggestedNodes { get; set; } = new();

        /// <summary>
        /// Specific steps to complete the workflow
        /// </summary>
        public List<string> NextSteps { get; set; } = new();
    }

    /// <summary>
    /// System information DTO for fallback scenarios
    /// </summary>
    public class WorkflowSystemInfoDto
    {
        /// <summary>
        /// Whether fallback was triggered
        /// </summary>
        public bool FallbackTriggered { get; set; }

        /// <summary>
        /// Timestamp when the issue occurred
        /// </summary>
        public string? Timestamp { get; set; }

        /// <summary>
        /// Suggested time to retry the operation
        /// </summary>
        public string? SuggestedRetryTime { get; set; }
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