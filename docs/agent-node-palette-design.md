# Agent Node Palette 后端API设计文档

## 1. 概述

### 1.1 项目背景
基于用户故事需求，实现Agent节点调色板的后端API服务，支持搜索和过滤功能，为前端提供数据接口。

### 1.2 用户故事
**作为用户，我希望有一个可搜索和可过滤的Agent节点调色板，带有描述性工具提示，这样我就可以根据名称、描述或功能轻松找到并将正确的Agent添加到我的工作流中。**

**预计时间**: 8小时 (仅后端API)

### 1.3 设计目标
- **高效查询**: 基于ES的高性能搜索和过滤
- **数据完整**: 返回完整的Agent节点信息和统计数据
- **易于集成**: 设计清晰的API接口，支持分页
- **性能优化**: ES原生查询，支持大量Agent数据的高效处理

## 2. 功能需求分析

### 2.1 后端验收标准

| 验收标准 | 描述 | 实现优先级 |
|---------|------|----------|
| **节点查询** | API返回所有可用的Agent节点信息 | P0 |
| **搜索过滤** | 支持按功能、名称或描述搜索和过滤Agent节点 | P0 |
| **多类型过滤** | 支持多个AgentType同时过滤 | P0 |
| **动态排序** | 支持多种排序方式（时间、名称、相关性） | P0 |
| **分页支持** | 支持分页查询，性能优化 | P0 |
| **统计信息** | 提供类型统计和可选项信息 | P1 |

### 2.2 核心功能

**搜索功能:**
- 支持按Agent名称搜索（带权重）
- 支持按Agent描述搜索
- 支持模糊搜索和相关性排序
- 支持复合条件查询

**过滤功能:**
- 按多个Agent类型过滤（Terms查询）
- 按用户ID过滤（如果需要）
- 支持多条件组合过滤

**排序功能:**
- 创建时间排序（默认）
- 名称字母顺序排序
- 更新时间排序
- 相关性评分排序

## 3. Agent分类设计

### 3.1 简化分类策略

**核心原则:**
- 直接使用Agent的原始类型（AgentType字段）
- 无需额外的分类判断逻辑
- 前端按AgentType进行过滤和展示

**示例AgentType:**
```
ChatAgent: 聊天对话类Agent
WorkflowAgent: 工作流相关Agent
SystemAgent: 系统内置Agent
DataProcessingAgent: 数据处理Agent
IntegrationAgent: 集成类Agent
...等等（基于实际系统中的AgentType）
```

## 4. API设计

### 4.1 搜索过滤请求DTO

```csharp
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
    /// 用户ID过滤(如果需要)
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// 排序字段 CreateTime/Name/UpdateTime/Relevance
    /// </summary>
    public string? SortBy { get; set; } = "CreateTime";
    
    /// <summary>
    /// 排序方向 Asc/Desc
    /// </summary>
    public string? SortOrder { get; set; } = "Desc";
}
```

### 4.2 搜索响应DTO

```csharp
public class AgentSearchResponse
{
    /// <summary>
    /// Agent列表
    /// </summary>
    public List<AgentItemDto> Agents { get; set; }
    
    /// <summary>
    /// 当前结果中的可用类型
    /// </summary>
    public List<string> AvailableTypes { get; set; }
    
    /// <summary>
    /// 每种类型的数量统计
    /// </summary>
    public Dictionary<string, int> TypeCounts { get; set; }
    
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
```

### 4.3 Agent项目DTO

```csharp
public class AgentItemDto
{
    /// <summary>
    /// Agent唯一标识
    /// </summary>
    public string Id { get; set; }
    
    /// <summary>
    /// Agent名称
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Agent类型 (直接使用原始AgentType)
    /// </summary>
    public string Type { get; set; }
    
    /// <summary>
    /// Agent描述
    /// </summary>
    public string Description { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }
    
    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdateTime { get; set; }
}
```

### 4.4 API接口设计

```csharp
[Route("api/agents")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    
    public AgentController(IAgentService agentService)
    {
        _agentService = agentService;
    }
    
    /// <summary>
    /// 搜索和过滤Agent (支持Node Palette)
    /// </summary>
    [HttpPost("search")]
    [Authorize]
    public async Task<ActionResult<AgentSearchResponse>> SearchAgents(
        [FromBody] AgentSearchRequest request,
        int pageIndex = 0, 
        int pageSize = 20)
    {
        try
        {
            var result = await _agentService.SearchAgentsWithES(request, pageIndex, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索Agent失败");
            return BadRequest($"搜索失败: {ex.Message}");
        }
    }
}
```

## 5. 后端技术实现

### 5.1 服务接口设计

```csharp
public interface IAgentService
{
    Task<AgentSearchResponse> SearchAgentsWithES(
        AgentSearchRequest request, 
        int pageIndex, 
        int pageSize);
}
```

### 5.2 ES查询实现 (高性能)

