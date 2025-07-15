using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Domain.WorkflowOrchestration;

namespace Aevatar.Application.Contracts.WorkflowOrchestration
{
    /// <summary>
    /// Agent索引池接口 - 管理所有可用Agent的信息和缓存
    /// </summary>
    public interface IAgentIndexPool
    {
        /// <summary>
        /// 获取所有可用的Agent信息
        /// </summary>
        /// <returns>所有Agent的索引信息</returns>
        Task<IEnumerable<AgentIndexInfo>> GetAllAgentsAsync();

        /// <summary>
        /// 根据ID获取特定Agent信息
        /// </summary>
        /// <param name="agentId">Agent唯一标识</param>
        /// <returns>Agent索引信息，如果不存在则返回null</returns>
        Task<AgentIndexInfo?> GetAgentByIdAsync(string agentId);

        /// <summary>
        /// 搜索Agent（支持关键词、分类过滤）
        /// </summary>
        /// <param name="query">搜索关键词（可选）</param>
        /// <param name="categories">分类过滤（可选）</param>
        /// <param name="limit">返回结果数量限制</param>
        /// <returns>匹配的Agent列表</returns>
        Task<IEnumerable<AgentIndexInfo>> SearchAgentsAsync(string? query = null, string[]? categories = null, int limit = 50);

        /// <summary>
        /// 刷新Agent索引缓存
        /// </summary>
        /// <returns>刷新操作的结果</returns>
        Task<AgentIndexRefreshResult> RefreshIndexAsync();
    }

    /// <summary>
    /// Agent索引刷新结果
    /// </summary>
    public class AgentIndexRefreshResult
    {
        /// <summary>
        /// 是否刷新成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 总扫描的Agent数量
        /// </summary>
        public int TotalScanned { get; set; }

        /// <summary>
        /// 新增的Agent数量
        /// </summary>
        public int NewAgents { get; set; }

        /// <summary>
        /// 更新的Agent数量
        /// </summary>
        public int UpdatedAgents { get; set; }

        /// <summary>
        /// 刷新耗时（毫秒）
        /// </summary>
        public long RefreshDuration { get; set; }

        /// <summary>
        /// 错误信息（如果有）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 最后刷新时间
        /// </summary>
        public DateTime RefreshTime { get; set; } = DateTime.UtcNow;
    }
} 