# MCP Gateway Troubleshooting Guide

## English Documentation

### Common Issues and Solutions

#### **Connection Issues**

##### Issue: Gateway Connection Timeout
**Symptoms:**
- MCPGAgent fails to connect to gateway
- Timeout exceptions in logs
- Connection state shows "Failed"

**Solutions:**
1. **Increase timeout values:**
   ```json
   {
     "MCPGateway": {
       "RequestTimeout": "00:02:00"
     }
   }
   ```

2. **Check network connectivity:**
   ```bash
   # Test gateway accessibility
   curl -v https://your-gateway.com/health
   
   # Check DNS resolution
   nslookup your-gateway.com
   ```

3. **Verify gateway status:**
   ```bash
   kubectl get pods -n adapter
   kubectl logs -n adapter deployment/mcpgateway-service
   ```

##### Issue: Authentication Failures
**Symptoms:**
- 401 Unauthorized responses
- "Authentication failed" error messages
- Gateway rejecting requests

**Solutions:**
1. **Verify token format:**
   ```csharp
   // Ensure token includes Bearer prefix
   "AuthToken": "Bearer your-actual-token"
   ```

2. **Test token validity:**
   ```bash
   # Test with curl
   curl -H "Authorization: Bearer your-token" https://gateway.com/health
   ```

3. **Check token expiration:**
   ```bash
   # For Azure tokens
   az account get-access-token --resource your-client-id
   ```

##### Issue: Session Routing Problems
**Symptoms:**
- Inconsistent responses from MCP tools
- Session state not preserved
- Connection to different server instances

**Solutions:**
1. **Enable session affinity:**
   ```json
   {
     "MCPGateway": {
       "EnableSessionAffinity": true
     }
   }
   ```

2. **Verify session ID generation:**
   ```csharp
   // Check MCPGAgentState for SessionId
   var state = await agent.GetStateAsync();
   Logger.LogInformation("Session ID: {SessionId}", state.SessionId);
   ```

3. **Monitor session headers:**
   ```json
   {
     "MCPGateway": {
       "EnableDetailedLogging": true
     }
   }
   ```

#### **Adapter Management Issues**

##### Issue: Adapter Creation Fails
**Symptoms:**
- 400 Bad Request when creating adapters
- Validation errors
- Conflict responses

**Solutions:**
1. **Validate input parameters:**
   ```csharp
   var input = new CreateMCPAdapterDto
   {
       Name = "valid-adapter-name", // Must be lowercase alphanumeric with hyphens
       ImageName = "valid-image-name",
       ImageVersion = "1.0.0"
   };
   ```

2. **Check for naming conflicts:**
   ```bash
   # List existing adapters
   curl https://gateway.com/adapters
   ```

3. **Verify resource limits:**
   ```json
   {
     "resourceLimits": {
       "cpuLimit": 1.0,        // 0.1 - 32.0 cores
       "memoryLimitMB": 512,   // 64 - 32768 MB
       "maxConnections": 100   // 1 - 10000
     }
   }
   ```

##### Issue: Adapter Status Shows as Failed
**Symptoms:**
- Adapter status is "Failed"
- Health checks failing
- No active connections

**Solutions:**
1. **Check adapter logs:**
   ```bash
   curl https://gateway.com/adapters/my-adapter/logs?lines=50
   ```

2. **Verify image availability:**
   ```bash
   # Check if image exists in registry
   docker pull your-registry/mcp-server:1.0.0
   ```

3. **Review resource allocation:**
   ```bash
   kubectl describe pod -n adapter -l app=my-adapter
   ```

#### **Performance Issues**

##### Issue: High Latency
**Symptoms:**
- Slow response times
- Gateway timeouts
- Poor user experience

**Solutions:**
1. **Optimize connection pooling:**
   ```csharp
   services.AddHttpClient<MCPGatewayManager>(client =>
   {
       client.Timeout = TimeSpan.FromSeconds(30);
       client.DefaultRequestHeaders.ConnectionClose = false;
   });
   ```

2. **Implement caching:**
   ```csharp
   [MemoryCache(Duration = 300)] // 5 minutes
   public async Task<List<MCPAdapterDto>> GetAdaptersAsync()
   ```

3. **Monitor gateway metrics:**
   ```bash
   curl https://gateway.com/adapters/my-adapter/metrics
   ```

##### Issue: High Memory Usage
**Symptoms:**
- OutOfMemoryException
- Slow garbage collection
- Pod restarts

**Solutions:**
1. **Adjust resource limits:**
   ```json
   {
     "resourceLimits": {
       "memoryLimitMB": 1024
     }
   }
   ```

2. **Implement connection cleanup:**
   ```csharp
   public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
   {
       await _mcpClient?.DisposeAsync();
       await base.OnDeactivateAsync(reason, cancellationToken);
   }
   ```

### Debugging Tools

#### **Enable Detailed Logging**

```json
{
  "Logging": {
    "LogLevel": {
      "Aevatar.Application.MCPGateway": "Debug",
      "Aevatar.GAgents.MCP": "Debug"
    }
  },
  "MCPGateway": {
    "EnableDetailedLogging": true
  }
}
```

