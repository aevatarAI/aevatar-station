using System.Threading.Tasks;
using Aevatar.Application.Contracts.WorkflowOrchestration;

namespace Aevatar.Service;

/// <summary>
/// 工作流编排服务接口
/// </summary>
public interface IWorkflowOrchestrationService
{
    /// <summary>
    /// 根据用户目标生成工作流视图配置，直接返回前端可渲染的格式
    /// </summary>
    /// <param name="userGoal">用户目标描述</param>
    /// <returns>前端可渲染的工作流视图配置</returns>
    Task<WorkflowViewConfigDto?> GenerateWorkflowAsync(string userGoal);
} 