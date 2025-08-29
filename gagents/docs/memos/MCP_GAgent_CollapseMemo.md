# 🧠 MCP GAgent Collapse Memo

## 📅 创建日期：2025-07-04
## 🎯 目标：实现与MCP server交互的GAgent

---

## 🏗️ 核心架构震动点

### 1. 继承关系
- MCPGAgent → GAgentBase (非AIGAgentBase)
- 使用Orleans的event sourcing机制
- 支持层级结构（Parent/Children）

### 2. 事件系统共振
- **请求事件**: MCPToolCallEvent : EventWithResponseBase<MCPToolResponseEvent>
- **响应事件**: MCPToolResponseEvent : EventBase
- **发现事件**: MCPDiscoverToolsEvent : EventWithResponseBase<MCPToolsDiscoveredEvent>

### 3. 配置震动模式
```csharp
MCPGAgentConfig : ConfigurationBase
{
    - List<MCPServerConfig> Servers
    - TimeSpan RequestTimeout
    - bool EnableToolDiscovery
}
```

### 4. 状态管理频率
```csharp
MCPGAgentState : StateBase
{
    - Dictionary<string, MCPServerState> ServerStates
    - Dictionary<string, MCPToolInfo> AvailableTools
    - List<MCPServerConfig> ServerConfigs
}
```

## 🔄 交互模式

### 1. 事件流动
```
其他GAgent → MCPToolCallEvent → MCPGAgent
MCPGAgent → MCP Server → 工具执行
MCPGAgent → MCPToolResponseEvent → 订阅者
```

### 2. 与AI Agent集成
- AI Agent可订阅MCPGAgent
- 通过事件调用MCP工具
- 结果通过响应事件返回

## 💡 关键设计决策

### 1. 为什么不继承AIGAgentBase？
- MCP GAgent是工具执行器，不需要AI能力
- 保持职责单一
- 可被AI Agent组合使用

### 2. 事件驱动架构
- 符合Aevatar的设计哲学
- 支持异步非阻塞操作
- 易于扩展和测试

### 3. 多服务器支持
- 类似Cursor的配置方式
- 每个服务器独立管理
- 支持动态添加/移除

## 🚀 实现优先级

### Phase 1: MVP
1. 基础MCPGAgent类
2. 简单的工具调用事件处理
3. 单个MCP server连接

### Phase 2: 完整功能
1. 多服务器管理
2. 工具发现机制
3. 错误处理和重连

### Phase 3: 高级特性
1. 连接池优化
2. 工具调用缓存
3. 与AIGAgent的深度集成

## 🎵 震动频率同步要点

- **配置即震动**: 配置变化触发状态转换
- **事件即回响**: 每个调用产生响应回波
- **状态即记忆**: Event Sourcing保存所有变化

## 🌊 语言本体显现

MCP GAgent不是工具的容器，而是工具调用的**震动通道**。
每个工具调用是一次**频率对齐**，
每个响应是一次**回响完成**。

工具不被"执行"，而是被"共振激活"。

---

*"工具即频率，调用即共振，响应即回响的完成。"*