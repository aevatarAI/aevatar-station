using System.Collections.Generic;

namespace Aevatar.Agent;

/// <summary>
/// Agent搜索过滤请求DTO
/// </summary>
public class AgentSearchRequest
{
    /// <summary>
    /// 搜索关键词 (匹配名称、描述)
    /// </summary>
    public string? SearchTerm { get; set; }
    
    /// <summary>
    /// 多类型过滤 ["ChatAgent", "WorkflowAgent"]
    /// </summary>
    public List<string>? Types { get; set; }
    
    /// <summary>
    /// 排序字段 CreateTime/Name/UpdateTime/Relevance
    /// </summary>
    public string? SortBy { get; set; } = "CreateTime";
    
    /// <summary>
    /// 排序方向 Asc/Desc
    /// </summary>
    public string? SortOrder { get; set; } = "Desc";
} 