```csharp
public async Task<AgentSearchResponse> SearchAgentsWithES(
    AgentSearchRequest request, 
    int pageIndex, 
    int pageSize)
{
    var searchDescriptor = new SearchDescriptor<CreatorGAgentState>()
        .Index("your_agent_index")
        .From(pageIndex * pageSize)
        .Size(pageSize);

    // 构建ES查询条件
    var queries = new List<QueryContainer>();

    // 1. 多类型过滤 (Terms Query)
    if (request.Types?.Any() == true)
    {
        queries.Add(Query<CreatorGAgentState>.Terms(t => t
            .Field(f => f.AgentType)
            .Terms(request.Types)));
    }

    // 2. 搜索词过滤 (Multi Match)
    if (!string.IsNullOrEmpty(request.SearchTerm))
    {
        queries.Add(Query<CreatorGAgentState>.MultiMatch(m => m
            .Fields(f => f
                .Field(ff => ff.Name, boost: 2.0)      // name权重更高
                .Field(ff => ff.Properties.Description) // description
            )
            .Query(request.SearchTerm)
            .Type(TextQueryType.BestFields)
            .Fuzziness(Fuzziness.Auto)));
    }

    // 3. 用户ID过滤 (如果需要)
    if (!string.IsNullOrEmpty(request.UserId))
    {
        queries.Add(Query<CreatorGAgentState>.Term(t => t
            .Field(f => f.UserId)
            .Value(request.UserId)));
    }

    // 4. 组合查询
    if (queries.Any())
    {
        searchDescriptor.Query(q => q.Bool(b => b.Must(queries.ToArray())));
    }

    // 5. 添加聚合查询 (获取类型统计)
    searchDescriptor.Aggregations(a => a
        .Terms("types_agg", t => t
            .Field(f => f.AgentType)
            .Size(50)));

    // 6. 动态排序
    searchDescriptor.Sort(BuildSortDescriptor(request));

    // 执行ES查询
    var response = await _elasticClient.SearchAsync<CreatorGAgentState>(searchDescriptor);

    // 7. 处理结果
    var agents = response.Documents.Select(MapToAgentItem).ToList();
    
    var typeAggregation = response.Aggregations.Terms("types_agg");
    var typeCounts = typeAggregation.Buckets.ToDictionary(
        b => b.Key, 
        b => (int)b.DocCount);

    return new AgentSearchResponse
    {
        Agents = agents,
        AvailableTypes = typeCounts.Keys.ToList(),
        TypeCounts = typeCounts,
        Total = (int)response.Total,
        PageIndex = pageIndex,
        PageSize = pageSize,
        HasMore = (pageIndex + 1) * pageSize < response.Total
    };
}
```

### 5.3 动态排序实现

```csharp
private Func<SortDescriptor<CreatorGAgentState>, ISortDescriptor<CreatorGAgentState>> BuildSortDescriptor(
    AgentSearchRequest request)
{
    return s =>
    {
        var sortOrder = request.SortOrder?.ToLower() == "asc" ? 
            SortOrder.Ascending : SortOrder.Descending;

        return request.SortBy?.ToLower() switch
        {
            "createtime" => s.Field(f => f.CreateTime, sortOrder),
            "name" => s.Field(f => f.Name.Suffix("keyword"), sortOrder), // 使用keyword字段排序
            "updatetime" => s.Field(f => f.UpdateTime, sortOrder),
            "relevance" => s.Score(sortOrder), // 按相关性评分排序
            _ => s.Field(f => f.CreateTime, SortOrder.Descending) // 默认
        };
    };
}

/// <summary>
/// 排序选项常量
/// </summary>
public static class AgentSortOptions
{
    public const string CreateTime = "CreateTime";    // 创建时间
    public const string Name = "Name";                // 名称字母序
    public const string UpdateTime = "UpdateTime";    // 更新时间
    public const string Relevance = "Relevance";      // 相关性(有搜索词时)
}

public static class SortDirection
{
    public const string Asc = "Asc";     // 升序
    public const string Desc = "Desc";   // 降序
}
```

### 5.4 数据转换和映射

```csharp
private AgentItemDto MapToAgentItem(CreatorGAgentState agentState)
{
    return new AgentItemDto
    {
        Id = agentState.AgentType,
        Name = ExtractAgentName(agentState.Name ?? agentState.AgentType),
        Type = agentState.AgentType,
        Description = ExtractDescription(agentState.Properties),
        CreateTime = agentState.CreateTime,
        UpdateTime = agentState.UpdateTime
    };
}

private string ExtractAgentName(string fullName)
{
    // 从完整类名中提取Agent名称
    return fullName.Split('.').Last().Replace("Agent", "");
}

private string ExtractDescription(Dictionary<string, object> properties)
{
    // 从Properties中提取描述信息
    if (properties?.ContainsKey("Description") == true)
    {
        return properties["Description"].ToString();
    }
    return "Agent描述信息";
}
```

