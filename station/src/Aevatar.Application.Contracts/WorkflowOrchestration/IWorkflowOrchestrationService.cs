using System.Threading.Tasks;
using Aevatar.Domain.WorkflowOrchestration;

namespace Aevatar.Application.Contracts.WorkflowOrchestration;

/// <summary>
/// 工作流编排服务接口
/// </summary>
public interface IWorkflowOrchestrationService
{
    /// <summary>
    /// 根据用户目标生成完整工作流
    /// </summary>
    /// <param name="userGoal">用户目标描述</param>
    /// <returns>生成的工作流定义</returns>
    Task<WorkflowDefinition> GenerateWorkflowAsync(string userGoal);

    /// <summary>
    /// Parse workflow JSON to frontend format DTO
    /// </summary>
    /// <param name="jsonContent">JSON content from LLM</param>
    /// <returns>Parsed WorkflowViewConfigDto</returns>
    Task<WorkflowViewConfigDto?> ParseWorkflowJsonToViewConfigAsync(string jsonContent);
} 