namespace Aevatar.Domain.WorkflowOrchestration
{
    /// <summary>
    /// 工作流节点类型
    /// </summary>
    public enum WorkflowNodeType
    {
        /// <summary>
        /// 开始节点
        /// </summary>
        Start = 1,

        /// <summary>
        /// 结束节点
        /// </summary>
        End = 2,

        /// <summary>
        /// Agent执行节点
        /// </summary>
        Agent = 3,

        /// <summary>
        /// 条件判断节点
        /// </summary>
        Condition = 4,

        /// <summary>
        /// 循环控制节点
        /// </summary>
        Loop = 5,

        /// <summary>
        /// 并行分支节点
        /// </summary>
        Parallel = 6,

        /// <summary>
        /// 合并汇总节点
        /// </summary>
        Merge = 7,

        /// <summary>
        /// 数据转换节点
        /// </summary>
        Transform = 8,

        /// <summary>
        /// 延迟等待节点
        /// </summary>
        Delay = 9,

        /// <summary>
        /// 用户输入节点
        /// </summary>
        UserInput = 10
    }

    /// <summary>
    /// 连接类型
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// 顺序执行
        /// </summary>
        Sequential = 1,

        /// <summary>
        /// 条件分支
        /// </summary>
        Conditional = 2,

        /// <summary>
        /// 并行分支
        /// </summary>
        Parallel = 3,

        /// <summary>
        /// 合并连接
        /// </summary>
        Merge = 4,

        /// <summary>
        /// 循环回退
        /// </summary>
        LoopBack = 5,

        /// <summary>
        /// 异常处理
        /// </summary>
        ErrorHandling = 6
    }

    /// <summary>
    /// 工作流复杂度级别
    /// </summary>
    public enum WorkflowComplexity
    {
        /// <summary>
        /// 简单：1-3个节点，线性流程
        /// </summary>
        Simple = 1,

        /// <summary>
        /// 中等：4-8个节点，包含分支或并行
        /// </summary>
        Medium = 2,

        /// <summary>
        /// 复杂：9+个节点，包含循环、复杂数据流
        /// </summary>
        Complex = 3
    }

    /// <summary>
    /// 变量作用域
    /// </summary>
    public enum VariableScope
    {
        /// <summary>
        /// 全局作用域
        /// </summary>
        Global = 1,

        /// <summary>
        /// 节点作用域
        /// </summary>
        Node = 2,

        /// <summary>
        /// 分支作用域
        /// </summary>
        Branch = 3,

        /// <summary>
        /// 循环作用域
        /// </summary>
        Loop = 4
    }

    /// <summary>
    /// 循环类型
    /// </summary>
    public enum LoopType
    {
        /// <summary>
        /// 固定次数循环
        /// </summary>
        Fixed = 1,

        /// <summary>
        /// 条件循环
        /// </summary>
        Conditional = 2,

        /// <summary>
        /// 遍历循环
        /// </summary>
        Foreach = 3,

        /// <summary>
        /// 无限循环（直到手动停止）
        /// </summary>
        Infinite = 4
    }

    /// <summary>
    /// Agent执行状态
    /// </summary>
    public enum AgentExecutionStatus
    {
        /// <summary>
        /// 待执行
        /// </summary>
        Pending = 1,

        /// <summary>
        /// 执行中
        /// </summary>
        Running = 2,

        /// <summary>
        /// 执行成功
        /// </summary>
        Completed = 3,

        /// <summary>
        /// 执行失败
        /// </summary>
        Failed = 4,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 5,

        /// <summary>
        /// 超时
        /// </summary>
        Timeout = 6
    }

    /// <summary>
    /// 工作流执行状态
    /// </summary>
    public enum WorkflowExecutionStatus
    {
        /// <summary>
        /// 已创建
        /// </summary>
        Created = 1,

        /// <summary>
        /// 执行中
        /// </summary>
        Running = 2,

        /// <summary>
        /// 已暂停
        /// </summary>
        Paused = 3,

        /// <summary>
        /// 执行完成
        /// </summary>
        Completed = 4,

        /// <summary>
        /// 执行失败
        /// </summary>
        Failed = 5,

        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 6
    }
} 