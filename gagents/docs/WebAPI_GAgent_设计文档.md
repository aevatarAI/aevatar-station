# WebAPI GAgent 设计文档

## 概述

WebAPI GAgent 是 Aevatar 框架中一个通用的HTTP API客户端代理，旨在为各种Web API交互提供统一、可配置、可扩展的解决方案。它支持多种认证方式、请求重试、状态管理和事件驱动的架构。

## 设计目标

### 1. 通用性
- 支持任意HTTP API的调用
- 可配置的base URL、headers、认证方式
- 灵活的请求/响应处理

### 2. 认证支持
- Bearer Token 认证
- Basic Authentication (username/password)
- API Key 认证 (Header/Query Parameter)
- OAuth 2.0 Client Credentials
- 自定义Header认证
- 无认证模式

### 3. 可靠性
- 自动重试机制（指数退避）
- 超时控制
- 错误处理和分类
- 请求/响应日志

### 4. 性能
- HTTP连接池复用
- 响应缓存（可选）
- 速率限制支持
- 异步非阻塞设计

### 5. 可观测性
- 请求追踪和指标
- 详细的状态管理
- 事件驱动的监控
- 调试和诊断信息

## 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                    WebAPI GAgent                             │
│  ┌─────────────────────────────────────────────────────┐    │
│  │                WebAPIGAgent                         │    │
│  │  - 状态管理 (Orleans Event Sourcing)                │    │
│  │  - 配置管理                                         │    │
│  │  - 事件发布/订阅                                    │    │
│  └─────────────────────────────────────────────────────┘    │
│                            │                                │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              WebAPIClient                           │    │
│  │  - HTTP请求封装                                     │    │
│  │  - 重试逻辑                                        │    │
│  │  - 超时处理                                        │    │
│  │  - 响应解析                                        │    │
│  └─────────────────────────────────────────────────────┘    │
│                            │                                │
│  ┌─────────────────────────────────────────────────────┐    │
│  │         WebAPIAuthenticationHandler                 │    │
│  │  - Bearer Token                                     │    │
│  │  - Basic Auth                                      │    │
│  │  - API Key                                         │    │
│  │  - OAuth 2.0                                       │    │
│  │  - Custom Headers                                  │    │
│  └─────────────────────────────────────────────────────┘    │
│                            │                                │
│  ┌─────────────────────────────────────────────────────┐    │
│  │           HttpClient (.NET)                         │    │
│  │  - 连接池管理                                      │    │
│  │  - SSL/TLS处理                                     │    │
│  │  - 压缩支持                                        │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

## 核心组件设计

### 1. WebAPIGAgent（主要组件）

#### 接口定义
```csharp
public interface IWebAPIGAgent : IStateGAgent<WebAPIGAgentState>
{
    // 基础HTTP操作
    Task<WebAPIResponse<T>> GetAsync<T>(string endpoint, Dictionary<string, string>? headers = null);
    Task<WebAPIResponse<T>> PostAsync<T>(string endpoint, object? payload = null, Dictionary<string, string>? headers = null);
    Task<WebAPIResponse<T>> PutAsync<T>(string endpoint, object? payload = null, Dictionary<string, string>? headers = null);
    Task<WebAPIResponse<T>> DeleteAsync<T>(string endpoint, Dictionary<string, string>? headers = null);
    Task<WebAPIResponse<string>> RequestAsync(HttpMethod method, string endpoint, object? payload = null, Dictionary<string, string>? headers = null);
    
    // 配置管理
    Task<bool> UpdateConfigurationAsync(WebAPIGAgentConfiguration configuration);
    Task<WebAPIGAgentConfiguration> GetConfigurationAsync();
    
    // 状态查询
    Task<WebAPIHealthStatus> GetHealthStatusAsync();
    Task<List<WebAPIRequestRecord>> GetRequestHistoryAsync(int limit = 50);
    Task<WebAPIMetrics> GetMetricsAsync();
    
    // 认证管理
    Task<bool> RefreshAuthenticationAsync();
    Task<bool> TestConnectionAsync();
}
```

#### 状态管理
```csharp
[GenerateSerializer]
public class WebAPIGAgentState : StateBase
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public WebAPIGAgentConfiguration? Configuration { get; set; }
    [Id(3)] public List<WebAPIRequestRecord> RequestHistory { get; set; } = new();
    [Id(4)] public WebAPIMetrics Metrics { get; set; } = new();
    [Id(5)] public WebAPIHealthStatus HealthStatus { get; set; } = new();
    [Id(6)] public DateTime LastActivityAt { get; set; }
    [Id(7)] public Dictionary<string, object> CustomProperties { get; set; } = new();
}
```

### 2. 配置系统

