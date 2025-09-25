using System;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.WorkflowRun;

/// <summary>
/// Workflow运行请求DTO
/// </summary>
public class WorkflowRunRequestDto
{
    /// <summary>
    /// 工作流视图Agent ID
    /// </summary>
    [Required]
    public Guid ViewAgentId { get; set; }

    /// <summary>
    /// 事件属性（可选）
    /// </summary>
    public object? EventProperties { get; set; }
}
