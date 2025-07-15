using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Domain.WorkflowOrchestration;

namespace Aevatar.Application.Contracts.WorkflowOrchestration
{
    /// <summary>
    /// 工作流编排服务接口 - 核心的工作流生成和管理
    /// </summary>
    public interface IWorkflowOrchestrationService
    {
        /// <summary>
        /// 根据用户目标生成完整工作流
        /// </summary>
        /// <param name="request">工作流生成请求</param>
        /// <returns>生成的工作流定义</returns>
        Task<WorkflowGenerationResult> GenerateWorkflowAsync(WorkflowGenerationRequest request);

        /// <summary>
        /// 验证工作流定义的有效性
        /// </summary>
        /// <param name="workflow">待验证的工作流定义</param>
        /// <returns>验证结果</returns>
        Task<WorkflowValidationResult> ValidateWorkflowAsync(WorkflowDefinition workflow);

        /// <summary>
        /// 优化工作流性能
        /// </summary>
        /// <param name="workflow">待优化的工作流定义</param>
        /// <returns>优化后的工作流</returns>
        Task<WorkflowOptimizationResult> OptimizeWorkflowAsync(WorkflowDefinition workflow);

        /// <summary>
        /// 分析工作流复杂度
        /// </summary>
        /// <param name="workflow">工作流定义</param>
        /// <returns>复杂度分析结果</returns>
        Task<WorkflowComplexityAnalysis> AnalyzeComplexityAsync(WorkflowDefinition workflow);

        /// <summary>
        /// 预估工作流执行时间
        /// </summary>
        /// <param name="workflow">工作流定义</param>
        /// <returns>执行时间预估结果</returns>
        Task<WorkflowExecutionEstimate> EstimateExecutionTimeAsync(WorkflowDefinition workflow);
    }

    /// <summary>
    /// 工作流生成请求
    /// </summary>
    public class WorkflowGenerationRequest
    {
        /// <summary>
        /// 用户目标描述
        /// </summary>
        public string UserGoal { get; set; } = string.Empty;

        /// <summary>
        /// 复杂度限制
        /// </summary>
        public WorkflowComplexity? MaxComplexity { get; set; }

        /// <summary>
        /// 最大节点数量限制
        /// </summary>
        public int? MaxNodes { get; set; }

        /// <summary>
        /// 最大执行时间限制（毫秒）
        /// </summary>
        public int? MaxExecutionTime { get; set; }

        /// <summary>
        /// 偏好的Agent类别
        /// </summary>
        public List<string> PreferredCategories { get; set; } = new();

        /// <summary>
        /// 排除的Agent列表
        /// </summary>
        public List<string> ExcludedAgents { get; set; } = new();

        /// <summary>
        /// 是否允许并行执行
        /// </summary>
        public bool AllowParallelExecution { get; set; } = true;

        /// <summary>
        /// 是否允许循环结构
        /// </summary>
        public bool AllowLoops { get; set; } = true;

        /// <summary>
        /// 用户上下文信息
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>
        /// 生成选项
        /// </summary>
        public WorkflowGenerationOptions Options { get; set; } = new();
    }

    /// <summary>
    /// 工作流生成选项
    /// </summary>
    public class WorkflowGenerationOptions
    {
        /// <summary>
        /// 是否包含详细的Agent选择理由
        /// </summary>
        public bool IncludeSelectionReasons { get; set; } = true;

        /// <summary>
        /// 是否自动优化生成的工作流
        /// </summary>
        public bool AutoOptimize { get; set; } = true;

        /// <summary>
        /// 是否生成UI位置信息
        /// </summary>
        public bool GenerateUIPositions { get; set; } = true;

        /// <summary>
        /// 是否包含错误处理节点
        /// </summary>
        public bool IncludeErrorHandling { get; set; } = false;

        /// <summary>
        /// LLM模型配置
        /// </summary>
        public LLMConfiguration LLMConfig { get; set; } = new();
    }

    /// <summary>
    /// LLM配置
    /// </summary>
    public class LLMConfiguration
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelName { get; set; } = "gpt-4";

        /// <summary>
        /// 最大Token数量
        /// </summary>
        public int MaxTokens { get; set; } = 8000;

