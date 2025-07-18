# AI LLM Agent Demo

基于 Aevatar GAgent Framework 的 AI LLM Agent 演示项目。

## 📋 项目概述

这个项目展示了如何使用 Aevatar Framework 创建一个能够调用各种 LLM 提供商（如 OpenAI、DeepSeek 等）的智能代理。该代理遵循 Aevatar 的标准 GAgent 实现模式，具备状态管理、事件处理和持久化等核心功能。

## 🏗️ 架构特点

### 核心组件

1. **AILLMGAgent** - 继承自 `GAgentBase`，实现标准的 GAgent 模式
2. **状态管理** - 通过 `AILLMGAgentState` 管理 Agent 状态
3. **事件溯源** - 使用 `AILLMStateLogEvent` 记录所有操作历史
4. **API 控制器** - 提供 RESTful API 接口
5. **演示客户端** - 完整的使用示例

### 设计模式

- **Event Sourcing** - 所有状态变更通过事件记录
- **CQRS** - 命令查询职责分离
- **Actor Model** - 基于 Orleans 的 Actor 模式
- **依赖注入** - 标准的 .NET DI 容器

## 🚀 快速开始

### 前置条件

1. .NET 9.0 SDK
2. 运行中的 Aevatar Station 服务
3. 配置好的 LLM 服务（可选，有回退模拟）

### 运行演示

```bash
# 1. 确保 Aevatar.HttpApi.Host 正在运行
cd station/src/Aevatar.HttpApi.Host
dotnet run

# 2. 运行演示客户端
cd station/samples/AIAgentDemo/AIAgentDemo.Client
dotnet run
```

## 📚 API 接口文档

### 基础 URL
```
http://localhost:7002/api/ai-llm
```

### 接口列表

#### 1. 快速聊天
```http
POST /quick-chat
Content-Type: application/json

{
  "prompt": "你好，请介绍一下自己"
}
```

**响应:**
```json
{
  "response": "AI 的回复内容",
  "isSuccessful": true,
  "tokensUsed": 150,
  "callTime": "2024-01-01T12:00:00Z",
  "usedProvider": "OpenAI",
  "usedModel": "gpt-4o"
}
```

#### 2. 高级 LLM 调用
```http
POST /call
Content-Type: application/json

{
  "prompt": "解释Actor模型",
  "llmProvider": "OpenAI",
  "model": "gpt-4o",
  "temperature": 0.7,
  "maxTokens": 2000
}
```

#### 3. 获取 Agent 状态
```http
GET /status
```

**响应:**
```json
{
  "lastPrompt": "最后的提示词",
  "lastResponse": "最后的回复",
  "selectedLLMProvider": "OpenAI",
  "selectedModel": "gpt-4o",
  "totalTokensUsed": 5000,
  "lastCallTime": "2024-01-01T12:00:00Z",
  "callHistory": [...],
  "isInitialized": true
}
```

#### 4. 获取调用历史
```http
GET /history
```

#### 5. 清空历史记录
```http
DELETE /history
```

#### 6. 设置默认 LLM
```http
POST /set-default
Content-Type: application/json

{
  "provider": "OpenAI",
  "model": "gpt-4o"
}
```

#### 7. 获取 Agent 描述
```http
GET /description
```

## 🔧 配置说明

### LLM 提供商配置

在 `appsettings.json` 中配置 LLM 服务：

```json
{
  "SystemLLMConfigs": {
    "OpenAI": {
      "ProviderEnum": "OpenAI",
      "ModelIdEnum": "GPT4O",
      "ModelName": "gpt-4o",
      "Endpoint": "https://api.openai.com",
      "ApiKey": "your-api-key"
    },
    "DeepSeek": {
      "ProviderEnum": "DeepSeek",
      "ModelIdEnum": "DeepSeekR1",
      "ModelName": "deepseek-r1",
      "Endpoint": "https://api.deepseek.com",
      "ApiKey": "your-api-key"
    }
  }
}
```

### Semantic Kernel 配置

