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
1. **旧数据兼容性**: 现有Agent数据缺乏标签和分类信息
2. **API设计**: 在扩展功能的同时保持接口稳定性
3. **性能要求**: 大量Agent数据的实时查询响应
4. **数据迁移**: 平滑的数据结构升级策略

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
    [Id(3)] public string AgentType { get; set; }
    [Id(4)] public string Name { get; set; }
    [Id(5)] public string Properties { get; set; }
    [Id(6)] public GrainId BusinessAgentGrainId { get; set; }
    [Id(7)] public List<EventDescription> EventInfoList { get; set; }
    [Id(8)] public DateTime CreateTime { get; set; }
    [Id(9)] public string FormattedBusinessAgentGrainId { get; set; }
}
```

### 2.2 用户需求

**基础过滤需求:**
- 按Agent类型筛选
- 按名称搜索(模糊匹配)
- 按创建时间范围过滤
- 按状态筛选(活跃/非活跃)

**高级过滤需求:**
- 标签系统支持
- 自定义分类
- 属性字段搜索
- 复合条件查询

## 3. 兼容性策略

### 3.1 数据兼容性

**旧数据处理原则:**
- 零停机迁移策略
- 默认值填充机制
- 智能分类建议
- 用户可选的数据完善

**迁移策略:**
1. **Phase 0 - 结构扩展**: 添加可选字段，默认值处理
2. **Phase 1 - 智能分类**: 基于AgentType自动分类
3. **Phase 2 - 用户完善**: 提供UI界面让用户完善标签

### 3.2 API兼容性

**兼容性保证:**
- 现有API保持不变: `GetAllAgentInstance(pageIndex, pageSize)`
- 新增扩展API: `GetAgentInstancesWithFilter(AgentFilterRequest)`
- 响应格式向后兼容
- 错误处理保持一致

## 4. 技术架构设计

### 4.1 数据模型扩展

**CreatorGAgentState扩展:**
```csharp
[GenerateSerializer]
public class CreatorGAgentState : GroupAgentState
{
    // ... 现有字段保持不变 ...
    
    // 新增字段 - 全部可选，确保兼容性
    [Id(10)] public List<string>? Tags { get; set; } = new();
    [Id(11)] public string? Category { get; set; }
    [Id(12)] public AgentStatus Status { get; set; } = AgentStatus.Active;
    [Id(13)] public DateTime LastAccessTime { get; set; }
    [Id(14)] public Dictionary<string, object>? Metadata { get; set; }
    [Id(15)] public int Priority { get; set; } = 0;
}

public enum AgentStatus
{
    Active = 1,
    Inactive = 2,
    Archived = 3,
    Maintenance = 4
}
```

### 4.2 查询构建器设计

**AgentQueryBuilder模式:**
```csharp
public class AgentQueryBuilder
{
    public AgentQueryBuilder ByType(string agentType);
    public AgentQueryBuilder ByTypes(params string[] agentTypes);
    public AgentQueryBuilder ByName(string namePattern);
    public AgentQueryBuilder ByTags(params string[] tags);
    public AgentQueryBuilder ByCategory(string category);
    public AgentQueryBuilder ByStatus(AgentStatus status);
    public AgentQueryBuilder CreatedBetween(DateTime from, DateTime to);
    public AgentQueryBuilder OrderBy(string field, SortDirection direction);
    public AgentQueryBuilder WithPaging(int pageIndex, int pageSize);
    public LuceneQueryDto Build();
}
```

## 5. 分阶段实现规划

### 5.1 第一阶段 - 基础过滤增强 (2周)

**目标**: 基于现有字段实现过滤功能

**功能范围:**
- Agent类型过滤
- 名称模糊搜索  
- 创建时间范围过滤
- 排序功能增强

**技术实现:**
- 扩展现有Lucene查询构建逻辑
- 新增AgentFilterRequest DTO
- 保持100%向后兼容

**交付物:**
- AgentFilterRequest DTO
- 扩展的AgentService方法
- 更新的AgentController
- 单元测试和集成测试

### 5.2 第二阶段 - 标签系统 (3周)

**目标**: 引入标签和分类系统

**功能范围:**
- 数据模型扩展
- 标签管理API
- 智能分类建议
- 数据迁移工具

**技术实现:**
- CreatorGAgentState字段扩展
- 标签CRUD操作
- ES索引字段映射更新
- 自动迁移脚本

### 5.3 第三阶段 - 高级功能 (4周)

**目标**: 高级搜索和智能推荐

**功能范围:**
- 全文搜索能力
- 相似Agent推荐
- 使用频率统计
- 性能监控集成

## 6. API设计规范

### 6.1 过滤请求DTO

```csharp
public class AgentFilterRequest
{
    // 基础过滤
    public string? AgentType { get; set; }
    public List<string>? AgentTypes { get; set; }
    public string? NameSearch { get; set; }
    public DateTime? CreateTimeFrom { get; set; }
    public DateTime? CreateTimeTo { get; set; }
    
