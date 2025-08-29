# Aevatar GAgents 项目概述

## 简介

**Aevatar GAgents** 是一个自定义智能代理解决方案，旨在使开发人员能够定制代理并在 **Aevatar Station** 上快速创建、管理和部署它们。该项目基于 .NET 平台，使用 Orleans 分布式虚拟 Actor 框架构建，支持事件溯源和流处理能力。

## 技术栈

- **.NET 8.0 SDK**: 核心开发平台
- **ABP 8.2.0**: 应用程序框架，提供模块化开发支持
- **Orleans 7.0**: 微软的分布式虚拟 Actor 框架
- **Orleans Event Sourcing**: 基于事件的状态管理
- **Orleans Stream**: 流处理能力

## 项目结构

### 核心组件

- **Aevatar.GAgents.Basic**: 基础代理组件，提供基本功能
- **Aevatar.GAgents.AI.Abstractions**: AI 功能的抽象层
- **Aevatar.GAgents.Router**: 消息路由组件

### AI 相关模块

- **Aevatar.GAgents.AIGAgent**: AI 代理实现
- **Aevatar.GAgents.SemanticKernel**: 语义内核集成
- **Aevatar.GAgents.MultiAIChatGAgent**: 多 AI 聊天代理
- **Aevatar.GAgents.GraphRetrievalAgent**: 图检索代理

### 社交媒体集成

- **Aevatar.GAgents.Twitter**: Twitter 集成代理
- **Aevatar.GAgents.Telegram**: Telegram 集成代理
- **Aevatar.GAgents.SocialGAgent**: 社交媒体通用代理
- **Aevatar.GAgents.ChatAgent**: 聊天功能代理
- **Aevatar.GAgents.GroupChat**: 群聊功能代理

### 区块链集成

- **Aevatar.GAgents.AElf**: AElf 区块链集成

### 示例应用

- **AIGAgent 示例**: 演示 AI 代理功能的示例
- **GroupChat 示例**: 演示群聊功能的示例

## 开发架构

项目采用基于 Actor 模型的分布式系统架构：

1. **Actor 模型**: 每个代理作为独立的 Actor 运行
2. **事件溯源**: 利用事件溯源进行状态管理
3. **事件驱动**: 基于事件的通信机制
4. **模块化设计**: 各功能模块可独立开发和部署

## 创建代理的步骤

1. **创建 Agent 存储类**:
   - 使用 `[GenerateSerializer]` 和 `[Id(n)]` 特性

2. **创建 EventSourcing 事件类**:
   - 必须继承自 `StateLogEventBase`
   - 用于记录状态变更

3. **创建接收外部消息的事件类**:
   - 必须继承自 `EventBase`
   - 使用 `[GenerateSerializer]` 和 `[Id(n)]` 特性

4. **创建 Agent 实现类**:
   - 继承自 `GAgentBase<TState, TEvent>`
   - 实现处理事件的方法，使用 `[EventHandler]` 特性

## 依赖项

项目依赖以下主要包：

- **Aevatar.Core**: 核心功能库
- **Aevatar.EventSourcing.Core**: 事件溯源核心库
- **Aevatar.Core.Abstractions**: 核心抽象库

## 许可证

项目采用 MIT 许可证，详情参见 [LICENSE](LICENSE) 文件。 