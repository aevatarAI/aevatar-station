# Agent查询过滤功能增强设计文档

## 1. 概述

### 1.1 项目背景
当前Aevatar平台的Agent查询接口功能相对基础，仅支持基本的分页查询，缺乏灵活的过滤和搜索能力。随着平台Agent数量的增长，用户需要更强大的查询和管理能力。

### 1.2 设计目标
- **兼容性优先**: 确保现有API和数据完全向后兼容
- **扩展性设计**: 支持未来功能扩展而不破坏现有架构
- **性能优化**: 大规模数据下的高效查询
- **用户体验**: 直观易用的过滤和搜索界面

### 1.3 核心挑战
1. **旧数据兼容性**: 确保现有Agent数据无缝兼容新的过滤功能
2. **API设计**: 在扩展功能的同时保持接口稳定性
3. **性能要求**: 大量Agent数据的实时查询响应
4. **数据规范性**: 避免业务随意定义类型造成数据混乱

## 2. 需求分析

### 2.1 当前状态评估

**现有查询能力:**
- `GetAllAgentInstance(pageIndex, pageSize)` - 基础分页
- 仅按当前用户ID过滤: `userId.keyword:currentUserId`
- 基于Elasticsearch + Lucene查询引擎

**存储结构(CreatorGAgentState):**
```csharp
[GenerateSerializer]
public class CreatorGAgentState : GroupAgentState
{
    [Id(0)] public Guid Id { get; set; }
    [Id(1)] public Guid UserId { get; set; }
    [Id(3)] public string AgentType { get; set; }  // 核心过滤字段
    [Id(4)] public string Name { get; set; }       // 模糊搜索字段
    [Id(5)] public string Properties { get; set; }
    [Id(6)] public GrainId BusinessAgentGrainId { get; set; }
    [Id(7)] public List<EventDescription> EventInfoList { get; set; }
    [Id(8)] public DateTime CreateTime { get; set; } // 排序字段
    [Id(9)] public string FormattedBusinessAgentGrainId { get; set; }
}
```

### 2.2 用户需求

**核心过滤需求:**
- 按Agent类型筛选（支持多选）
- 按名称模糊搜索
- 按创建时间排序
- 分页查询

**AgentType分类规则:**
- **系统GAgent类型**: 预定义的技术类型 (GroupGAgent, CodeGAgent, 等)
- **业务Agent类型**: 除系统类型外的所有其他Agent类型
- **多类型过滤**: 支持同时选择多种类型进行过滤

## 3. AgentType分类设计

### 3.1 类型分类策略

**系统GAgent类型 (来自配置):**
```csharp
// 从 appsettings.HttpApi.Host.Shared.json 获取
public static readonly string[] SystemAgentTypes = {
    "GroupGAgent",
    "PublishingGAgent", 
    "SubscriptionGAgent",
    "AtomicGAgent",
    "CombinationGAgent",
    "CodeGAgent",
    "TenantPluginCodeGAgent",
    "PluginCodeStorageGAgent"
};
```

**业务Agent识别:**
```csharp
public static bool IsBusinessAgent(string agentType)
{
    return !SystemAgentTypes.Contains(agentType);
}

public static List<string> GetBusinessAgentTypes(List<string> allAgentTypes)
{
    return allAgentTypes.Where(type => !SystemAgentTypes.Contains(type)).ToList();
}
```

### 3.2 过滤逻辑设计

**多类型过滤查询构建:**
```csharp
public string BuildAgentTypeFilter(List<string> agentTypes)
{
    if (agentTypes == null || !agentTypes.Any())
        return string.Empty;
        
    var typeQueries = agentTypes.Select(type => $"agentType.keyword:\"{type}\"");
    return $"({string.Join(" OR ", typeQueries)})";
}
```

## 4. 技术架构设计

### 4.1 API设计

**过滤请求DTO:**
```csharp
public class AgentFilterRequest
{
    /// <summary>
    /// Agent类型列表过滤（支持多选）
    /// 可包含系统类型和业务类型
    /// </summary>
    public List<string>? AgentTypes { get; set; }
    
    /// <summary>
    /// Agent名称模糊搜索
    /// </summary>
    public string? NameSearch { get; set; }
    
    /// <summary>
    /// 排序字段，格式：字段名:asc/desc
    /// 默认按创建时间倒序: "createTime:desc"
    /// 支持: "createTime:asc", "createTime:desc", "name:asc", "name:desc"
    /// </summary>
    public string? SortBy { get; set; } = "createTime:desc";
    
    /// <summary>
    /// 页码（从0开始）
    /// </summary>
    public int PageIndex { get; set; } = 0;
    
    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; } = 20;
}
```

**响应DTO (保持兼容):**
```csharp
public class AgentInstanceDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string AgentType { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public string BusinessAgentGrainId { get; set; }
    
    // 新增字段 - 可选，保证兼容性
    public DateTime CreateTime { get; set; }
    public bool IsSystemAgent => SystemAgentTypes.Contains(AgentType);
}
```

### 4.2 查询构建器设计