#### 主配置类
```csharp
[GenerateSerializer]
public class WebAPIGAgentConfiguration : ConfigurationBase
{
    [Id(0)] public string BaseUrl { get; set; } = string.Empty;
    [Id(1)] public WebAPIAuthenticationConfig Authentication { get; set; } = new();
    [Id(2)] public WebAPITimeoutConfig Timeouts { get; set; } = new();
    [Id(3)] public WebAPIRetryConfig Retry { get; set; } = new();
    [Id(4)] public WebAPICacheConfig Cache { get; set; } = new();
    [Id(5)] public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    [Id(6)] public bool EnableLogging { get; set; } = true;
    [Id(7)] public bool EnableMetrics { get; set; } = true;
    [Id(8)] public WebAPIRateLimitConfig RateLimit { get; set; } = new();
}
```

#### 认证配置
```csharp
[GenerateSerializer]
public class WebAPIAuthenticationConfig
{
    [Id(0)] public WebAPIAuthenticationType Type { get; set; } = WebAPIAuthenticationType.None;
    [Id(1)] public string? BearerToken { get; set; }
    [Id(2)] public string? ApiKey { get; set; }
    [Id(3)] public string? ApiKeyHeader { get; set; } = "X-API-Key";
    [Id(4)] public string? Username { get; set; }
    [Id(5)] public string? Password { get; set; }
    [Id(6)] public OAuth2Config? OAuth2 { get; set; }
    [Id(7)] public Dictionary<string, string> CustomHeaders { get; set; } = new();
}

[GenerateSerializer]
public enum WebAPIAuthenticationType
{
    None,
    BearerToken,
    ApiKey,
    BasicAuth,
    OAuth2ClientCredentials,
    CustomHeaders
}

[GenerateSerializer]
public class OAuth2Config
{
    [Id(0)] public string ClientId { get; set; } = string.Empty;
    [Id(1)] public string ClientSecret { get; set; } = string.Empty;
    [Id(2)] public string TokenUrl { get; set; } = string.Empty;
    [Id(3)] public string Scope { get; set; } = string.Empty;
    [Id(4)] public string? AccessToken { get; set; }
    [Id(5)] public DateTime? TokenExpiresAt { get; set; }
}
```

#### 其他配置类
```csharp
[GenerateSerializer]
public class WebAPITimeoutConfig
{
    [Id(0)] public int RequestTimeoutSeconds { get; set; } = 30;
    [Id(1)] public int ConnectionTimeoutSeconds { get; set; } = 10;
}

[GenerateSerializer]
public class WebAPIRetryConfig
{
    [Id(0)] public bool EnableRetry { get; set; } = true;
    [Id(1)] public int MaxAttempts { get; set; } = 3;
    [Id(2)] public int BaseDelaySeconds { get; set; } = 1;
    [Id(3)] public int MaxDelaySeconds { get; set; } = 30;
    [Id(4)] public double BackoffMultiplier { get; set; } = 2.0;
    [Id(5)] public List<int> RetryableStatusCodes { get; set; } = new() { 429, 500, 502, 503, 504 };
}

[GenerateSerializer]
public class WebAPICacheConfig
{
    [Id(0)] public bool EnableCache { get; set; } = false;
    [Id(1)] public int DefaultCacheDurationSeconds { get; set; } = 300;
    [Id(2)] public int MaxCacheSize { get; set; } = 1000;
    [Id(3)] public List<string> CacheableEndpoints { get; set; } = new();
}

[GenerateSerializer]
public class WebAPIRateLimitConfig
{
    [Id(0)] public bool EnableRateLimit { get; set; } = false;
    [Id(1)] public int RequestsPerMinute { get; set; } = 60;
    [Id(2)] public int BurstSize { get; set; } = 10;
}
```

### 3. 响应和记录类型

#### 响应封装
```csharp
[GenerateSerializer]
public class WebAPIResponse<T>
{
    [Id(0)] public bool IsSuccess { get; set; }
    [Id(1)] public int StatusCode { get; set; }
    [Id(2)] public T? Data { get; set; }
    [Id(3)] public string? ErrorMessage { get; set; }
    [Id(4)] public Dictionary<string, string> Headers { get; set; } = new();
    [Id(5)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [Id(6)] public long ElapsedMilliseconds { get; set; }
    [Id(7)] public string RequestId { get; set; } = Guid.NewGuid().ToString();
}
```

#### 请求记录
```csharp
[GenerateSerializer]
public class WebAPIRequestRecord
{
    [Id(0)] public string RequestId { get; set; } = string.Empty;
    [Id(1)] public string Method { get; set; } = string.Empty;
    [Id(2)] public string Endpoint { get; set; } = string.Empty;
    [Id(3)] public DateTime RequestTime { get; set; }
    [Id(4)] public DateTime ResponseTime { get; set; }
    [Id(5)] public long ElapsedMilliseconds { get; set; }
    [Id(6)] public int StatusCode { get; set; }
    [Id(7)] public bool IsSuccess { get; set; }
    [Id(8)] public string? ErrorMessage { get; set; }
    [Id(9)] public int RetryCount { get; set; }
    [Id(10)] public Dictionary<string, string> RequestHeaders { get; set; } = new();
    [Id(11)] public Dictionary<string, string> ResponseHeaders { get; set; } = new();
}
```

