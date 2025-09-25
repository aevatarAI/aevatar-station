# WebAPI GAgent

WebAPI GAgent 是 Aevatar 框架中的一个通用HTTP API客户端代理，提供统一、可配置、可扩展的Web API交互解决方案。

## 特性

### 🔐 多种认证方式
- **Bearer Token** - 现代API的首选认证方式
- **API Key** - 支持Header或Query Parameter方式
- **Basic Auth** - 传统用户名/密码认证
- **Custom Headers** - 完全自定义的认证头

### 🔄 可靠性保证
- **自动重试** - 指数退避算法，可配置重试次数和状态码
- **超时控制** - 请求和连接超时设置
- **健康检查** - 自动监控API可用性
- **错误分类** - 详细的错误类型和状态码处理

### 📊 可观测性
- **请求历史** - 完整的请求/响应记录
- **性能指标** - 成功率、响应时间、重试次数等
- **状态管理** - 基于Orleans事件溯源的持久化状态
- **事件发布** - 请求、响应、错误等业务事件

### ⚡ 性能优化
- **连接复用** - 基于HttpClientFactory的连接池管理
- **速率限制** - 可配置的请求频率控制
- **并发控制** - 防止API过载的并发限制

## 快速开始

### 1. 基本用法

```csharp
using Aevatar.GAgents.Basic.WebApiGAgent;

// 创建配置
var config = new WebApiGAgentConfiguration
{
    BaseUrl = "https://api.example.com/v1",
    Authentication = new WebApiAuthenticationConfig
    {
        Type = WebApiAuthenticationType.BearerToken,
        BearerToken = "your-bearer-token"
    },
    EnableLogging = true,
    EnableMetrics = true
};

// 获取GAgent实例
var webApiAgent = await gAgentFactory.GetGAgentAsync<IWebApiGAgent>(Guid.NewGuid(), config);

// 执行GET请求
var response = await webApiAgent.GetAsync<User>("/users/123");
if (response.IsSuccess)
{
    Console.WriteLine($"User: {response.Data?.Name}");
}
```

### 2. CRUD操作示例

```csharp
// GET - 获取用户
var user = await webApiAgent.GetAsync<User>("/users/123");

// POST - 创建用户
var newUser = new CreateUserRequest { Name = "John", Email = "john@example.com" };
var created = await webApiAgent.PostAsync<User>("/users", newUser);

// PUT - 更新用户
var update = new UpdateUserRequest { Name = "John Updated" };
var updated = await webApiAgent.PutAsync<User>($"/users/{userId}", update);

// DELETE - 删除用户
var deleted = await webApiAgent.DeleteAsync<string>($"/users/{userId}");
```

## 认证配置示例

### Bearer Token 认证
```csharp
var config = new WebApiGAgentConfiguration
{
    BaseUrl = "https://api.example.com",
    Authentication = new WebApiAuthenticationConfig
    {
        Type = WebApiAuthenticationType.BearerToken,
        BearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    }
};
```

### API Key 认证
```csharp
var config = new WebApiGAgentConfiguration
{
    BaseUrl = "https://api.service.com",
    Authentication = new WebApiAuthenticationConfig
    {
        Type = WebApiAuthenticationType.ApiKey,
        ApiKey = "your-api-key",
        ApiKeyHeader = "X-API-Key"  // 默认是 "X-API-Key"
    }
};
```

### Basic 认证
```csharp
var config = new WebApiGAgentConfiguration
{
    BaseUrl = "https://api.internal.com",
    Authentication = new WebApiAuthenticationConfig
    {
        Type = WebApiAuthenticationType.BasicAuth,
        Username = "admin",
        Password = "password123"
    }
};
```

### 自定义Headers认证
```csharp
var config = new WebApiGAgentConfiguration
{
    BaseUrl = "https://api.custom.com",
    Authentication = new WebApiAuthenticationConfig
    {
        Type = WebApiAuthenticationType.CustomHeaders,
        CustomHeaders = new Dictionary<string, string>
        {
            ["X-Custom-Auth"] = "custom-value",
            ["X-Client-ID"] = "client-12345"
        }
    }
};
```

## 高级配置

### 重试机制
```csharp
var config = new WebApiGAgentConfiguration
{
    // ...其他配置
    Retry = new WebApiRetryConfig
    {
        EnableRetry = true,
        MaxAttempts = 3,                    // 最大重试次数
        BaseDelaySeconds = 1,               // 基础延迟时间
        MaxDelaySeconds = 30,               // 最大延迟时间
        BackoffMultiplier = 2.0,            // 退避乘数
        RetryableStatusCodes = new List<int> { 429, 500, 502, 503, 504 }
    }
};
```

### 超时设置
```csharp
var config = new WebApiGAgentConfiguration
{
    // ...其他配置
    Timeouts = new WebApiTimeoutConfig
    {
        RequestTimeoutSeconds = 30,     // 请求超时
        ConnectionTimeoutSeconds = 10   // 连接超时
    }
};
```