        /// <summary>
        /// 温度参数
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// 超时时间（毫秒）
        /// </summary>
        public int TimeoutMs { get; set; } = 30000;
    }

    /// <summary>
    /// 工作流生成结果
    /// </summary>
    public class WorkflowGenerationResult
    {
        /// <summary>
        /// 是否生成成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 生成的工作流定义
        /// </summary>
        public WorkflowDefinition? Workflow { get; set; }

        /// <summary>
        /// 生成过程统计
        /// </summary>
        public WorkflowGenerationStatistics Statistics { get; set; } = new();

        /// <summary>
        /// 错误信息
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// 警告信息
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// 生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 工作流生成统计
    /// </summary>
    public class WorkflowGenerationStatistics
    {
        /// <summary>
        /// 总耗时（毫秒）
        /// </summary>
        public long TotalDurationMs { get; set; }

        /// <summary>
        /// LLM调用耗时（毫秒）
        /// </summary>
        public long LLMCallDurationMs { get; set; }

        /// <summary>
        /// Agent筛选耗时（毫秒）
        /// </summary>
        public long AgentSelectionDurationMs { get; set; }

        /// <summary>
        /// JSON验证耗时（毫秒）
        /// </summary>
        public long ValidationDurationMs { get; set; }

        /// <summary>
        /// 使用的Token数量
        /// </summary>
        public int TokensUsed { get; set; }

        /// <summary>
        /// 候选Agent数量
        /// </summary>
        public int CandidateAgents { get; set; }

        /// <summary>
        /// 选中Agent数量
        /// </summary>
        public int SelectedAgents { get; set; }

        /// <summary>
        /// 生成的节点数量
        /// </summary>
        public int GeneratedNodes { get; set; }

        /// <summary>
        /// 生成的连接数量
        /// </summary>
        public int GeneratedConnections { get; set; }
    }

    /// <summary>
    /// 工作流验证结果
    /// </summary>
    public class WorkflowValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证错误列表
        /// </summary>
        public List<ValidationError> Errors { get; set; } = new();

        /// <summary>
        /// 验证警告列表
        /// </summary>
        public List<ValidationWarning> Warnings { get; set; } = new();

        /// <summary>
        /// 验证通过的规则数量
        /// </summary>
        public int PassedRules { get; set; }

        /// <summary>
        /// 总验证规则数量
        /// </summary>
        public int TotalRules { get; set; }
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 错误位置（节点ID或连接ID）
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// 错误严重程度
        /// </summary>
        public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    }

    /// <summary>
    /// 验证警告
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>
        /// 警告代码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 警告消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 建议的解决方案
        /// </summary>
        public string? Suggestion { get; set; }
    }

    /// <summary>
    /// 验证严重程度
    /// </summary>
    public enum ValidationSeverity
    {
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    /// <summary>
    /// 工作流优化结果
    /// </summary>
    public class WorkflowOptimizationResult
    {
        /// <summary>
        /// 优化后的工作流
        /// </summary>
        public WorkflowDefinition OptimizedWorkflow { get; set; } = new();

        /// <summary>
        /// 优化改进列表
        /// </summary>
        public List<OptimizationImprovement> Improvements { get; set; } = new();

        /// <summary>
        /// 优化前后性能对比
        /// </summary>
        public PerformanceComparison Performance { get; set; } = new();
    }

    /// <summary>
    /// 优化改进
    /// </summary>
    public class OptimizationImprovement
    {
        /// <summary>
        /// 改进类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 改进描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 性能提升百分比
        /// </summary>
        public double ImprovementPercentage { get; set; }
    }

    /// <summary>
    /// 性能对比
    /// </summary>
    public class PerformanceComparison
    {
        /// <summary>
        /// 优化前执行时间
        /// </summary>
        public int OriginalExecutionTime { get; set; }

        /// <summary>
        /// 优化后执行时间
        /// </summary>
        public int OptimizedExecutionTime { get; set; }

        /// <summary>
        /// 性能提升百分比
        /// </summary>
        public double ImprovementPercentage { get; set; }
    }

    /// <summary>
    /// 工作流复杂度分析
    /// </summary>
    public class WorkflowComplexityAnalysis
    {
        /// <summary>
        /// 复杂度等级
        /// </summary>
        public WorkflowComplexity ComplexityLevel { get; set; }

        /// <summary>
        /// 复杂度分数（1-100）
        /// </summary>
        public int ComplexityScore { get; set; }

        /// <summary>
        /// 分析详情
        /// </summary>
        public ComplexityDetails Details { get; set; } = new();
    }

    /// <summary>
    /// 复杂度详情
    /// </summary>
    public class ComplexityDetails
    {
        /// <summary>
        /// 节点数量
        /// </summary>
        public int NodeCount { get; set; }

        /// <summary>
        /// 连接数量
        /// </summary>
        public int ConnectionCount { get; set; }

        /// <summary>
        /// 并行分支数量
        /// </summary>
        public int ParallelBranches { get; set; }

        /// <summary>
        /// 循环数量
        /// </summary>
        public int LoopCount { get; set; }

        /// <summary>
        /// 条件节点数量
        /// </summary>
        public int ConditionalNodes { get; set; }

        /// <summary>
        /// 最大嵌套深度
        /// </summary>
        public int MaxNestingDepth { get; set; }
    }

    /// <summary>
    /// 工作流执行时间预估
    /// </summary>
    public class WorkflowExecutionEstimate
    {
        /// <summary>
        /// 最小执行时间（毫秒）
        /// </summary>
        public int MinExecutionTime { get; set; }

        /// <summary>
        /// 最大执行时间（毫秒）
        /// </summary>
        public int MaxExecutionTime { get; set; }

        /// <summary>
        /// 平均执行时间（毫秒）
        /// </summary>
        public int AverageExecutionTime { get; set; }

        /// <summary>
        /// 置信度（0-1）
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 时间分解
        /// </summary>
        public Dictionary<string, int> TimeBreakdown { get; set; } = new();
    }
} 