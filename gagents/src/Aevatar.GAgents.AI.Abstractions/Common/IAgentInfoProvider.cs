using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.GAgents.AI.Common;

/// <summary>
/// Agent信息提供接口，支持生产环境和测试环境解耦
/// </summary>
public interface IAgentInfoProvider
{
    /// <summary>
    /// 获取所有Agent信息
    /// </summary>
    Task<List<AgentIndexInfo>> GetAllAgentsAsync();

    /// <summary>
    /// 根据ID获取Agent信息
    /// </summary>
    Task<AgentIndexInfo?> GetAgentByIdAsync(string agentId);

    /// <summary>
    /// 根据分类获取Agent信息
    /// </summary>
    Task<List<AgentIndexInfo>> GetAgentsByCategoryAsync(string category);

    /// <summary>
    /// 根据能力获取Agent信息
    /// </summary>
    Task<List<AgentIndexInfo>> GetAgentsByCapabilityAsync(string capability);

    /// <summary>
    /// 获取Agent统计信息
    /// </summary>
    Task<AgentStatistics> GetStatisticsAsync();
}

/// <summary>
/// Agent统计信息
/// </summary>
public class AgentStatistics
{
    public int TotalAgents { get; set; }
    public Dictionary<string, int> CategoriesCount { get; set; } = new();
    public Dictionary<string, int> CapabilitiesCount { get; set; } = new();
    public DateTime LastUpdated { get; set; }
} 