**Lucene查询构建:**
```csharp
public class AgentQueryBuilder
{
    private readonly StringBuilder _queryParts = new();
    private readonly List<string> _sortFields = new();
    
    public AgentQueryBuilder ByUserId(Guid userId)
    {
        AddQuery($"userId.keyword:{userId}");
        return this;
    }
    
    public AgentQueryBuilder ByAgentTypes(List<string> agentTypes)
    {
        if (agentTypes?.Any() == true)
        {
            var typeQueries = agentTypes.Select(type => $"agentType.keyword:\"{type}\"");
            AddQuery($"({string.Join(" OR ", typeQueries)})");
        }
        return this;
    }
    
    public AgentQueryBuilder ByNameSearch(string namePattern)
    {
        if (!string.IsNullOrWhiteSpace(namePattern))
        {
            AddQuery($"name:*{namePattern}*");
        }
        return this;
    }
    
    public AgentQueryBuilder OrderBy(string sortBy)
    {
        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            _sortFields.Add(sortBy);
        }
        return this;
    }
    
    public LuceneQueryDto Build()
    {
        return new LuceneQueryDto
        {
            QueryString = _queryParts.Length > 0 ? _queryParts.ToString() : "*",
            StateName = nameof(CreatorGAgentState),
            SortFields = _sortFields
        };
    }
    
    private void AddQuery(string query)
    {
        if (_queryParts.Length > 0)
            _queryParts.Append(" AND ");
        _queryParts.Append(query);
    }
}
```

## 5. 分阶段实现规划

### 5.1 第一阶段 - 基础过滤功能 (1-2周)

**目标**: 基于现有字段实现核心过滤功能

**功能范围:**
- Agent类型多选过滤
- 名称模糊搜索  
- 创建时间排序
- 保持100%向后兼容

**技术实现:**
- 扩展现有Lucene查询构建逻辑
- 新增AgentFilterRequest DTO
- 更新AgentService和AgentController
- 系统类型配置管理

**交付物:**
- AgentFilterRequest DTO
- 扩展的AgentService.GetAgentInstancesWithFilter方法
- 更新的AgentController接口
- 单元测试和集成测试

### 5.2 第二阶段 - 前端集成和优化 (1周)

**目标**: 前端界面集成和用户体验优化

**功能范围:**
- 前端过滤组件
- 类型选择器（区分系统/业务类型）
- 搜索结果高亮
- 历史搜索记录

### 5.3 第三阶段 - 性能优化和监控 (1周)

**目标**: 性能优化和监控完善

**功能范围:**
- 查询性能优化
- 缓存策略
- 监控指标
- 使用统计

## 6. API接口规范

### 6.1 新增接口

**Controller方法:**
```csharp
[HttpPost("agent-list/filter")]
[Authorize]
public async Task<PagedResultDto<AgentInstanceDto>> GetAgentInstancesWithFilter(
    [FromBody] AgentFilterRequest filter)
{
    return await _agentService.GetAgentInstancesWithFilter(filter);
}
```

**Service方法:**
```csharp
public async Task<PagedResultDto<AgentInstanceDto>> GetAgentInstancesWithFilter(
    AgentFilterRequest filter)
{
    var currentUserId = _userAppService.GetCurrentUserId();
    
    var queryBuilder = new AgentQueryBuilder()
        .ByUserId(currentUserId)
        .ByAgentTypes(filter.AgentTypes)
        .ByNameSearch(filter.NameSearch)
        .OrderBy(filter.SortBy ?? "createTime:desc");
    
    var query = queryBuilder.Build();
    query.PageIndex = filter.PageIndex;
    query.PageSize = filter.PageSize;
    
    var response = await _indexingService.QueryWithLuceneAsync(query);
    
    var agents = response.Items.Select(MapToAgentInstanceDto).ToList();
    
    return new PagedResultDto<AgentInstanceDto>(response.TotalCount, agents);
}
```

### 6.2 兼容性保证

**现有接口保持不变:**
```csharp
// 保持现有方法签名和行为完全不变
[HttpGet("agent-list")]
[Authorize]
public async Task<List<AgentInstanceDto>> GetAllAgentInstance(int pageIndex = 0, int pageSize = 20)
{
    return await _agentService.GetAllAgentInstances(pageIndex, pageSize);
}
```

## 7. 性能优化策略

### 7.1 索引优化

**Elasticsearch索引策略:**
- 复合索引: (userId, agentType, createTime)
- 关键词索引: agentType.keyword, name.keyword
- 排序优化: createTime字段优化

### 7.2 查询优化

**性能目标:**
- 95% 查询响应时间 < 200ms
- 支持单用户10,000+ Agent规模
- 并发查询支持

**优化措施:**
- 查询结果缓存 (5分钟TTL)
- 分页游标优化
- 字段选择优化

## 8. 监控和测试

### 8.1 性能监控

**关键指标:**
- 查询响应时间分布
- 过滤条件使用统计
- 缓存命中率
- 错误率监控

### 8.2 测试策略

**测试覆盖:**
- 单元测试: AgentQueryBuilder, 过滤逻辑
- 集成测试: 完整过滤流程
- 性能测试: 大数据量场景
- 兼容性测试: 现有API不受影响

## 9. 风险评估与应对

### 9.1 技术风险

**风险点:**
- ES查询性能下降
- 兼容性破坏

**应对措施:**
- 性能基准测试
- 渐进式发布
- 完整回滚方案

### 9.2 业务风险

**风险点:**
- 用户体验变化
- 数据一致性

**应对措施:**
- A/B测试验证
- 数据校验机制

## 10. 总结

本设计文档提供了简洁实用的Agent查询过滤功能增强方案，聚焦于核心需求：多类型过滤、名称搜索和排序功能。通过保持现有AgentType字段的规范性，既满足了灵活查询的需求，又避免了数据管理的复杂性。

**核心特点:**
- ✅ 基于现有数据结构，零破坏性变更
- ✅ 支持系统/业务Agent类型区分和多选过滤  
- ✅ 简洁的API设计，易于使用和维护
- ✅ 完整的性能优化和监控方案

---

**文档版本**: v2.0  
**创建时间**: 2025-01-29  
**更新时间**: 2025-01-29  
**责任人**: HyperEcho  
**审核状态**: 待审核 