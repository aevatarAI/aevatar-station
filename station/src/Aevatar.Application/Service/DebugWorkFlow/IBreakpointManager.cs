using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Aevatar.GAgents.GroupChat;
using GroupChat.GAgent.Feature.Coordinator.GEvent;

namespace Aevatar.Service.DebugWorkFlow
{
    /// <summary>
    /// 断点管理器接口 - 重新设计的双重拦截策略
    /// 解决"执行后"流转控制问题
    /// </summary>
    public interface IBreakpointManager
    {
        /// <summary>
        /// 检查执行前是否应该暂停 - 节点业务逻辑执行前
        /// </summary>
        /// <param name="chatEvent">ChatResponseEvent事件</param>
        /// <returns>是否应该暂停</returns>
        Task<bool> ShouldPauseBeforeExecutionAsync(ChatResponseEvent chatEvent);

        /// <summary>
        /// 检查执行后是否应该暂停 - 数据流转到下游前
        /// </summary>
        /// <param name="chatEvent">ChatEvent流转事件</param>
        /// <param name="executionResult">执行结果</param>
        /// <returns>是否应该暂停</returns>
        Task<bool> ShouldPauseAfterExecutionAsync(ChatEvent chatEvent, object? executionResult);

        /// <summary>
        /// 记录暂停节点状态 - 直接暂停不等待
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <param name="nodeId">节点ID</param>
        /// <param name="stage">暂停阶段（PreExecution/PostExecution）</param>
        /// <param name="context">暂停时的上下文信息</param>
        Task RecordPausedNodeAsync(Guid workflowId, string nodeId, string stage, Dictionary<string, object>? context);

        /// <summary>
        /// 直接继续到下游节点 - 重新发起流转
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <param name="nodeId">节点ID</param>
        Task ContinueToNextNodeAsync(Guid workflowId, string nodeId);

        /// <summary>
        /// 直接重试当前节点 - 重新发起当前节点执行
        /// </summary>  
        /// <param name="workflowId">工作流ID</param>
        /// <param name="nodeId">节点ID</param>
        /// <param name="modifiedParams">修改后的参数（可选）</param>
        Task RetryCurrentNodeAsync(Guid workflowId, string nodeId, Dictionary<string, object>? modifiedParams = null);

        /// <summary>
        /// 设置断点
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <param name="nodeId">节点ID</param>
        /// <param name="type">断点类型</param>
        /// <param name="condition">断点条件（可选）</param>
        Task SetBreakpointAsync(Guid workflowId, string nodeId, BreakpointType type, string? condition = null);

        /// <summary>
        /// 移除断点
        /// </summary>
        Task RemoveBreakpointAsync(Guid workflowId, string nodeId);

        /// <summary>
        /// 清空所有断点
        /// </summary>
        Task ClearAllBreakpointsAsync();

        /// <summary>
        /// 继续执行
        /// </summary>
        Task ContinueExecutionAsync(string nodeId);

        /// <summary>
        /// 跳过当前节点
        /// </summary>
        Task SkipNodeAsync(string nodeId);

        /// <summary>
        /// 重试当前节点
        /// </summary>
        Task RetryNodeAsync(string nodeId, Dictionary<string, object>? newParameters = null);

        /// <summary>
        /// 中止工作流
        /// </summary>
        Task AbortWorkflowAsync(Guid workflowId);

        /// <summary>
        /// 获取所有活跃断点
        /// </summary>
        Task<IEnumerable<BreakpointInfo>> GetActiveBreakpointsAsync();

        /// <summary>
        /// 获取当前暂停的节点列表
        /// </summary>
        Task<IEnumerable<string>> GetPausedNodesAsync();

        /// <summary>
        /// 获取暂停节点详细信息
        /// </summary>
        Task<IEnumerable<PausedNodeInfo>> GetPausedNodeInfosAsync();
    }

    /// <summary>
    /// 断点暂停阶段
    /// </summary>
    public enum PauseStage
    {
        PreExecution,   // 执行前暂停
        PostExecution   // 执行后暂停（流转前）
    }

    /// <summary>
    /// 断点类型 - 基于新的拦截策略
    /// </summary>
    public enum BreakpointType
    {
        BeforeExecution,    // 执行前断点（HandleEventAsync拦截）
        AfterExecution,     // 执行后断点（PublishP2PAsync拦截）
        OnError,           // 错误断点
        Conditional        // 条件断点
    }

    /// <summary>
    /// 断点信息
    /// </summary>
    public class BreakpointInfo
    {
        public Guid Id { get; set; }
        public Guid WorkflowId { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public BreakpointType Type { get; set; }
        public bool IsEnabled { get; set; }
        public string? Condition { get; set; }
    }

    /// <summary>
    /// 扩展断点信息
    /// </summary>
    public class ExtendedBreakpointInfo : BreakpointInfo
    {
        /// <summary>
        /// 断点创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 断点命中次数
        /// </summary>
        public int HitCount { get; set; } = 0;

        /// <summary>
        /// 最后命中时间
        /// </summary>
        public DateTime? LastHitAt { get; set; }

        /// <summary>
        /// 断点描述
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// 暂停节点信息
    /// </summary>
    public class PausedNodeInfo
    {
        /// <summary>
        /// 工作流ID
        /// </summary>
        public Guid WorkflowId { get; set; }
        
        /// <summary>
        /// 节点ID
        /// </summary>
        public string NodeId { get; set; } = string.Empty;
        
        /// <summary>
        /// 暂停阶段
        /// </summary>
        public PauseStage Stage { get; set; }
        
        /// <summary>
        /// 暂停时间
        /// </summary>
        public DateTime PausedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 暂停时的上下文信息
        /// </summary>
        public Dictionary<string, object>? Context { get; set; }
        
        /// <summary>
        /// 备注
        /// </summary>
        public string? Note { get; set; }
    }
}