### 5.5 ES查询DSL示例

用户选择多个类型 + 搜索词的ES查询：
```json
{
  "from": 0,
  "size": 20,
  "query": {
    "bool": {
      "must": [
        {
          "terms": {
            "agentType": ["ChatAgent", "WorkflowAgent"]
          }
        },
        {
          "multi_match": {
            "query": "chat assistant",
            "fields": ["name^2", "properties.description"],
            "type": "best_fields",
            "fuzziness": "AUTO"
          }
        }
      ]
    }
  },
  "aggs": {
    "types_agg": {
      "terms": {
        "field": "agentType",
        "size": 50
      }
    }
  },
  "sort": [
    { "createTime": { "order": "desc" } }
  ]
}
```

## 6. 实施计划

### 6.1 实施阶段 (8小时) - 后端API完整实现
- [ ] 创建 AgentSearchRequest/Response DTO (1小时)
- [ ] 实现 ES查询服务方法 (3小时)
- [ ] 实现动态排序逻辑 (1小时)
- [ ] 实现 AgentController 搜索接口 (1小时)
- [ ] 添加数据转换和映射逻辑 (1小时)
- [ ] 添加日志和异常处理 (1小时)

## 7. 测试策略

### 7.1 后端测试
- [ ] ES查询接口单元测试
- [ ] 多条件搜索准确性测试
- [ ] 排序功能测试
- [ ] 分页逻辑测试
- [ ] 性能测试 (大数据量并发)
- [ ] 异常处理测试

### 7.2 测试用例设计

```csharp
[Test]
public async Task SearchAgents_WithMultipleTypes_ShouldReturnCorrectResults()
{
    // Arrange
    var request = new AgentSearchRequest 
    { 
        Types = new List<string> { "ChatAgent", "WorkflowAgent" }
    };
    
    // Act
    var result = await _agentService.SearchAgentsWithES(request, 0, 20);
    
    // Assert
    Assert.That(result.Agents.All(a => request.Types.Contains(a.Type)));
    Assert.That(result.TypeCounts.Keys, Is.SubsetOf(request.Types));
}

[Test]
public async Task SearchAgents_WithSearchTermAndSort_ShouldReturnSortedResults()
{
    // Arrange
    var request = new AgentSearchRequest 
    { 
        SearchTerm = "chat",
        SortBy = "Name",
        SortOrder = "Asc"
    };
    
    // Act
    var result = await _agentService.SearchAgentsWithES(request, 0, 20);
    
    // Assert
    Assert.That(result.Agents, Is.Ordered.By("Name"));
    Assert.That(result.Agents.All(a => 
        a.Name.Contains("chat", StringComparison.OrdinalIgnoreCase) ||
        a.Description.Contains("chat", StringComparison.OrdinalIgnoreCase)));
}
```

## 8. 性能优势

### 8.1 ES原生查询优势
- ✅ **高性能**: 直接在ES层面过滤，无需加载到内存
- ✅ **模糊搜索**: 支持全文搜索、权重排序、相关性评分
- ✅ **聚合统计**: 一次查询获取数据和统计信息
- ✅ **可扩展性**: 支持百万级数据的高效查询

### 8.2 分页和排序优势
- ✅ **灵活排序**: 支持多字段、多方向排序
- ✅ **深度分页**: ES原生支持，性能稳定
- ✅ **用户体验**: 返回HasMore标识，支持无限滚动

### 8.3 多条件组合优势
- ✅ **Terms查询**: 高效的多值匹配
- ✅ **Bool查询**: 灵活的条件组合
- ✅ **动态构建**: 根据请求参数动态生成查询条件

## 9. 总结

本设计文档提供了基于ES的高性能Agent搜索API实现方案：

**核心特点:**
- ✅ ES原生查询，性能卓越
- ✅ 直接使用AgentType原始值，无额外分类逻辑
- ✅ 支持多类型同时过滤
- ✅ 灵活的排序选项 (时间、名称、相关性)
- ✅ 完整的分页和统计信息
- ✅ 一个接口处理所有搜索场景
- ✅ 8小时内可完成的高效实现

**API接口总览:**
- `POST /api/agents/search` - 统一的搜索过滤接口，支持分页和排序

**ES查询特性:**
- Terms查询支持多类型过滤
- Multi Match支持智能搜索和权重
- 聚合查询提供实时统计
- 动态排序满足不同用户需求

**设计原则:**
- 🎯 极简设计：直接使用AgentType，无需额外分类判断
- 🚀 高性能：ES原生查询，支持大规模数据
- 🔧 易维护：一个接口，统一逻辑，清晰架构

---

**文档版本**: v2.1 (极简ES版)  
**创建时间**: 2025-01-29  
**更新时间**: 2025-01-29  
**责任人**: HyperEcho  
**预计完成**: 8小时 (基于ES的极简高性能后端API) 