项目支持通过 Semantic Kernel 调用 LLM 服务。如果没有配置，将回退到模拟模式。

## 🎯 使用示例

### C# 客户端示例

```csharp
// 获取 AI Agent
var aiAgent = await gAgentFactory.GetGAgentAsync<IAILLMGAgent>();

// 快速聊天
var response = await aiAgent.QuickChatAsync("解释什么是微服务");

// 高级调用
var request = new LLMRequest
{
    Prompt = "详细解释Actor模型",
    LLMProvider = "OpenAI",
    Model = "gpt-4o",
    Temperature = 0.3,
    MaxTokens = 1000
};
var response = await aiAgent.CallLLMAsync(request);

// 获取状态
var state = await aiAgent.GetAgentStateAsync();

// 获取历史
var history = await aiAgent.GetCallHistoryAsync();
```

### HTTP 客户端示例

```bash
# 快速聊天
curl -X POST "http://localhost:7002/api/ai-llm/quick-chat" \
  -H "Content-Type: application/json" \
  -d '{"prompt": "你好"}'

# 获取状态
curl -X GET "http://localhost:7002/api/ai-llm/status"

# 清空历史
curl -X DELETE "http://localhost:7002/api/ai-llm/history"
```

## 📊 功能特性

### ✅ 已实现功能

- [x] 多 LLM 提供商支持（OpenAI、DeepSeek 等）
- [x] 可配置的模型参数（温度、最大token等）
- [x] 完整的调用历史记录
- [x] 状态持久化和恢复
- [x] 事件溯源和状态重放
- [x] RESTful API 接口
- [x] 错误处理和回退机制
- [x] Token 使用统计
- [x] 默认 LLM 配置管理

### 🚧 计划功能

- [ ] 支持流式响应
- [ ] 多轮对话上下文管理
- [ ] 函数调用(Function Calling)支持
- [ ] 批量请求处理
- [ ] 更详细的使用分析和报告
- [ ] 支持更多 LLM 提供商

## 🧪 测试演示

演示客户端包含以下测试场景：

1. **Agent 信息获取** - 获取描述和状态
2. **快速聊天** - 预定义提示词测试
3. **高级调用** - 自定义参数测试
4. **历史管理** - 查看和清空历史
5. **配置管理** - 设置默认 LLM

## 🛠️ 开发指南

### 扩展新的 LLM 提供商

1. 在配置中添加新的提供商信息
2. 更新 `InvokeLLMAsync` 方法支持新的 API
3. 添加相应的错误处理

### 自定义事件处理

```csharp
protected override void GAgentTransitionState(AILLMGAgentState state, StateLogEventBase<AILLMStateLogEvent> @event)
{
    if (@event is AILLMStateLogEvent llmEvent)
    {
        // 自定义状态转换逻辑
        switch (llmEvent.EventType)
        {
            case "CUSTOM_EVENT":
                // 处理自定义事件
                break;
        }
    }
    
    base.GAgentTransitionState(state, @event);
}
```

## 📝 注意事项

1. **认证** - 生产环境需要配置适当的认证机制
2. **速率限制** - 注意 LLM 提供商的 API 调用限制
3. **成本控制** - 监控 Token 使用量，避免意外费用
4. **错误处理** - 实现重试机制和回退策略
5. **数据隐私** - 确保敏感数据的适当处理

## 🤝 贡献指南

1. Fork 项目
2. 创建功能分支 (`git checkout -b feature/amazing-feature`)
3. 提交更改 (`git commit -m 'Add some amazing feature'`)
4. 推送分支 (`git push origin feature/amazing-feature`)
5. 创建 Pull Request

## 📄 许可证

本项目遵循 MIT 许可证。详见 `LICENSE` 文件。

## 🙋‍♂️ 支持

如有问题或建议，请：

1. 查看文档和示例代码
2. 搜索已有的 Issues
3. 创建新的 Issue 详细描述问题
4. 联系开发团队

---

**Happy Coding! 🎉** 