#### 指标和健康状态
```csharp
[GenerateSerializer]
public class WebAPIMetrics
{
    [Id(0)] public long TotalRequests { get; set; }
    [Id(1)] public long SuccessfulRequests { get; set; }
    [Id(2)] public long FailedRequests { get; set; }
    [Id(3)] public double AverageResponseTimeMs { get; set; }
    [Id(4)] public long TotalRetries { get; set; }
    [Id(5)] public Dictionary<int, long> StatusCodeCounts { get; set; } = new();
    [Id(6)] public DateTime LastResetAt { get; set; } = DateTime.UtcNow;
}

[GenerateSerializer]
public class WebAPIHealthStatus
{
    [Id(0)] public bool IsHealthy { get; set; }
    [Id(1)] public string StatusMessage { get; set; } = string.Empty;
    [Id(2)] public DateTime LastCheckAt { get; set; }
    [Id(3)] public int ConsecutiveFailures { get; set; }
    [Id(4)] public Dictionary<string, object> HealthDetails { get; set; } = new();
}
```

### 4. 事件系统

#### 状态日志事件
```csharp
[GenerateSerializer]
public abstract record WebAPIStateLogEvent : StateLogEventBase<WebAPIStateLogEvent>;

[GenerateSerializer]
public record ConfigurationUpdatedLogEvent : WebAPIStateLogEvent
{
    [Id(0)] public string ConfigurationJson { get; init; } = string.Empty;
    [Id(1)] public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

[GenerateSerializer]
public record RequestExecutedLogEvent : WebAPIStateLogEvent
{
    [Id(0)] public WebAPIRequestRecord RequestRecord { get; init; } = new();
}

[GenerateSerializer]
public record MetricsUpdatedLogEvent : WebAPIStateLogEvent
{
    [Id(0)] public WebAPIMetrics Metrics { get; init; } = new();
}

[GenerateSerializer]
public record HealthStatusChangedLogEvent : WebAPIStateLogEvent
{
    [Id(0)] public WebAPIHealthStatus HealthStatus { get; init; } = new();
    [Id(1)] public WebAPIHealthStatus PreviousStatus { get; init; } = new();
}
```

#### 业务事件
```csharp
[GenerateSerializer]
public record WebAPIRequestEvent : EventBase
{
    [Id(0)] public string Method { get; init; } = string.Empty;
    [Id(1)] public string Endpoint { get; init; } = string.Empty;
    [Id(2)] public object? Payload { get; init; }
    [Id(3)] public Dictionary<string, string> Headers { get; init; } = new();
}

[GenerateSerializer]
public record WebAPIResponseEvent : EventBase
{
    [Id(0)] public string RequestId { get; init; } = string.Empty;
    [Id(1)] public int StatusCode { get; init; }
    [Id(2)] public bool IsSuccess { get; init; }
    [Id(3)] public long ElapsedMilliseconds { get; init; }
    [Id(4)] public string? ErrorMessage { get; init; }
}

[GenerateSerializer]
public record WebAPIErrorEvent : EventBase
{
    [Id(0)] public string RequestId { get; init; } = string.Empty;
    [Id(1)] public string ErrorType { get; init; } = string.Empty;
    [Id(2)] public string ErrorMessage { get; init; } = string.Empty;
    [Id(3)] public string Endpoint { get; init; } = string.Empty;
    [Id(4)] public int RetryCount { get; init; }
}

[GenerateSerializer]
public record WebAPIHealthCheckEvent : EventBase
{
    [Id(0)] public bool IsHealthy { get; init; }
    [Id(1)] public string StatusMessage { get; init; } = string.Empty;
    [Id(2)] public int ConsecutiveFailures { get; init; }
}
```

### 5. 认证处理器

#### 认证处理器接口
```csharp
public interface IWebAPIAuthenticationHandler
{
    Task<WebAPIAuthenticationResult> PrepareAuthenticationAsync(
        WebAPIAuthenticationRequest request,
        WebAPIAuthenticationConfig config,
        CancellationToken cancellationToken = default);
    
    Task<bool> RefreshTokenAsync(
        WebAPIAuthenticationConfig config,
        CancellationToken cancellationToken = default);
    
    bool SupportsAuthenticationType(WebAPIAuthenticationType type);
}

public class WebAPIAuthenticationRequest
{
    public HttpMethod Method { get; set; } = HttpMethod.Get;
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string>? Parameters { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class WebAPIAuthenticationResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> QueryParameters { get; set; } = new();
    public bool RequiresRefresh { get; set; }
}
```

