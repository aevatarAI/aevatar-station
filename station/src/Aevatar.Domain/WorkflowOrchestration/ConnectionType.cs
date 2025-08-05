namespace Aevatar.Domain.WorkflowOrchestration
{
    /// <summary>
    /// 工作流连接类型 - 简化版本
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// 顺序连接 - 默认的数据流或顺序执行
        /// </summary>
        Sequential = 1,

        /// <summary>
        /// 条件连接 - 基于条件的分支
        /// </summary>
        Conditional = 2,

        /// <summary>
        /// 并行连接 - 并行执行分支
        /// </summary>
        Parallel = 3
    }
} 