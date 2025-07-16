using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Domain.WorkflowOrchestration;

namespace Aevatar.Application.Contracts.WorkflowOrchestration
{
    /// <summary>
    /// 工作流JSON验证器接口
    /// </summary>
    public interface IWorkflowJsonValidator
    {
        /// <summary>
        /// 验证和解析工作流JSON
        /// </summary>
        /// <param name="jsonContent">JSON内容</param>
        /// <returns>验证结果</returns>
        Task<WorkflowJsonValidationResult> ValidateWorkflowJsonAsync(string jsonContent);

        /// <summary>
        /// 清理JSON内容（移除markdown标记等）
        /// </summary>
        /// <param name="jsonContent">原始JSON内容</param>
        /// <returns>清理后的JSON内容</returns>
        string CleanJsonContent(string jsonContent);
    }

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
} 