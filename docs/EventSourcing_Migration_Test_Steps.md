# EventSourcing 迁移测试步骤

## 📋 测试目的
验证Orleans EventSourcing从Memory存储到MongoDB存储的迁移过程是否正常工作。

## 🔧 测试环境要求
- MongoDB服务运行在localhost:27017
- Redis服务运行在localhost:6379
- 端口8308(HttpApi)和8082(AuthServer)可用

## 📝 测试步骤

### 阶段1: Memory存储阶段

#### 1.1 修改配置为Memory
```bash
# 修改 src/Aevatar.Silo/appsettings.json
# 将 OrleansEventSourcing.Provider 设置为 "Memory"
```

#### 1.2 启动服务
```bash
# 启动Silo服务
cd src/Aevatar.Silo
dotnet run

# 启动Developer服务（新终端）
cd src/Aevatar.Developer.Host
dotnet run
```

#### 1.3 创建Agents和Events
```bash
# 执行create-agents-with-subagents.py
python create-agents-with-subagents.py
```

**预期结果:**
- 成功创建3个TwitterAgent
- 每个Agent发布3个事件
- 所有数据存储在内存中

### 阶段2: MongoDB存储阶段

#### 2.1 修改配置为MongoDB
```bash
# 修改 src/Aevatar.Silo/appsettings.json
# 将 OrleansEventSourcing.Provider 设置为 "Mongodb"
```

#### 2.2 重启服务
```bash
# 停止所有服务 (Ctrl+C)
# 重新启动Silo服务
cd src/Aevatar.Silo
dotnet run

# 重新启动Developer服务（新终端）
cd src/Aevatar.Developer.Host
dotnet run
```

#### 2.3 发送更多Events
```bash
# 执行send-more-events.py
python send-more-events.py
```

**预期结果:**
- 能够使用之前创建的Agent ID发送新事件
- 事件成功存储到MongoDB
- 系统能够正常处理Memory到MongoDB的迁移

## 🔍 验证要点

### 数据一致性检查
- [ ] Agent状态是否正确保存到MongoDB
- [ ] Event历史是否完整
- [ ] 新发送的事件是否正确处理

### 性能检查
- [ ] 迁移后响应时间是否正常
- [ ] 内存使用是否合理
- [ ] 无错误日志

### 功能检查
- [ ] Agent创建功能正常
- [ ] Event发布功能正常
- [ ] 数据查询功能正常

## 📊 测试记录

### 执行时间
- 开始时间: 2025-01-08 12:40PM
- 结束时间: 2025-01-08 12:50PM
- 总耗时: 约10分钟

### 测试结果
- [x] 阶段1完成 - Memory模式成功
- [x] 阶段2完成 - MongoDB模式发现问题
- [ ] 所有验证通过

### 问题记录
- 问题1: **IndexOutOfRangeException in StringEncodedWriteVector.FlipBit**
  - 错误位置: Orleans.EventSourcing.Common.StringEncodedWriteVector.FlipBit
  - 影响: 所有从Memory创建的Agent在MongoDB模式下无法处理新事件
  - 错误信息: "Index was outside the bounds of the array"

- 问题2: **连接失败错误**
  - 错误: Unable to connect to endpoint S127.0.0.1:10001:111127370
  - 影响: 服务重启后第一次调用失败
  - 可能原因: 服务启动未完成或端口冲突

- 问题3: **数据格式不兼容**
  - 问题: Memory EventSourcing创建的状态与MongoDB EventSourcing不兼容
  - 影响: 无法实现平滑迁移
  - 表现: 写入向量格式错误

### 解决方案
- 解决方案1: **修复写入向量兼容性**
  - 需要在Aevatar.EventSourcing.Core中处理不同存储模式的写入向量格式
  - 建议在FlipBit方法中添加边界检查和兼容性逻辑

- 解决方案2: **改进迁移策略**
  - 不建议直接从Memory切换到MongoDB
  - 需要实现专门的迁移工具或清理策略
  - 考虑在切换前清理所有Grain状态

- 解决方案3: **增强服务启动检查**
  - 在执行测试前添加服务健康检查
  - 确保所有服务完全启动后再进行测试

## 📋 配置文件备份

### Memory配置
```json
{
  "OrleansEventSourcing": {
    "Provider": "Memory"
  }
}
```

### MongoDB配置
```json
{
  "OrleansEventSourcing": {
    "Provider": "Mongodb"
  }
}
```

## 🎯 成功标准
1. 所有Agent和Event在Memory阶段成功创建
2. 配置修改后服务能够正常重启
3. MongoDB阶段能够识别之前的Agent并接收新事件
4. 无致命错误或异常
5. 数据在MongoDB中正确存储

## 🚨 关键发现

### 迁移兼容性问题
❌ **Memory到MongoDB的直接迁移不兼容**
- Memory EventSourcing创建的Grain状态无法在MongoDB模式下正常工作
- 写入向量(WriteVector)格式存在差异，导致IndexOutOfRangeException
- 这表明两种存储模式的内部数据结构不兼容

### 测试执行结果
✅ **阶段1(Memory模式)**: 完全成功
- 创建了3个Agent: TwitterAgent1, TwitterAgent2, TwitterAgent3
- 每个Agent成功发布3个事件，总计9个事件
- 所有操作正常，无错误

❌ **阶段2(MongoDB模式)**: 发现严重问题
- 能够识别之前创建的Agent ID (说明基本连接正常)
- 但所有事件发布操作都失败，出现IndexOutOfRangeException
- 错误集中在StringEncodedWriteVector.FlipBit方法

### 技术分析
1. **写入向量不兼容**: Memory和MongoDB存储的写入向量格式不同
2. **状态迁移问题**: 从Memory到MongoDB需要专门的迁移逻辑
3. **架构限制**: 当前架构不支持存储模式的热切换

## 📝 备注
- 测试使用无认证模式，已移除token验证
- Agent IDs在脚本中硬编码，确保测试一致性
- 如遇到问题，检查MongoDB连接和Orleans配置
- **重要**: 不建议在生产环境中直接切换EventSourcing Provider 