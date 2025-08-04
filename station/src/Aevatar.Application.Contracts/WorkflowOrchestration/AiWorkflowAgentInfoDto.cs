using System;

namespace Aevatar.Application.Contracts.WorkflowOrchestration;

/// <summary>
/// AI工作流Agent信息DTO
/// </summary>
public class AiWorkflowAgentInfoDto
{
    /// <summary>
    /// Agent名称（类名）
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent类型（完整类型名）
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent描述（来自DescriptionAttribute）
    /// </summary>
    public string Description { get; set; } = string.Empty;
} 