#### **Monitor Connection States**

```csharp
public async Task<Dictionary<string, object>> GetConnectionDiagnosticsAsync()
{
    var state = await GetStateAsync();
    return new Dictionary<string, object>
    {
        ["ConnectionType"] = state.ConnectionType,
        ["SessionId"] = state.SessionId,
        ["LastConnected"] = state.LastConnected,
        ["RetryCount"] = state.RetryCount,
        ["LastError"] = state.LastConnectionError
    };
}
```

#### **Health Check Endpoints**

```bash
# Gateway health
curl https://gateway.com/health

# Adapter health
curl https://gateway.com/adapters/my-adapter/status

# Connection test
curl -X POST https://gateway.com/adapters/my-adapter/test
```

---

## 中文文档

### 概述

MCP Gateway集成提供了一个全面的权限系统，允许对适配器管理、监控和网关操作进行细粒度访问控制。

### 常见问题和解决方案

#### **连接问题**

##### 问题：网关连接超时
**症状：**
- MCPGAgent无法连接到网关
- 日志中出现超时异常
- 连接状态显示"Failed"

**解决方案：**
1. **增加超时值：**
   ```json
   {
     "MCPGateway": {
       "RequestTimeout": "00:02:00"
     }
   }
   ```

2. **检查网络连接性：**
   ```bash
   # 测试网关可访问性
   curl -v https://your-gateway.com/health
   
   # 检查DNS解析
   nslookup your-gateway.com
   ```

##### 问题：身份验证失败
**症状：**
- 401未授权响应
- "身份验证失败"错误消息
- 网关拒绝请求

**解决方案：**
1. **验证令牌格式：**
   ```csharp
   // 确保令牌包含Bearer前缀
   "AuthToken": "Bearer your-actual-token"
   ```

2. **测试令牌有效性：**
   ```bash
   # 使用curl测试
   curl -H "Authorization: Bearer your-token" https://gateway.com/health
   ```

##### 问题：会话路由问题
**症状：**
- MCP工具响应不一致
- 会话状态未保持
- 连接到不同的服务器实例

**解决方案：**
1. **启用会话亲和性：**
   ```json
   {
     "MCPGateway": {
       "EnableSessionAffinity": true
     }
   }
   ```

2. **验证会话ID生成：**
   ```csharp
   // 检查MCPGAgentState中的SessionId
   var state = await agent.GetStateAsync();
   Logger.LogInformation("会话ID: {SessionId}", state.SessionId);
   ```

#### **适配器管理问题**

##### 问题：适配器创建失败
**症状：**
- 创建适配器时出现400错误请求
- 验证错误
- 冲突响应

**解决方案：**
1. **验证输入参数：**
   ```csharp
   var input = new CreateMCPAdapterDto
   {
       Name = "valid-adapter-name", // 必须是小写字母数字加连字符
       ImageName = "valid-image-name",
       ImageVersion = "1.0.0"
   };
   ```

2. **检查命名冲突：**
   ```bash
   # 列出现有适配器
   curl https://gateway.com/adapters
   ```

##### 问题：适配器状态显示为失败
**症状：**
- 适配器状态为"Failed"
- 健康检查失败
- 无活动连接

**解决方案：**
1. **检查适配器日志：**
   ```bash
   curl https://gateway.com/adapters/my-adapter/logs?lines=50
   ```

2. **验证镜像可用性：**
   ```bash
   # 检查注册表中是否存在镜像
   docker pull your-registry/mcp-server:1.0.0
   ```

### 调试工具

#### **启用详细日志记录**

```json
{
  "Logging": {
    "LogLevel": {
      "Aevatar.Application.MCPGateway": "Debug",
      "Aevatar.GAgents.MCP": "Debug"
    }
  },
  "MCPGateway": {
    "EnableDetailedLogging": true
  }
}
```

#### **监控连接状态**

```csharp
public async Task<Dictionary<string, object>> GetConnectionDiagnosticsAsync()
{
    var state = await GetStateAsync();
    return new Dictionary<string, object>
    {
        ["ConnectionType"] = state.ConnectionType,
        ["SessionId"] = state.SessionId,
        ["LastConnected"] = state.LastConnected,
        ["RetryCount"] = state.RetryCount,
        ["LastError"] = state.LastConnectionError
    };
}
```

#### **健康检查端点**

```bash
# 网关健康状态
curl https://gateway.com/health

# 适配器健康状态
curl https://gateway.com/adapters/my-adapter/status

# 连接测试
curl -X POST https://gateway.com/adapters/my-adapter/test
```

### 性能调优

#### **连接优化**
1. 使用连接池优化HTTP客户端
2. 实现适当的缓存策略
3. 监控网关延迟并调整超时
4. 一致使用异步模式

#### **资源管理**
1. 适当设置资源限制
2. 监控CPU和内存使用情况
3. 实现连接清理机制
4. 配置基于指标的自动扩展
