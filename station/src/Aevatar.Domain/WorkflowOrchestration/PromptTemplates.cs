using System;
using System.Collections.Generic;

namespace Aevatar.Domain.WorkflowOrchestration
{
    /// <summary>
    /// 提示词构建结果
    /// </summary>
    public class PromptBuildResult
    {
        public string Prompt { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public int EstimatedTokenCount { get; set; }
        public TimeSpan BuildTime { get; set; }
    }

    /// <summary>
    /// 提示词模板
    /// </summary>
    public class PromptTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public WorkflowComplexity TargetComplexity { get; set; }
        public List<string> RequiredParameters { get; set; } = new List<string>();
        public Dictionary<string, string> DefaultValues { get; set; } = new Dictionary<string, string>();
        public string Version { get; set; } = "1.0.0";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 模板选项
    /// </summary>
    public class TemplateOptions
    {
        public bool IncludeExamples { get; set; } = true;
        public bool IncludeConstraints { get; set; } = true;
        public bool OptimizeForTokens { get; set; } = false;
        public string Language { get; set; } = "en";
        public Dictionary<string, object> CustomParameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 提示词验证结果
    /// </summary>
    public class PromptValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
        public List<ValidationWarning> Warnings { get; set; } = new List<ValidationWarning>();
        public int EstimatedTokenCount { get; set; }
        public string ValidationSummary { get; set; } = string.Empty;
    }

    /// <summary>
    /// 验证错误
    /// </summary>
    public class ValidationError
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public object? Value { get; set; }
    }

    /// <summary>
    /// 验证警告
    /// </summary>
    public class ValidationWarning
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
    }

    /// <summary>
    /// 提示词优化选项
    /// </summary>
    public class PromptOptimizationOptions
    {
        public bool OptimizeForTokens { get; set; } = true;
        public bool OptimizeForClarity { get; set; } = true;
        public bool OptimizeForPerformance { get; set; } = false;
        public int MaxTokens { get; set; } = 4000;
        public string TargetModel { get; set; } = "gpt-4";
        public Dictionary<string, object> CustomOptions { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 提示词优化结果
    /// </summary>
    public class PromptOptimizationResult
    {
        public string OptimizedPrompt { get; set; } = string.Empty;
        public string OriginalPrompt { get; set; } = string.Empty;
        public int TokenReduction { get; set; }
        public double ImprovementPercentage { get; set; }
        public List<OptimizationChange> Changes { get; set; } = new List<OptimizationChange>();
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// 优化更改
    /// </summary>
    public class OptimizationChange
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Before { get; set; } = string.Empty;
        public string After { get; set; } = string.Empty;
        public int TokensSaved { get; set; }
    }


} 