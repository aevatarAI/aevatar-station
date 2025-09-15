using System;

namespace Aevatar.WorkflowRun;

/// <summary>
/// Workflow运行结果DTO
/// </summary>
public class WorkflowRunResultDto
{
    /// <summary>
    /// 执行是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 工作流协调器Agent ID
    /// </summary>
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// 执行结果消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
