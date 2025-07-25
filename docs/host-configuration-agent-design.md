# Host Configuration Agent 设计文档

## 设计目标

创建一个简洁、独立的配置管理Agent，专门用于存储和管理Host的业务配置，基于 `hostId + HostTypeEnum` 作为唯一标识。

## 核心原则

### 1. **简洁性优先**
- 最小化功能集：只提供必要的配置CRUD操作
- 简单的数据结构：避免过度设计
- 直接的API接口：每个方法职责单一且清晰

### 2. **独立性**
- 与现有CodeGAgent完全分离
- 专门用于Host配置管理，不混合其他功能
- 独立的状态和事件管理

### 3. **安全性**
- 仅允许业务配置存储，保护系统配置
- 简单的键值验证机制
- 操作审计支持

## 架构设计

### Agent标识策略

```
Agent Grain Key = $"{hostId}:{hostType}"
```

**示例：**
- Client Host: `"MyApp001:Client"`
- Silo Host: `"MyApp001:Silo"`  
- WebHook Host: `"MyApp001:WebHook"`

### 数据模型

#### HostConfigurationGAgentState
```csharp
[GenerateSerializer]
public class HostConfigurationGAgentState : StateBase
{
    [Id(0)] public string HostId { get; set; }
    [Id(1)] public HostTypeEnum HostType { get; set; }
    [Id(2)] public Dictionary<string, object> BusinessConfiguration { get; set; } = new();
    [Id(3)] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Id(4)] public string UpdatedBy { get; set; } = "System";
}
```

#### 事件模型
```csharp
// 基础事件
public class HostConfigurationGEvent : StateLogEventBase<HostConfigurationGEvent>

// 具体事件（只需要一个）
public class UpdateBusinessConfigurationGEvent : HostConfigurationGEvent
{
    public string HostId { get; set; }
    public HostTypeEnum HostType { get; set; }
    public Dictionary<string, object> BusinessConfiguration { get; set; }
    public string UpdatedBy { get; set; }
}
```

### Agent接口设计

```csharp
public interface IHostConfigurationGAgent : IStateGAgent<HostConfigurationGAgentState>
{
    // 核心配置管理（3个方法足够）
    Task SetBusinessConfigurationAsync(Dictionary<string, object> configuration, string updatedBy = "System");
    Task<Dictionary<string, object>> GetBusinessConfigurationAsync();
    Task ClearBusinessConfigurationAsync(string updatedBy = "System");
    
    // 元数据查询（2个方法）
    Task<DateTime> GetLastUpdatedAsync();
    Task<string> GetLastUpdatedByAsync();
}
```

## 实现特点

### 1. **最小化复杂度**
- **只有1个事件类型**：所有配置更新都通过`UpdateBusinessConfigurationGEvent`
- **只有3个核心方法**：Set、Get、Clear
- **简单的状态结构**：5个字段，直接映射

### 2. **Orleans最佳实践**
- 使用标准的事件源模式
- Grain Key基于业务标识（hostId + hostType）
- 标准的StorageProvider和LogConsistencyProvider配置

### 3. **保护机制**
- 复用现有的`ProtectedKeyConfigurationProvider.GetProtectedKeys()`
- 在Set操作时验证键名，拒绝系统保护键
- 抛出明确的异常信息

## 使用流程

### 1. **Agent获取**
```csharp
// 构建唯一的Grain Key
var grainKey = $"{hostId}:{hostType}";
var configAgent = grainFactory.GetGrain<IHostConfigurationGAgent>(grainKey);
```

### 2. **配置管理**
```csharp
// 设置配置
var config = new Dictionary<string, object>
{
    ["MyApp:ApiUrl"] = "https://api.example.com",
    ["MyApp:Timeout"] = 30,
    ["Features:EnableCache"] = true
};
await configAgent.SetBusinessConfigurationAsync(config, "developer@company.com");

// 获取配置
var currentConfig = await configAgent.GetBusinessConfigurationAsync();

// 清除配置
await configAgent.ClearBusinessConfigurationAsync("admin@company.com");
```