### 6. HTTP客户端封装

#### 客户端接口
```csharp
public interface IWebAPIClient
{
    Task<WebAPIResponse<T>> SendRequestAsync<T>(
        HttpMethod method,
        string endpoint,
        object? payload = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
    
    Task<WebAPIResponse<string>> SendRequestAsync(
        HttpMethod method,
        string endpoint,
        object? payload = null,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
    
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
```

## 使用场景示例

### 1. REST API 调用
```csharp
// 配置
var config = new WebAPIGAgentConfiguration
{
    BaseUrl = "https://api.example.com/v1",
    Authentication = new WebAPIAuthenticationConfig
    {
        Type = WebAPIAuthenticationType.BearerToken,
        BearerToken = "your-token-here"
    },
    DefaultHeaders = new Dictionary<string, string>
    {
        ["Accept"] = "application/json",
        ["User-Agent"] = "MyApp/1.0"
    }
};

// 使用
var apiAgent = await gAgentFactory.GetGAgentAsync<IWebAPIGAgent>(Guid.NewGuid(), config);
var response = await apiAgent.GetAsync<UserData>("/users/123");
```

### 2. OAuth 2.0 API
```csharp
var config = new WebAPIGAgentConfiguration
{
    BaseUrl = "https://api.oauth-service.com",
    Authentication = new WebAPIAuthenticationConfig
    {
        Type = WebAPIAuthenticationType.OAuth2ClientCredentials,
        OAuth2 = new OAuth2Config
        {
            ClientId = "your-client-id",
            ClientSecret = "your-client-secret",
            TokenUrl = "https://auth.oauth-service.com/token",
            Scope = "api:read api:write"
        }
    }
};

var apiAgent = await gAgentFactory.GetGAgentAsync<IWebAPIGAgent>(Guid.NewGuid(), config);
var response = await apiAgent.PostAsync<CreateResponse>("/resources", new { name = "test" });
```

### 3. 第三方服务集成
```csharp
var config = new WebAPIGAgentConfiguration
{
    BaseUrl = "https://api.thirdparty.com",
    Authentication = new WebAPIAuthenticationConfig
    {
        Type = WebAPIAuthenticationType.ApiKey,
        ApiKey = "your-api-key",
        ApiKeyHeader = "X-API-Key"
    },
    Retry = new WebAPIRetryConfig
    {
        EnableRetry = true,
        MaxAttempts = 5,
        BaseDelaySeconds = 2
    },
    RateLimit = new WebAPIRateLimitConfig
    {
        EnableRateLimit = true,
        RequestsPerMinute = 100
    }
};
```

## 扩展点设计

### 1. 自定义认证处理器
开发者可以实现自定义认证逻辑：
```csharp
public class CustomAuthenticationHandler : IWebAPIAuthenticationHandler
{
    // 实现自定义认证逻辑
}
```

### 2. 请求/响应中间件
支持请求和响应的拦截处理：
```csharp
public interface IWebAPIMiddleware
{
    Task<WebAPIRequest> ProcessRequestAsync(WebAPIRequest request);
    Task<WebAPIResponse> ProcessResponseAsync(WebAPIResponse response);
}
```

### 3. 缓存策略
支持不同的缓存实现：
```csharp
public interface IWebAPICacheProvider
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    Task RemoveAsync(string key);
}
```

## 性能考虑

### 1. 连接复用
- 使用 HttpClientFactory 管理连接池
- 避免频繁创建 HttpClient 实例

### 2. 内存管理
- 限制请求历史记录数量
- 定期清理过期缓存
- 使用对象池减少 GC 压力

### 3. 并发控制
- 速率限制防止 API 滥用
- 信号量控制并发请求数

### 4. 序列化优化
- 使用 System.Text.Json 提高性能
- 支持流式序列化大响应

## 安全考虑

### 1. 敏感信息保护
- 配置中的密码和密钥应加密存储
- 日志中不记录敏感信息

### 2. SSL/TLS
- 强制 HTTPS 用于生产环境
- 支持证书验证自定义

### 3. 请求验证
- 输入验证防止注入攻击
- URL 白名单机制

## 监控和诊断

### 1. 指标收集
- 请求成功率
- 响应时间分布
- 错误率统计
- 重试次数

### 2. 日志记录
- 结构化日志格式
- 可配置的日志级别
- 请求/响应追踪

### 3. 健康检查
- 定期连通性测试
- 服务可用性监控
- 自动恢复机制

## 总结

WebAPI GAgent 设计提供了一个功能完整、易于使用、高度可配置的 HTTP API 客户端解决方案。通过模块化设计和事件驱动架构，它能够满足各种 Web API 集成需求，同时保持良好的可扩展性和维护性。
