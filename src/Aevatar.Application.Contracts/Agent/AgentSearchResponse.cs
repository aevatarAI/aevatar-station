using System.Collections.Generic;

namespace Aevatar.Agent;

/// <summary>
/// Agent搜索响应DTO
/// </summary>
public class AgentSearchResponse
{
    /// <summary>
    /// Agent列表
    /// </summary>
    public List<AgentItemDto> Agents { get; set; } = new List<AgentItemDto>();
    
    /// <summary>
    /// 当前结果中的可用类型
    /// </summary>
    public List<string> AvailableTypes { get; set; } = new List<string>();
    
    /// <summary>
    /// 每种类型的数量统计
    /// </summary>
    public Dictionary<string, int> TypeCounts { get; set; } = new Dictionary<string, int>();
    
    /// <summary>
    /// 总数
    /// </summary>
    public int Total { get; set; }
    
    /// <summary>
    /// 页索引
    /// </summary>
    public int PageIndex { get; set; }
    
    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// 是否有更多数据
    /// </summary>
    public bool HasMore { get; set; }
} 