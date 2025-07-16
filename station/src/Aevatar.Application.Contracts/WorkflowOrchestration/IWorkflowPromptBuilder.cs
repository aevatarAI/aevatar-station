using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Domain.WorkflowOrchestration;

namespace Aevatar.Application.Contracts.WorkflowOrchestration
{
    /// <summary>
    /// 工作流提示词构建服务接口 - 简化版本
    /// </summary>
    public interface IWorkflowPromptBuilder
    {
        /// <summary>
        /// 构建工作流生成提示词
        /// </summary>
        /// <param name="userGoal">用户目标</param>
        /// <param name="availableAgents">可用Agent列表</param>
        /// <param name="options">生成选项</param>
        /// <returns>构建的提示词</returns>
        Task<string> BuildWorkflowGenerationPromptAsync(
            string userGoal,
            IEnumerable<AgentIndexInfo> availableAgents,
            WorkflowGenerationOptions options);

        /// <summary>
        /// 验证提示词的有效性
        /// </summary>
        /// <param name="prompt">待验证的提示词</param>
        /// <returns>验证结果</returns>
        Task<PromptValidationResult> ValidatePromptAsync(string prompt);

        /// <summary>
        /// 构建Agent选择提示词
        /// </summary>
        /// <param name="userGoal">用户目标</param>
        /// <param name="availableAgents">可用Agent列表</param>
        /// <param name="options">生成选项</param>
        /// <returns>Agent选择提示词</returns>
        Task<string> BuildAgentSelectionPromptAsync(
            string userGoal,
            IEnumerable<AgentIndexInfo> availableAgents,
            WorkflowGenerationOptions options);
    }

    /// <summary>
    /// 提示词构建请求
    /// </summary>
    public class PromptBuildRequest
    {
        /// <summary>
        /// 用户目标描述
        /// </summary>
        public string UserGoal { get; set; } = string.Empty;

        /// <summary>
        /// 可用的Agent列表
        /// </summary>
        public List<AgentIndexInfo> AvailableAgents { get; set; } = new();

        /// <summary>
        /// 工作流生成选项
        /// </summary>
        public WorkflowGenerationOptions Options { get; set; } = new();

        /// <summary>
        /// 用户上下文信息
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>
        /// 提示词构建配置
        /// </summary>
        public PromptBuildConfiguration Configuration { get; set; } = new();
    }

    /// <summary>
    /// 提示词构建配置
    /// </summary>
    public class PromptBuildConfiguration
    {
        /// <summary>
        /// 是否包含Agent详细描述
        /// </summary>
        public bool IncludeDetailedAgentDescriptions { get; set; } = true;

        /// <summary>
        /// 是否包含示例工作流
        /// </summary>
        public bool IncludeExamples { get; set; } = true;

        /// <summary>
        /// 最大提示词长度
        /// </summary>
        public int MaxPromptLength { get; set; } = 16000;

        /// <summary>
        /// Agent描述策略
        /// </summary>
        public AgentDescriptionStrategy DescriptionStrategy { get; set; } = AgentDescriptionStrategy.Balanced;

        /// <summary>
        /// 输出格式偏好
        /// </summary>
        public OutputFormatPreference OutputFormat { get; set; } = OutputFormatPreference.StructuredJSON;

        /// <summary>
        /// 语言偏好
        /// </summary>
        public string LanguagePreference { get; set; } = "zh-CN";
    }

    /// <summary>
    /// Agent描述策略
    /// </summary>
    public enum AgentDescriptionStrategy
    {
        /// <summary>
        /// 仅使用L1简短描述
        /// </summary>
        L1Only = 1,

        /// <summary>
        /// 仅使用L2详细描述
        /// </summary>
        L2Only = 2,

        /// <summary>
        /// 平衡使用L1和L2描述
        /// </summary>
        Balanced = 3,

        /// <summary>
        /// 智能选择最相关的描述
        /// </summary>
        Adaptive = 4
    }

    /// <summary>
    /// 输出格式偏好
    /// </summary>
    public enum OutputFormatPreference
    {
        /// <summary>
        /// 结构化JSON格式
        /// </summary>
        StructuredJSON = 1,

        /// <summary>
        /// 简化JSON格式
        /// </summary>
        SimpleJSON = 2,

        /// <summary>
        /// 带注释的JSON格式
        /// </summary>
        AnnotatedJSON = 3
    }

    /// <summary>
    /// 提示词验证结果
    /// </summary>
    public class PromptValidationResult
    {
        /// <summary>
        /// 是否验证成功
        /// </summary>
        public bool IsValid { get; set; }

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