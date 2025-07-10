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

### 4.3 Agent项目DTO (对齐现有结构)

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
    public string AgentType { get; set; }
    
    /// <summary>
    /// Agent属性信息
    /// </summary>
    public Dictionary<string, object>? Properties { get; set; }
    
    /// <summary>
    /// 业务Agent Grain ID
    /// </summary>
    public string? BusinessAgentGrainId { get; set; }
    
    /// <summary>
    /// Agent描述 (从Properties中提取)
    /// </summary>
    public string? Description { get; set; }
}
```

### 4.4 API接口设计 (对齐现有模式)

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
            var result = await _agentService.SearchAgentsWithLucene(request, pageIndex, pageSize);
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
    Task<AgentSearchResponse> SearchAgentsWithLucene(
        AgentSearchRequest request, 
        int pageIndex, 
        int pageSize);
}
```

### 5.2 基于现有架构的Lucene查询实现

```csharp
public class AgentService : IAgentService
{
    private readonly IIndexingService _indexingService;
    private readonly IUserAppService _userAppService;
    private readonly ILogger<AgentService> _logger;
    
    public AgentService(
        IIndexingService indexingService,
        IUserAppService userAppService,
        ILogger<AgentService> logger)
    {
        _indexingService = indexingService;
        _userAppService = userAppService;
        _logger = logger;
    }
    
    public async Task<AgentSearchResponse> SearchAgentsWithLucene(
        AgentSearchRequest request, 
        int pageIndex, 
        int pageSize)
    {
        _logger.LogInformation("开始搜索Agent，搜索词: {SearchTerm}", request.SearchTerm);
        
        // 1. 获取当前用户ID (对齐现有逻辑)
        var currentUserId = _userAppService.GetCurrentUserId();
        
        // 2. 构建Lucene查询字符串
        var queryString = BuildLuceneQuery(request, currentUserId);
        
        // 3. 执行查询 (使用现有的IndexingService)
        var response = await _indexingService.QueryWithLuceneAsync(new LuceneQueryDto()
        {
            QueryString = queryString,
            StateName = nameof(CreatorGAgentState),
            PageSize = pageSize,
            PageIndex = pageIndex
        });
        
        if (response.TotalCount == 0)
        {
            return new AgentSearchResponse
            {
                Agents = new List<AgentItemDto>(),
                AvailableTypes = new List<string>(),
                TypeCounts = new Dictionary<string, int>(),
                Total = 0,
                PageIndex = pageIndex,
                PageSize = pageSize,
                HasMore = false
            };
        }
        
        // 4. 转换数据 (对齐现有模式)
        var agents = response.Items.Select(MapToAgentItem).ToList();
        
        // 5. 应用客户端排序 (如果需要)
        agents = ApplySorting(agents, request.SortBy, request.SortOrder);
        
        // 6. 统计类型信息
        var typeCounts = agents.GroupBy(a => a.AgentType)
                              .ToDictionary(g => g.Key, g => g.Count());
        
        _logger.LogInformation("搜索完成，返回 {Count} 个Agent", agents.Count);
        
        return new AgentSearchResponse
        {
            Agents = agents,
            AvailableTypes = typeCounts.Keys.ToList(),
            TypeCounts = typeCounts,
            Total = (int)response.TotalCount,
            PageIndex = pageIndex,
            PageSize = pageSize,
            HasMore = (pageIndex + 1) * pageSize < response.TotalCount
        };
    }
}
```

### 5.3 Lucene查询字符串构建

```csharp
private string BuildLuceneQuery(AgentSearchRequest request, string currentUserId)
{
    var queryParts = new List<string>();
    
    // 1. 用户ID过滤 (必须条件，对齐现有逻辑)
    queryParts.Add($"userId.keyword:{currentUserId}");
    
    // 2. 类型过滤 (多选支持)
    if (request.Types?.Any() == true)
    {
        var typeQuery = string.Join(" OR ", 
            request.Types.Select(type => $"agentType.keyword:\"{type}\""));
        queryParts.Add($"({typeQuery})");
    }
    
    // 3. 搜索词过滤 (名称和属性描述)
    if (!string.IsNullOrEmpty(request.SearchTerm))
    {
        var searchTerm = EscapeLuceneString(request.SearchTerm);
        var nameQuery = $"name:*{searchTerm}*";
        var descQuery = $"properties.description:*{searchTerm}*";
        queryParts.Add($"({nameQuery} OR {descQuery})");
    }
    
    // 组合所有条件 (AND逻辑)
    return string.Join(" AND ", queryParts);
}

private string EscapeLuceneString(string input)
{
    // 转义Lucene特殊字符
    if (string.IsNullOrEmpty(input)) return input;
    
    var specialChars = new[] { '+', '-', '!', '(', ')', '{', '}', '[', ']', '^', '"', '~', '*', '?', ':', '\\' };
    foreach (var c in specialChars)
    {
        input = input.Replace(c.ToString(), "\\" + c);
    }
    return input;
}
```

### 5.4 数据转换和映射 (对齐现有模式)

```csharp
private AgentItemDto MapToAgentItem(Dictionary<string, object> state)
{
    // 对齐现有的数据转换逻辑
    var properties = state["properties"] == null
        ? null
        : JsonConvert.DeserializeObject<Dictionary<string, object>>((string)state["properties"]);
    
    var description = ExtractDescription(properties);
    
    return new AgentItemDto
    {
        Id = (string)state["id"],
        Name = (string)state["name"],
        AgentType = (string)state["agentType"],
        Properties = properties,
        BusinessAgentGrainId = state.TryGetValue("formattedBusinessAgentGrainId", out var value) 
            ? (string)value 
            : null,
        Description = description
    };
}

private string? ExtractDescription(Dictionary<string, object>? properties)
{
    // 从Properties中提取描述信息
    if (properties?.ContainsKey("description") == true)
    {
        return properties["description"]?.ToString();
    }
    if (properties?.ContainsKey("Description") == true)
    {
        return properties["Description"]?.ToString();
    }
    return null;
}
```

