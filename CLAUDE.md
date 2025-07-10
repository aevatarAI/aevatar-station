# Claude Code Memory

## Orleans EventSourcing vs Aevatar Framework EventSourcing Architecture

### 设计架构对比

#### Orleans EventSourcing (Memory模式)
```
存储层级:
- Provider: LogStorageBasedLogConsistencyProvider
- Storage: Memory-based (Orleans.EventSourcing.LogStorage)
- Format: LogStateWithMetaDataAndETag<TLogEntry>
- 数据结构:
  └── LogStateWithMetaDataAndETag
      ├── State.Log: List<TLogEntry> (事件列表)
      ├── State.GlobalVersion: int (全局版本)
      ├── State.WriteVector: string (Orleans格式: ",replica1,replica2")
      └── ETag: string

序列化:
- 使用Orleans原生序列化器
- 数据存储在内存中，重启丢失
- WriteVector使用逗号分隔格式

版本管理:
- 版本号从0开始递增
- 每个事件对应一个版本号
- GlobalVersion跟踪整体版本
```

#### Aevatar Framework EventSourcing (MongoDB模式)
```
存储层级:
- Provider: MongoDbStorageBasedLogConsistencyProvider
- Storage: MongoDB (Aevatar.EventSourcing.MongoDB)
- Format: ViewStateSnapshotWithMetadata<TLogView>
- 数据结构:
  └── ViewStateSnapshotWithMetadata
      ├── Snapshot: TLogView (当前状态快照)
      ├── SnapshotVersion: int (快照版本)
      └── WriteVector: string (Framework格式: "replica1;replica2")

MongoDB文档结构:
{
  "GrainId": "string",
  "Version": int,
  "Data": { /* 序列化的事件数据 */ }
}

序列化:
- 使用Aevatar GrainStateSerializer
- 数据持久化在MongoDB中
- WriteVector使用分号分隔格式

版本管理:
- 版本号存储在MongoDB的Version字段
- 通过GetLastVersionAsync获取最新版本
- 支持SetInitialVersionAsync设置起始版本
```

### 兼容性转换架构

#### 转换流程
```
Orleans Memory EventSourcing
    ↓ (LogViewAdaptor.ReadAsync)
1. 尝试读取Framework格式快照
    ↓ (失败时)
2. TryConvertOrleansLogStorageAsync
    ├── 读取Orleans LogStateWithMetaDataAndETag
    ├── 重放Orleans事件列表重建状态
    ├── ConvertOrleansToFrameworkSnapshot
    │   ├── 调用SetInitialVersionAsync设置版本起始值
    │   ├── ConvertOrleansWriteVectorToFrameworkFormat
    │   └── 创建ViewStateSnapshotWithMetadata
    └── WriteStateAsync保存Framework格式
    ↓
Aevatar Framework MongoDB EventSourcing
```

#### 数据格式转换
```
Orleans格式 → Framework格式:

WriteVector:
",AevatarSiloCluster" → "AevatarSiloCluster"
",replica1,replica2" → "replica1;replica2"

版本号:
Orleans.State.Log.Count → MongoDB.Version
Orleans.GlobalVersion → _globalVersion

状态管理:
Orleans事件重放 → Framework快照 + 版本占位符
```

### 核心组件说明

#### LogViewAdaptor (Framework)
- **位置**: `Aevatar.EventSourcing.Core/Storage/LogViewAdaptor.EventSourcing.cs`
- **作用**: Orleans与Framework的桥接层
- **核心方法**:
  - `TryConvertOrleansLogStorageAsync`: Orleans→Framework转换
  - `ConvertOrleansToFrameworkSnapshot`: 快照格式转换
  - `ConvertOrleansWriteVectorToFrameworkFormat`: WriteVector格式转换

#### SafeStringEncodedWriteVector (Framework)
- **位置**: `Aevatar.EventSourcing.Core/Common/SafeStringEncodedWriteVector.cs`
- **作用**: 安全的WriteVector操作，解决Orleans原生索引越界问题
- **核心方法**:
  - `GetBit`: 安全获取WriteVector位
  - `FlipBit`: 安全翻转WriteVector位

#### MongoDbLogConsistentStorage (Framework)
- **位置**: `Aevatar.EventSourcing.MongoDB/MongoDbLogConsistentStorage.cs`
- **作用**: MongoDB持久化存储实现
- **核心方法**:
  - `SetInitialVersionAsync`: 设置版本号起始值(新增)
  - `GetLastVersionAsync`: 获取最新版本号
  - `AppendAsync`: 追加事件到MongoDB

### 配置架构

#### appsettings.json
```json
{
  "OrleansEventSourcing": {
    "Provider": "Mongodb"  // "Memory" for Orleans, "Mongodb" for Framework
  }
}
```

#### OrleansHostExtension.cs
```csharp
// Orleans模式
siloBuilder.AddLogStorageBasedLogConsistencyProvider("LogStorage");

// Framework模式  
siloBuilder.AddMongoDbStorageBasedLogConsistencyProvider("LogStorage", options => {
    options.ClientSettings = MongoClientSettings.FromConnectionString(mongoClient);
    options.Database = database;
});

// 兼容性序列化器
services.AddSingleton<IGrainStateSerializer, HybridGrainStateSerializer>();
```

### 版本号管理策略

#### Orleans模式
- 版本号 = 事件列表长度
- 重启后从0开始重新计算
- 无持久化版本管理

#### Framework模式
- 版本号持久化在MongoDB
- 通过SetInitialVersionAsync保证迁移连续性
- 支持跨重启的版本追踪

### 已解决的关键问题 (2025-07-10)

1. **IndexOutOfRangeException**: SafeStringEncodedWriteVector替换Orleans原生方法
2. **版本号连续性**: SetInitialVersionAsync确保迁移时版本号延续
3. **性能优化**: 避免批量写入历史事件，只写版本占位符
4. **格式兼容**: WriteVector格式转换(",format" → ";format")
5. **代码简化**: 移除冗余Orleans兼容性代码

### 存储集合格式

#### MongoDB集合命名
```
格式: {ServiceId}/{ProviderName}/log/{GrainType}
示例: AevatarBasicService/LogStorage/log/testagentwithconfiguration
```

#### 文档结构
```json
{
  "_id": ObjectId,
  "GrainId": "testagentwithconfiguration/guid",
  "Version": 3,
  "Data": {
    "_t": "EventType",
    /* 事件数据 */
  }
}
```

### 迁移验证要点

- Orleans Memory事件数量 = Framework起始版本号
- WriteVector格式正确转换
- 状态重放结果一致
- 新事件从正确版本号继续追加