using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Aevatar.Domain.WorkflowOrchestration
{
    /// <summary>
    /// Agent描述属性 - 用于标记Agent类的能力和描述信息
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AgentDescriptionAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="agentId">Agent唯一标识</param>
        /// <param name="name">Agent名称</param>
        /// <param name="l1Description">L1简短描述（100-150字符）</param>
        /// <param name="l2Description">L2详细描述（300-500字符）</param>
        public AgentDescriptionAttribute(
            string agentId,
            string name, 
            string l1Description, 
            string l2Description)
        {
            if (string.IsNullOrWhiteSpace(agentId))
                throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
            
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Agent name cannot be null or empty", nameof(name));
            
            if (string.IsNullOrWhiteSpace(l1Description))
                throw new ArgumentException("L1 description cannot be null or empty", nameof(l1Description));
            
            if (string.IsNullOrWhiteSpace(l2Description))
                throw new ArgumentException("L2 description cannot be null or empty", nameof(l2Description));

            // 验证描述长度
            if (l1Description.Length < 50 || l1Description.Length > 150)
                throw new ArgumentException("L1 description must be between 50-150 characters", nameof(l1Description));
            
            if (l2Description.Length < 200 || l2Description.Length > 500)
                throw new ArgumentException("L2 description must be between 200-500 characters", nameof(l2Description));

            AgentId = agentId;
            Name = name;
            L1Description = l1Description;
            L2Description = l2Description;
        }

        /// <summary>
        /// Agent唯一标识符
        /// </summary>
        public string AgentId { get; }

        /// <summary>
        /// Agent名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// L1描述：100-150字符简短描述，用于快速语义匹配
        /// </summary>
        public string L1Description { get; }

        /// <summary>
        /// L2描述：300-500字符详细描述，用于精确能力分析
        /// </summary>
        public string L2Description { get; }

        /// <summary>
        /// Agent分类标签
        /// </summary>
        public string[] Categories { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 预估执行时间（毫秒）
        /// </summary>
        public int EstimatedExecutionTime { get; set; } = 1000;

        /// <summary>
        /// Agent标签（用于搜索和分类）
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 是否为实验性功能
        /// </summary>
        public bool IsExperimental { get; set; } = false;

        /// <summary>
        /// 是否已弃用
        /// </summary>
        public bool IsDeprecated { get; set; } = false;

        /// <summary>
        /// 替代Agent（当前Agent被弃用时的推荐替代）
        /// </summary>
        public string? ReplacementAgent { get; set; }

        /// <summary>
        /// Agent作者信息
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// 文档链接
        /// </summary>
        public string? DocumentationUrl { get; set; }

        /// <summary>
        /// 示例使用场景
        /// </summary>
        public string[] ExampleUseCases { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 验证属性设置的有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult Validate()
        {
            var errors = new List<string>();

            // 验证必需字段
            if (string.IsNullOrWhiteSpace(AgentId))
                errors.Add("AgentId is required");

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Name is required");

            if (string.IsNullOrWhiteSpace(L1Description))
                errors.Add("L1Description is required");

            if (string.IsNullOrWhiteSpace(L2Description))
                errors.Add("L2Description is required");

            // 验证描述长度
            if (L1Description?.Length < 50 || L1Description?.Length > 150)
                errors.Add("L1Description must be between 50-150 characters");

            if (L2Description?.Length < 200 || L2Description?.Length > 500)
                errors.Add("L2Description must be between 200-500 characters");

            // 验证弃用状态
            if (IsDeprecated && string.IsNullOrWhiteSpace(ReplacementAgent))
                errors.Add("ReplacementAgent should be specified when Agent is deprecated");

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// 验证版本号格式
        /// </summary>
        /// <param name="version">版本号</param>
        /// <returns>是否有效</returns>
        private static bool IsValidVersionFormat(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return false;

            var parts = version.Split('.');
            if (parts.Length != 3)
                return false;

            return parts.All(part => int.TryParse(part, out _));
        }

        /// <summary>
        /// 获取Agent的完整描述信息
        /// </summary>
        /// <returns>描述信息字典</returns>
        public Dictionary<string, object> GetDescriptionInfo()
        {
            return new Dictionary<string, object>
            {
                ["AgentId"] = AgentId,
                ["Name"] = Name,
                ["L1Description"] = L1Description,
                ["L2Description"] = L2Description,
                ["Categories"] = Categories,
                ["EstimatedExecutionTime"] = EstimatedExecutionTime,
                ["Tags"] = Tags,
                ["IsExperimental"] = IsExperimental,
                ["IsDeprecated"] = IsDeprecated,
                ["ReplacementAgent"] = ReplacementAgent,
                ["Author"] = Author,
                ["DocumentationUrl"] = DocumentationUrl,
                ["ExampleUseCases"] = ExampleUseCases
            };
        }

        /// <summary>
        /// 检查是否匹配指定的搜索条件
        /// </summary>
        /// <param name="query">搜索查询</param>
        /// <param name="categories">分类过滤</param>
        /// <param name="tags">标签过滤</param>
        /// <returns>是否匹配</returns>
        public bool MatchesSearchCriteria(string? query = null, string[]? categories = null, string[]? tags = null)
        {
            // 查询匹配检查
            if (!string.IsNullOrWhiteSpace(query))
            {
                var queryLower = query.ToLowerInvariant();
                if (!Name.ToLowerInvariant().Contains(queryLower) &&
                    !L1Description.ToLowerInvariant().Contains(queryLower) &&
                    !L2Description.ToLowerInvariant().Contains(queryLower))
                {
                    return false;
                }
            }

            // 分类匹配检查
            if (categories?.Length > 0)
            {
                if (!categories.Any(cat => Categories.Contains(cat, StringComparer.OrdinalIgnoreCase)))
                    return false;
            }

            // 标签匹配检查
            if (tags?.Length > 0)
            {
                if (!tags.Any(tag => Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Agent参数描述属性 - 用于标记Agent方法参数
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    public class AgentParameterAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="description">参数描述</param>
        public AgentParameterAttribute(string description)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 是否为必需参数
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 默认值
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// 参数验证规则
        /// </summary>
        public string[] ValidationRules { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 参数示例值
        /// </summary>
        public string? ExampleValue { get; set; }

        /// <summary>
        /// 参数单位（如适用）
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 最小值（数值参数）
        /// </summary>
        public double? MinValue { get; set; }

        /// <summary>
        /// 最大值（数值参数）
        /// </summary>
        public double? MaxValue { get; set; }
    }

    /// <summary>
    /// Agent输出描述属性 - 用于标记Agent方法返回值
    /// </summary>
    [AttributeUsage(AttributeTargets.ReturnValue | AttributeTargets.Method, AllowMultiple = false)]
    public class AgentOutputAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="description">输出描述</param>
        public AgentOutputAttribute(string description)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        /// <summary>
        /// 输出描述
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 输出数据类型
        /// </summary>
        public string? DataType { get; set; }

        /// <summary>
        /// 输出格式
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// 示例输出
        /// </summary>
        public string? ExampleOutput { get; set; }

        /// <summary>
        /// 是否可能为空
        /// </summary>
        public bool CanBeNull { get; set; } = false;
    }

    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否验证通过
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证错误列表
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// 验证警告列表
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }
} 