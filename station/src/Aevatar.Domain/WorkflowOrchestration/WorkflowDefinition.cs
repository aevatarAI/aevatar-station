using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Domain.WorkflowOrchestration
{
    /// <summary>
    /// 工作流定义 - 完整的工作流编排描述
    /// </summary>
    public class WorkflowDefinition
    {
        /// <summary>
        /// 工作流唯一标识
        /// </summary>
        [Required]
        public string WorkflowId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 工作流名称
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 工作流描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 用户原始目标
        /// </summary>
        [Required]
        public string UserGoal { get; set; } = string.Empty;

        /// <summary>
        /// 工作流复杂度级别
        /// </summary>
        public WorkflowComplexity Complexity { get; set; } = WorkflowComplexity.Simple;

        /// <summary>
        /// 所有节点定义
        /// </summary>
        public List<WorkflowNode> Nodes { get; set; } = new();

        /// <summary>
        /// 节点间连接关系
        /// </summary>
        public List<WorkflowConnection> Connections { get; set; } = new();

        /// <summary>
        /// 全局变量定义
        /// </summary>
        public Dictionary<string, WorkflowVariable> GlobalVariables { get; set; } = new();

        /// <summary>
        /// 选中的Agent列表
        /// </summary>
        public List<SelectedAgent> SelectedAgents { get; set; } = new();

        /// <summary>
        /// 预估执行时间（毫秒）
        /// </summary>
        public int EstimatedExecutionTime { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; } = "1.0.0";
    }

    /// <summary>
    /// 工作流节点定义
    /// </summary>
    public class WorkflowNode
    {
        /// <summary>
        /// 节点唯一标识
        /// </summary>
        [Required]
        public string NodeId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 节点名称
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 节点类型
        /// </summary>
        [Required]
        public WorkflowNodeType Type { get; set; }

        /// <summary>
        /// 节点描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Agent类型名称（仅当Type为Agent时有效）
        /// </summary>
        public string? TypeName { get; set; }

        /// <summary>
        /// 节点配置参数
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();

        /// <summary>
        /// 输入变量映射
        /// </summary>
        public Dictionary<string, string> InputMappings { get; set; } = new();

        /// <summary>
        /// 输出变量映射
        /// </summary>
        public Dictionary<string, string> OutputMappings { get; set; } = new();

        /// <summary>
        /// 条件表达式（仅当Type为Condition时有效）
        /// </summary>
        public string? ConditionExpression { get; set; }

        /// <summary>
        /// 循环配置（仅当Type为Loop时有效）
        /// </summary>
        public LoopConfiguration? LoopConfig { get; set; }

        /// <summary>
        /// UI位置信息
        /// </summary>
        public NodePosition Position { get; set; } = new();
    }

    /// <summary>
    /// 工作流连接关系
    /// </summary>
    public class WorkflowConnection
    {
        /// <summary>
        /// 连接唯一标识
        /// </summary>
        [Required]
        public string ConnectionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 源节点ID
        /// </summary>
        [Required]
        public string SourceNodeId { get; set; } = string.Empty;

        /// <summary>
        /// 目标节点ID
        /// </summary>
        [Required]
        public string TargetNodeId { get; set; } = string.Empty;

        /// <summary>
        /// 连接类型
        /// </summary>
        public ConnectionType Type { get; set; } = ConnectionType.Sequential;

        /// <summary>
        /// 连接条件（可选）
        /// </summary>
        public string? Condition { get; set; }

        /// <summary>
        /// 连接标签
        /// </summary>
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// 工作流变量定义
    /// </summary>
    public class WorkflowVariable
    {
        /// <summary>
        /// 变量名称
        /// </summary>
        [Required]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 变量类型
        /// </summary>
        [Required]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 变量描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 默认值
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// 变量作用域
        /// </summary>
        public VariableScope Scope { get; set; } = VariableScope.Global;
    }

    /// <summary>
    /// 选中的Agent信息
    /// </summary>
    public class SelectedAgent
    {
        /// <summary>
        /// Agent类型名称
        /// </summary>
        [Required]
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// 选择理由
        /// </summary>
        public string SelectionReason { get; set; } = string.Empty;

        /// <summary>
        /// 在工作流中的角色
        /// </summary>
        public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// 循环配置
    /// </summary>
    public class LoopConfiguration
    {
        /// <summary>
        /// 循环类型
        /// </summary>
        public LoopType Type { get; set; } = LoopType.Fixed;

        /// <summary>
        /// 最大迭代次数
        /// </summary>
        public int MaxIterations { get; set; } = 10;

        /// <summary>
        /// 循环条件表达式
        /// </summary>
        public string? ConditionExpression { get; set; }

        /// <summary>
        /// 迭代变量名
        /// </summary>
        public string? IteratorVariable { get; set; }
    }

    /// <summary>
    /// 节点UI位置
    /// </summary>
    public class NodePosition
    {
        /// <summary>
        /// X坐标
        /// </summary>
        public double X { get; set; } = 0;

        /// <summary>
        /// Y坐标
        /// </summary>
        public double Y { get; set; } = 0;
    }
} 