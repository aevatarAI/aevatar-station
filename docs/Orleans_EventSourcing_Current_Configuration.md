# Orleans EventSourcing 当前配置说明

## 📋 配置总结

当前的Orleans EventSourcing配置已经简化并修复了所有问题。

## 🔧 核心配置

### 1. appsettings.json
```json
{
  "OrleansEventSourcing": {
    "Provider": "Mongodb"
  }
}
```

### 2. OrleansHostExtension.cs 关键配置

```csharp
// EventSourcing 配置
var eventSourcingProvider = configuration.GetSection("OrleansEventSourcing:Provider").Get<string>();
if (string.Equals("mongodb", eventSourcingProvider, StringComparison.CurrentCultureIgnoreCase))
{
    // 使用 Aevatar framework 的 MongoDB EventSourcing
    siloBuilder.AddMongoDbStorageBasedLogConsistencyProvider("LogStorage", options =>
    {
        options.ClientSettings = MongoClientSettings.FromConnectionString(mongoClient);
        options.Database = database;
    });
}
else
{
    // 保持 Orleans 原生 Memory EventSourcing 不变
    siloBuilder.AddLogStorageBasedLogConsistencyProvider("LogStorage");
}

// 序列化器配置（支持Orleans格式兼容性）
services.AddSingleton<IGrainStateSerializer, HybridGrainStateSerializer>();
services.AddKeyedSingleton<IGrainStateSerializer>("LogStorage", (sp, key) => sp.GetRequiredService<IGrainStateSerializer>());
```

### 3. Grain 配置
```csharp
[StorageProvider(ProviderName = "Default")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class TestAgentWithConfiguration : LogConsistentGrain<...>
```

## ✅ 已解决的问题

### 1. ~~FormatException 问题~~
- **状态**: ✅ 已完全解决
- **原因**: Log语句使用老式格式（`{0}`, `{1}`）
- **解决**: 全部改为现代格式（`{VariableName}`）

### 2. ~~Orleans版本冲突~~
- **状态**: ✅ 已解决  
- **解决**: 清理了 Directory.Packages.props 中的重复包定义

### 3. ~~存储提供者配置错误~~
- **状态**: ✅ 已解决
- **解决**: TestAgentWithConfiguration 改用 "Default" 而不是 "PubSubStore"

### 4. ~~序列化器注册问题~~
- **状态**: ✅ 已解决
- **解决**: 正确注册了 HybridGrainStateSerializer 和 keyed service

## 🚀 当前状态

- ✅ Orleans EventSourcing 正常运行
- ✅ MongoDB 后端工作正常
- ✅ HybridGrainStateSerializer 支持Orleans格式兼容性
- ✅ 所有Log语句使用安全格式
- ✅ 系统稳定启动

## 📚 技术架构

```
Application Layer
    ↓
Aevatar EventSourcing Framework
    ↓ (使用 AddMongoDbStorageBasedLogConsistencyProvider)
Aevatar.EventSourcing.MongoDB
    ↓ (使用 HybridGrainStateSerializer 支持兼容性)
MongoDB Collection
```

## 🔍 关键组件

1. **HybridGrainStateSerializer**: 支持Orleans Memory格式和Framework格式的兼容性
2. **AddMongoDbStorageBasedLogConsistencyProvider**: Aevatar框架的MongoDB EventSourcing提供者
3. **LogViewAdaptor**: 修复了所有格式化问题的事件存储适配器

## 🚨 注意事项

1. **不要回退到自定义Orleans兼容性代码** - 当前配置已经通过framework的provider解决了兼容性问题
2. **保持log格式的现代化** - 避免使用 `{0}`, `{1}` 等位置参数
3. **编译缓存** - 修改EventSourcing.Core后需要清理编译缓存

---
*当前配置简洁、稳定，完全解决了FormatException问题，支持Orleans Memory到MongoDB的平滑迁移。*