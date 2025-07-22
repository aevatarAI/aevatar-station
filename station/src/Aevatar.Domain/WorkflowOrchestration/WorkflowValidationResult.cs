using System.Collections.Generic;

namespace Aevatar.Domain.WorkflowOrchestration;

/// <summary>
/// 工作流JSON验证结果
/// </summary>
public class WorkflowJsonValidationResult
{
    /// <summary>
    /// 是否验证成功
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 解析后的工作流定义
    /// </summary>
    public WorkflowDefinition? ParsedWorkflow { get; set; }

    /// <summary>
    /// 错误列表
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// 警告列表
    /// </summary>
    public List<ValidationWarning> Warnings { get; set; } = new();
} 