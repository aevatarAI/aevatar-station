using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Domain.WorkflowOrchestration;

namespace Aevatar.Application.Contracts.WorkflowOrchestration
{
    /// <summary>
    /// Agent扫描服务接口 - 负责自动发现和索引标记了AgentDescriptionAttribute的Agent类
    /// </summary>
    public interface IAgentScannerService
    {
        /// <summary>
        /// 扫描所有可用的Agent并构建索引信息
        /// </summary>
        /// <param name="assembliesToScan">要扫描的程序集列表，如果为空则扫描所有已加载的程序集</param>
        /// <returns>发现的Agent信息列表</returns>
        Task<List<AgentIndexInfo>> ScanAgentsAsync(IEnumerable<Assembly> assembliesToScan = null);

        /// <summary>
        /// 从指定类型提取Agent信息
        /// </summary>
        /// <param name="agentType">Agent类型</param>
        /// <returns>Agent索引信息，如果不是有效的Agent则返回null</returns>
        AgentIndexInfo ExtractAgentInfo(Type agentType);

        /// <summary>
        /// 验证类型是否为有效的Agent
        /// </summary>
        /// <param name="type">要验证的类型</param>
        /// <returns>是否为有效的Agent类型</returns>
        bool IsValidAgent(Type type);

        /// <summary>
        /// 获取扫描统计信息
        /// </summary>
        /// <returns>扫描统计数据</returns>
        AgentScanStatistics GetScanStatistics();
    }

    /// <summary>
    /// Agent扫描统计信息
    /// </summary>
    public class AgentScanStatistics
    {
        public DateTime LastScanTime { get; set; }
        public int TotalAgentsFound { get; set; }
        public int AssembliesScanned { get; set; }
        public int TypesScanned { get; set; }
        public TimeSpan ScanDuration { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
        public Dictionary<string, int> AgentsByCategory { get; set; } = new Dictionary<string, int>();
        public Dictionary<int, int> AgentsByComplexity { get; set; } = new Dictionary<int, int>();
    }
} 