### 速率限制
```csharp
var config = new WebApiGAgentConfiguration
{
    // ...其他配置
    RateLimit = new WebApiRateLimitConfig
    {
        EnableRateLimit = true,
        RequestsPerMinute = 100,    // 每分钟最大请求数
        BurstSize = 20             // 突发请求容量
    }
};
```

## 监控和诊断

### 获取性能指标
```csharp
var metrics = await webApiAgent.GetMetricsAsync();
Console.WriteLine($"总请求数: {metrics.TotalRequests}");
Console.WriteLine($"成功率: {(metrics.SuccessfulRequests * 100.0 / metrics.TotalRequests):F1}%");
Console.WriteLine($"平均响应时间: {metrics.AverageResponseTimeMs:F1}ms");
Console.WriteLine($"总重试次数: {metrics.TotalRetries}");
```

### 查看请求历史
```csharp
var history = await webApiAgent.GetRequestHistoryAsync(10);
foreach (var record in history)
{
    Console.WriteLine($"{record.RequestTime}: {record.Method} {record.Endpoint} - " +
                     $"状态码: {record.StatusCode}, 耗时: {record.ElapsedMilliseconds}ms, " +
                     $"重试: {record.RetryCount}次");
}
```

### 健康状态检查
```csharp
var health = await webApiAgent.GetHealthStatusAsync();
Console.WriteLine($"健康状态: {(health.IsHealthy ? "健康" : "不健康")}");
Console.WriteLine($"状态消息: {health.StatusMessage}");
Console.WriteLine($"连续失败次数: {health.ConsecutiveFailures}");

// 测试连接
var isConnected = await webApiAgent.TestConnectionAsync();
Console.WriteLine($"连接测试: {(isConnected ? "成功" : "失败")}");
```

## 错误处理

### 处理API错误
```csharp
var response = await webApiAgent.GetAsync<User>("/users/123");

if (!response.IsSuccess)
{
    Console.WriteLine($"请求失败: {response.ErrorMessage}");
    Console.WriteLine($"状态码: {response.StatusCode}");
    
    // 根据状态码处理不同类型的错误
    switch (response.StatusCode)
    {
        case 401:
            Console.WriteLine("认证失败，请检查token");
            break;
        case 403:
            Console.WriteLine("权限不足");
            break;
        case 404:
            Console.WriteLine("资源不存在");
            break;
        case 429:
            Console.WriteLine("请求频率过高，稍后重试");
            break;
        case >= 500:
            Console.WriteLine("服务器错误，将自动重试");
            break;
    }
}
```

## 事件系统

WebAPI GAgent 发布以下事件，可以订阅进行监控：

- `WebApiRequestEvent` - 请求开始时触发
- `WebApiResponseEvent` - 收到响应时触发
- `WebApiErrorEvent` - 发生错误时触发
- `WebApiHealthCheckEvent` - 健康检查时触发
- `WebApiConfigurationUpdatedEvent` - 配置更新时触发

### 订阅事件示例
```csharp
// 在其他GAgent中订阅WebAPI事件
[EventHandler]
public async Task HandleWebApiResponseAsync(WebApiResponseEvent @event)
{
    if (!@event.IsSuccess && @event.StatusCode >= 500)
    {
        Logger.LogWarning("WebAPI服务器错误: {RequestId}, 状态码: {StatusCode}", 
                         @event.RequestId, @event.StatusCode);
    }
}
```

## 最佳实践

### 1. 配置管理
- 使用环境变量或配置文件管理敏感信息（API密钥、密码等）
- 为不同环境配置不同的重试和超时设置
- 启用日志和指标收集用于监控

### 2. 错误处理
- 总是检查 `IsSuccess` 属性
- 根据状态码实现不同的错误处理逻辑
- 对于临时错误，依赖自动重试机制

### 3. 性能优化
- 合理设置超时时间，避免长时间阻塞
- 根据API限制配置速率限制
- 定期检查指标，调整配置参数

### 4. 监控和运维
- 定期检查健康状态和指标
- 监控连续失败次数，及时处理异常
- 使用事件系统进行实时监控和告警

## 故障排除

### 常见问题

#### 1. 请求总是失败
- 检查base URL是否正确
- 验证认证配置是否正确
- 检查网络连接和防火墙设置

#### 2. 认证失败
- 确认认证类型选择正确
- 检查token或API密钥是否有效
- 验证认证头名称是否正确

#### 3. 请求超时
- 调整超时设置
- 检查网络状况
- 确认API服务器响应时间

#### 4. 重试次数过多
- 检查可重试的状态码配置
- 调整重试间隔和最大次数
- 检查API服务器稳定性

### 日志和调试
- 启用 `EnableLogging = true` 查看详细日志
- 检查请求历史了解具体错误信息
- 使用 `TestConnectionAsync()` 验证基本连接

## 扩展和自定义

WebAPI GAgent 设计为可扩展的，支持：

1. **自定义认证方式** - 通过继承实现新的认证类型
2. **请求/响应中间件** - 拦截和处理请求/响应
3. **自定义缓存策略** - 实现特定的缓存逻辑
4. **监控集成** - 集成第三方监控系统

如需自定义扩展，请参考设计文档和示例代码。