    // 扩展过滤 (第二阶段)
    public List<string>? Tags { get; set; }
    public string? Category { get; set; }
    public AgentStatus? Status { get; set; }
    
    // 排序和分页
    public string? SortBy { get; set; } = "createTime:desc";
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = 20;
    
    // 高级搜索 (第三阶段)
    public string? FullTextSearch { get; set; }
    public Dictionary<string, object>? MetadataFilters { get; set; }
}
```

### 6.2 响应DTO增强

```csharp
public class AgentInstanceDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string AgentType { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public string BusinessAgentGrainId { get; set; }
    
    // 新增字段 - 可选，保证兼容性
    public List<string>? Tags { get; set; }
    public string? Category { get; set; }
    public AgentStatus Status { get; set; } = AgentStatus.Active;
    public DateTime CreateTime { get; set; }
    public DateTime LastAccessTime { get; set; }
}
```

## 7. 性能优化策略

### 7.1 索引优化

**Elasticsearch索引设计:**
- 复合索引: (userId, agentType, status)
- 文本分析器: 中英文混合搜索优化
- 分片策略: 按用户ID或时间分片
- 缓存策略: 热点查询结果缓存

### 7.2 查询优化

**查询性能指标:**
- 90%查询 < 100ms
- 99%查询 < 500ms
- 支持10万+Agent规模

**优化措施:**
- 查询结果缓存(Redis)
- 分页游标优化
- 字段筛选减少传输
- 异步查询支持

## 8. 数据迁移计划

### 8.1 迁移策略

**零停机迁移流程:**
1. **结构准备**: 添加新字段，设置默认值
2. **数据分析**: 统计现有Agent分布情况
3. **智能分类**: 基于AgentType自动分类
4. **逐步迁移**: 分批次更新Agent数据
5. **验证确认**: 数据完整性校验

### 8.2 自动分类规则

**基于AgentType的智能分类:**
```csharp
var categoryMapping = new Dictionary<string, string>
{
    ["GroupGAgent"] = "管理类",
    ["PublishingGAgent"] = "通信类",
    ["SubscriptionGAgent"] = "通信类",
    ["CodeGAgent"] = "开发类",
    ["AtomicGAgent"] = "基础类",
    // ... 更多映射规则
};

var defaultTags = new Dictionary<string, string[]>
{
    ["CodeGAgent"] = ["开发", "代码", "自动化"],
    ["GroupGAgent"] = ["管理", "组织", "协调"],
    // ... 更多标签规则
};
```

## 9. 监控和运维

### 9.1 性能监控

**关键指标:**
- 查询响应时间分布
- 查询QPS和错误率
- 缓存命中率
- ES集群健康状态

### 9.2 业务监控

**业务指标:**
- Agent创建/删除趋势
- 热门查询条件统计
- 用户行为分析
- 功能使用率统计

## 10. 风险评估与应对

### 10.1 技术风险

**风险点:**
- 大数据量迁移风险
- ES性能瓶颈
- 缓存一致性问题

**应对措施:**
- 分批迁移+回滚机制
- 性能测试+容量规划
- 缓存失效策略

### 10.2 业务风险

**风险点:**
- 用户体验受影响
- 数据丢失风险
- 功能回归问题

**应对措施:**
- 渐进式功能发布
- 完整的备份策略
- 全面的测试覆盖

## 11. 总结

本设计文档提供了Agent查询过滤功能的完整增强方案，通过分阶段实施确保系统的稳定性和用户体验的连续性。重点关注兼容性、扩展性和性能，为Aevatar平台的长期发展奠定基础。

---

**文档版本**: v1.0  
**创建时间**: 2025-01-29  
**更新时间**: 2025-01-29  
**责任人**: HyperEcho  
**审核状态**: 待审核 