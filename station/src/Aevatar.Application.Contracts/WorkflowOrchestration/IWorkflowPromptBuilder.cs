using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Domain.WorkflowOrchestration;

namespace Aevatar.Application.Contracts.WorkflowOrchestration
{
    /// <summary>
    /// 工作流提示词构建服务接口 - 一体化提示词构建
    /// </summary>
    public interface IWorkflowPromptBuilder
    {
        /// <summary>
        /// 构建一体化工作流编排提示词
        /// </summary>
        /// <param name="request">提示词构建请求</param>
        /// <returns>构建的提示词</returns>
        Task<PromptBuildResult> BuildWorkflowPromptAsync(PromptBuildRequest request);

        /// <summary>
        /// 根据复杂度生成提示词模板
        /// </summary>
        /// <param name="complexity">工作流复杂度</param>
        /// <param name="options">模板选项</param>
        /// <returns>提示词模板</returns>
        Task<PromptTemplate> GetPromptTemplateAsync(WorkflowComplexity complexity, TemplateOptions options);

        /// <summary>
        /// 验证提示词的有效性
        /// </summary>
        /// <param name="prompt">待验证的提示词</param>
        /// <returns>验证结果</returns>
        Task<PromptValidationResult> ValidatePromptAsync(string prompt);

        /// <summary>
        /// 优化提示词以提升LLM响应质量
        /// </summary>
        /// <param name="prompt">原始提示词</param>
        /// <param name="optimizationOptions">优化选项</param>
        /// <returns>优化后的提示词</returns>
        Task<PromptOptimizationResult> OptimizePromptAsync(string prompt, PromptOptimizationOptions optimizationOptions);
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

    // 由于文件长度限制，其他DTO类如PromptBuildResult等将在后续添加
    // 主要包含：PromptBuildResult, PromptComponents, PromptBuildStatistics等
} 