### 5.5 客户端排序实现

```csharp
private List<AgentItemDto> ApplySorting(List<AgentItemDto> agents, string? sortBy, string? sortOrder)
{
    if (string.IsNullOrEmpty(sortBy)) return agents;
    
    var isDescending = sortOrder?.ToLower() == "desc";
    
    return sortBy.ToLower() switch
    {
        "name" => isDescending 
            ? agents.OrderByDescending(a => a.Name).ToList()
            : agents.OrderBy(a => a.Name).ToList(),
        "agenttype" => isDescending
            ? agents.OrderByDescending(a => a.AgentType).ToList()
            : agents.OrderBy(a => a.AgentType).ToList(),
        // CreateTime/UpdateTime需要从Properties中提取
        "createtime" => ApplyDateSorting(agents, "createTime", isDescending),
        "updatetime" => ApplyDateSorting(agents, "updateTime", isDescending),
        _ => agents // 默认不排序，保持Lucene查询结果顺序
    };
}

private List<AgentItemDto> ApplyDateSorting(List<AgentItemDto> agents, string dateField, bool isDescending)
{
    var sorted = agents.Select(a => new 
    {
        Agent = a,
        Date = ExtractDateFromProperties(a.Properties, dateField)
    })
    .OrderBy(x => isDescending ? -x.Date.Ticks : x.Date.Ticks)
    .Select(x => x.Agent)
    .ToList();
    
    return sorted;
}

private DateTime ExtractDateFromProperties(Dictionary<string, object>? properties, string field)
{
    if (properties?.ContainsKey(field) == true)
    {
        if (DateTime.TryParse(properties[field]?.ToString(), out var date))
        {
            return date;
        }
    }
    return DateTime.MinValue; // 默认值
}
```

### 5.6 Lucene查询示例

用户选择多个类型 + 搜索词的Lucene查询：
```
userId.keyword:user123 AND (agentType.keyword:"ChatAgent" OR agentType.keyword:"WorkflowAgent") AND (name:*chat* OR properties.description:*chat*)
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
    var result = await _agentService.SearchAgentsWithLucene(request, 0, 20);
    
    // Assert
    Assert.That(result.Agents.All(a => request.Types.Contains(a.AgentType)));
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
    var result = await _agentService.SearchAgentsWithLucene(request, 0, 20);
    
    // Assert
    Assert.That(result.Agents, Is.Ordered.By("Name"));
    Assert.That(result.Agents.All(a => 
        a.Name.Contains("chat", StringComparison.OrdinalIgnoreCase) ||
        a.Description.Contains("chat", StringComparison.OrdinalIgnoreCase)));
}
```

## 8. 性能优势

### 8.1 基于现有架构的优势
- ✅ **架构对齐**: 完全复用现有的IIndexingService和IUserAppService
- ✅ **Lucene查询**: 原生Lucene语法支持复杂查询条件
- ✅ **用户隔离**: 自动应用用户ID过滤，安全可靠
- ✅ **分页支持**: 复用现有分页逻辑，性能稳定

### 8.2 查询性能优势
- ✅ **索引查询**: 基于Lucene索引的高效查询
- ✅ **复合条件**: 支持AND/OR逻辑的复杂条件组合
- ✅ **模糊搜索**: 通配符搜索支持名称和描述过滤
- ✅ **类型过滤**: 高效的多值Terms查询

### 8.3 数据处理优势
- ✅ **客户端排序**: 灵活的多字段排序支持
- ✅ **实时统计**: 内存中统计类型分布
- ✅ **数据转换**: 对齐现有DTO结构，无缝集成
- ✅ **属性提取**: 智能提取Properties中的描述信息

## 9. 总结

本设计文档提供了基于现有架构的Agent搜索API实现方案：

**核心特点:**
- ✅ 完全对齐现有架构 (IIndexingService + IUserAppService)
- ✅ Lucene原生查询，性能可靠
- ✅ 自动用户隔离，安全性保障
- ✅ 直接使用AgentType原始值，无额外分类逻辑
- ✅ 支持多类型同时过滤和搜索词过滤
- ✅ 灵活的客户端排序选项
- ✅ 完整的分页和统计信息
- ✅ 复用现有数据结构和转换逻辑
- ✅ 8小时内可完成的高效实现

**API接口总览:**
- `POST /api/agents/search` - 统一的搜索过滤接口，支持分页和排序

**Lucene查询特性:**
- 用户ID自动过滤 (userId.keyword)
- 多类型OR查询 (agentType.keyword)
- 名称和描述模糊搜索 (name:*term* OR properties.description:*term*)
- 复合条件AND组合
- 特殊字符自动转义

**设计原则:**
- 🎯 架构对齐：完全复用现有服务和接口
- 🚀 性能优化：Lucene索引查询 + 客户端排序
- 🔧 易维护：一个接口，统一逻辑，清晰架构
- 🛡️ 安全性：自动用户隔离，权限控制

---

**文档版本**: v2.2 (现有架构对齐版)  
**创建时间**: 2025-01-29  
**更新时间**: 2025-01-29  
**责任人**: HyperEcho  
**预计完成**: 8小时 (基于现有架构的Lucene查询实现) 