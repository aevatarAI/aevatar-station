using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Domain.WorkflowOrchestration
{
    /// <summary>
    /// Agent索引信息 - 存储Agent的完整描述和能力信息
    /// </summary>
    public class AgentIndexInfo
    {
        /// <summary>
        /// Agent唯一标识符
        /// </summary>
        [Required]
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// Agent名称
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Agent类型全名（用于反射实例化）
        /// </summary>
        [Required]
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// L1描述：100-150字符简短描述，用于快速语义匹配
        /// </summary>
        [Required]
        [StringLength(150, MinimumLength = 50)]
        public string L1Description { get; set; } = string.Empty;

        /// <summary>
        /// L2描述：300-500字符详细描述，用于精确能力分析
        /// </summary>
        [Required]
        [StringLength(500, MinimumLength = 200)]
        public string L2Description { get; set; } = string.Empty;

        /// <summary>
        /// Agent分类标签
        /// </summary>
        public List<string> Categories { get; set; } = new();

        /// <summary>
        /// 输入参数定义
        /// </summary>
        public Dictionary<string, AgentParameterInfo> InputParameters { get; set; } = new();

        /// <summary>
        /// 输出参数定义
        /// </summary>
        public Dictionary<string, AgentParameterInfo> OutputParameters { get; set; } = new();

        /// <summary>
        /// Agent依赖的其他服务或资源
        /// </summary>
        public List<string> Dependencies { get; set; } = new();

        /// <summary>
        /// 执行复杂度评级（1-10）
        /// </summary>
        [Range(1, 10)]
        public int ComplexityLevel { get; set; } = 1;

        /// <summary>
        /// 预估执行时间（毫秒）
        /// </summary>
        public int EstimatedExecutionTime { get; set; } = 1000;

        /// <summary>
        /// 是否支持并行执行
        /// </summary>
        public bool SupportParallelExecution { get; set; } = true;

        /// <summary>
        /// Agent版本
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后扫描时间
        /// </summary>
        public DateTime LastScannedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Agent参数信息
    /// </summary>
    public class AgentParameterInfo
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数类型
        /// </summary>
        [Required]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 参数描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否必需参数
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 默认值
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// 参数验证规则
        /// </summary>
        public List<string> ValidationRules { get; set; } = new();
    }
} 