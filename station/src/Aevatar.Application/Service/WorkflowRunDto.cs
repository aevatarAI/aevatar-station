using System;
using System.Collections.Generic;
using Aevatar.AgentValidation;

namespace Aevatar.Service;

public class WorkflowRunRequestDto
{
    public Guid ViewAgentId { get; set; }
    public Dictionary<string, object> EventProperties { get; set; } = new();
}

public class WorkflowRunResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}