### 3. **与UserController集成**
```csharp
[HttpPost("{hostId}/business-configuration")]
public async Task<ActionResult<ConfigurationOperationResponseDto>> UploadBusinessConfiguration(
    string hostId, [FromBody] BusinessConfigurationDto configuration)
{
    // 为每种HostType分别存储配置
    foreach (HostTypeEnum hostType in Enum.GetValues<HostTypeEnum>())
    {
        var grainKey = $"{hostId}:{hostType}";
        var configAgent = _grainFactory.GetGrain<IHostConfigurationGAgent>(grainKey);
        
        await configAgent.SetBusinessConfigurationAsync(
            configuration.Configuration,
            CurrentUser?.UserName ?? "System");
    }
    
    return Ok(new ConfigurationOperationResponseDto { Success = true });
}
```

## 优势分析

### 1. **简洁性**
- **文件数量**：只需3个文件（State、Agent、Interface）
- **代码行数**：预计总共不超过200行
- **依赖关系**：最小化外部依赖

### 2. **性能**
- **内存效率**：每个Host-Type组合独立存储，避免大对象
- **查询效率**：直接通过Grain Key访问，O(1)时间复杂度
- **更新效率**：只影响特定Host-Type，不影响其他配置

### 3. **可维护性**
- **单一职责**：只管理业务配置，职责清晰
- **标准模式**：遵循Orleans事件源标准模式
- **易于测试**：简单的接口，易于单元测试

### 4. **扩展性**
- **水平扩展**：基于Grain Key的天然分片
- **功能扩展**：如需增加功能，可在现有接口上扩展
- **类型安全**：Dictionary<string, object>支持任意类型配置

## 与现有系统集成

### 1. **Kubernetes配置生成**
```csharp
// 在KubernetesHostManager中集成
public async Task<Dictionary<string, string>> GenerateConfigMapAsync(string hostId, HostTypeEnum hostType)
{
    // 1. 加载基础模板配置
    var baseConfig = LoadTemplateConfiguration(hostType);
    
    // 2. 获取业务配置
    var grainKey = $"{hostId}:{hostType}";
    var configAgent = _grainFactory.GetGrain<IHostConfigurationGAgent>(grainKey);
    var businessConfig = await configAgent.GetBusinessConfigurationAsync();
    
    // 3. 合并配置（业务配置覆盖模板中的非保护键）
    var mergedConfig = MergeConfigurations(baseConfig, businessConfig);
    
    return mergedConfig;
}
```

### 2. **API响应增强**
```csharp
[HttpGet("{hostId}/business-configuration")]
public async Task<ActionResult<HostConfigurationSummaryDto>> GetBusinessConfiguration(string hostId)
{
    var summary = new HostConfigurationSummaryDto
    {
        HostId = hostId,
        Configurations = new Dictionary<HostTypeEnum, Dictionary<string, object>>()
    };
    
    foreach (HostTypeEnum hostType in Enum.GetValues<HostTypeEnum>())
    {
        var grainKey = $"{hostId}:{hostType}";
        var configAgent = _grainFactory.GetGrain<IHostConfigurationGAgent>(grainKey);
        
        summary.Configurations[hostType] = await configAgent.GetBusinessConfigurationAsync();
    }
    
    return Ok(summary);
}
```

## 实现计划

### Phase 1: 核心实现（预计1小时）
1. 创建HostConfigurationGAgentState
2. 创建HostConfigurationGEvent
3. 实现HostConfigurationGAgent
4. 创建接口定义

### Phase 2: 集成测试（预计30分钟）
1. 更新UserController中的相关方法
2. 验证保护键机制
3. 测试基本CRUD操作

### Phase 3: 文档和示例（预计30分钟）
1. 创建使用示例
2. 编写测试用例
3. 更新API文档

## 风险评估

### 低风险
- **技术栈**：使用成熟的Orleans事件源模式
- **数据结构**：简单的键值对存储，无复杂关联
- **接口设计**：遵循现有模式，学习成本低

### 潜在风险与缓解
- **配置冲突**：通过保护键机制防止
- **性能问题**：通过Grain分片天然避免
- **数据丢失**：Orleans事件源提供持久化保障

## 总结

这个设计实现了：

✅ **极简设计**：3个核心方法，1个事件类型  
✅ **独立管理**：与现有Agent完全分离  
✅ **系统保护**：复用现有保护键机制  
✅ **性能优化**：基于业务标识的天然分片  
✅ **易于集成**：标准Orleans模式，无需额外学习  

相比之前的复杂实现，这个设计更加符合"做一件事并做好"的原则，同时保持了必要的功能完整性。