using System;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.WorkflowRun;

/// <summary>
/// ✅ TASK 17: Workflow event forwarding request DTO for service-direct execution
/// </summary>
public class WorkflowEventForwardingRequestDto
{
    /// <summary>
    /// Workflow Coordinator Agent ID (not View Agent ID)
    /// </summary>
    [Required]
    public Guid CoordinatorAgentId { get; set; }

    /// <summary>
    /// Initial message for the workflow text data pipeline
    /// </summary>
    [Required]
    public string InitialMessage { get; set; } = string.Empty;
}
