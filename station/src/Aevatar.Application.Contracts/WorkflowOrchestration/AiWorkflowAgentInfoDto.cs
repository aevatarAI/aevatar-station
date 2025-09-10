using Orleans;

namespace Aevatar.Application.Contracts.WorkflowOrchestration;

/// <summary>
/// AI工作流Agent信息DTO
/// </summary>
[GenerateSerializer]
public class AiWorkflowAgentInfoDto
{
    /// <summary>
    /// Agent名称（类名）
    /// </summary>
    [Id(0)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent类型（完整类型名）
    /// </summary>
    [Id(1)]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Agent描述（来自DescriptionAttribute）
    /// </summary>
    [Id(2)]
    public string Description { get; set; } = string.Empty;
} 