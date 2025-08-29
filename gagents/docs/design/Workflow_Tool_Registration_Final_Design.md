# 工作流工具注册 - 最终设计方案

## 概述

本文档总结了工作流系统中工具注册的最终设计方案，该方案通过抽象的能力系统实现了灵活且解耦的资源管理。

## 核心设计理念

### 1. 完全抽象化
- `IWorkflowUnit` 接口不包含任何工具相关的具体概念
- 使用通用的"关系"、"能力"和"执行上下文"概念
- 具体的工具注册逻辑由专门的基类处理

### 2. 关键接口设计

```csharp
public interface IWorkflowUnit
{
    // 建立与其他工作流单元的关系
    Task EstablishRelationshipAsync(GrainId relatedUnit, string relationship);
    
    // 获取此单元的能力描述
    Task<WorkflowUnitCapabilities> GetCapabilitiesAsync();
    
    // 准备执行工作流任务
    Task PrepareForExecutionAsync(WorkflowExecutionContext context);
}
```

### 3. 能力声明系统

```csharp
public class WorkflowUnitCapabilities
{
    public string UnitType { get; set; }
    public List<string> ProvidedCapabilities { get; set; }
    public List<string> RequiredCapabilities { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### 4. WorkflowAwareAIGAgentBase 实现

位于 `Aevatar.GAgents.GroupChat` 命名空间，避免循环依赖：

```csharp
public abstract class WorkflowAwareAIGAgentBase<TState, TStateLogEvent, TEvent> :
    AIGAgentBase<TState, TStateLogEvent, TEvent, AIGAgentConfigurationBase>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>
    where TEvent : EventBase
{
    protected virtual async Task OnWorkflowContextReadyAsync(WorkflowExecutionContext context)
    {
        // 自动发现 MCP 提供者
        var mcpProviders = await DiscoverMCPProvidersAsync(context.AvailableResources);
        
        if (mcpProviders.Any())
        {
            // 使用 AIGAgentBase 已有的 ConfigureMCPServersAsync 方法
            await ConfigureMCPServersAsync(mcpProviders);
        }
        
        // 子类可以处理其他类型的资源
        await HandleOtherResourcesAsync(context);
    }
}
```

## 执行流程

### 1. 工作流配置阶段
- WorkflowCoordinator 收集所有节点的能力
- 识别提供者和需求者之间的匹配关系
- 构建工作流执行上下文

### 2. 节点激活阶段
- 调用 `PrepareForExecutionAsync` 传递上下文
- 节点根据自身类型进行准备
- WorkflowAwareAIGAgentBase 自动处理 MCP 工具注册

### 3. 运行时阶段
- 节点使用已注册的工具执行任务
- 通过 Blackboard 共享数据
- 工作流协调器管理执行流程

## 设计优势

### 1. 高度解耦
- 工作流抽象层不依赖具体实现
- 各组件职责清晰分离
- 易于测试和维护

### 2. 灵活扩展
- 可以轻松添加新的资源类型
- 不需要修改核心接口
- 支持动态能力声明

### 3. 避免循环依赖
- WorkflowAwareAIGAgentBase 放在 GroupChat 项目中
- AIGAgent 项目保持独立
- 清晰的依赖层次结构

### 4. 自动化程度高
- 基于能力的自动资源发现
- 工具自动注册
- 减少手动配置

## 使用示例

### 1. 定义需要工具的 AI Agent

```csharp
public class TimeAwareAIGAgent : WorkflowAwareAIGAgentBase<
    TimeAwareAIGAgentState, 
    TimeAwareAIGAgentStateLogEvent, 
    EventBase>
{
    public override Task<WorkflowUnitCapabilities> GetCapabilitiesAsync()
    {
        return Task.FromResult(new WorkflowUnitCapabilities
        {
            UnitType = "TimeAwareAI",
            RequiredCapabilities = new List<string> { "MCPTools" }
        });
    }
    
    public async Task PrepareForExecutionAsync(WorkflowExecutionContext context)
    {
        // 基类会自动处理 MCP 工具注册
        await OnWorkflowContextReadyAsync(context);
    }
}
```

### 2. MCP 提供者声明能力

```csharp
public override Task<WorkflowUnitCapabilities> GetCapabilitiesAsync()
{
    return Task.FromResult(new WorkflowUnitCapabilities
    {
        UnitType = "MCPToolProvider",
        ProvidedCapabilities = new List<string> { "MCPTools" }
    });
}
```

## 与原始设计的对比

### 原始设计问题
1. WorkflowUnit 直接依赖 MCP 概念
2. 在 ProcessNextGAgent 中处理工具注册
3. 只能访问下一个节点的工具
4. 违反了抽象原则

### 最终设计改进
1. 完全抽象的接口设计
2. 在节点准备阶段统一处理
3. 可以访问工作流中所有资源
4. 遵循 SOLID 原则

## 总结

这个设计方案成功地实现了：
- ✅ 高度抽象的工作流系统
- ✅ 灵活的资源管理机制
- ✅ 避免循环依赖
- ✅ 保持代码的可维护性和可扩展性

通过将具体的工具注册逻辑封装在专门的基类中，我们既保持了接口的纯粹性，又提供了便利的实现支持。这是一个很好的抽象与实用性平衡的例子。 