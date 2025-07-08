using System;

namespace Aevatar.Workflow;

public class WorkflowResponseDto
{
    public Guid GroupAgentId { get; set; }
    public Guid AiAgentId { get; set; }
    public Guid TwitterAgentId { get; set; }
    public Guid SocialAgentId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
} 