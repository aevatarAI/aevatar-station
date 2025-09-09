# MCP Gateway Permissions Guide

## English Documentation

### Overview

The MCP Gateway integration provides a comprehensive permission system that allows fine-grained access control over adapter management, monitoring, and gateway operations.

### Permission Hierarchy

```
MCPGateway (Root Group)
├── Adapters
│   ├── Create          - Create new adapters
│   ├── Read            - View adapter information
│   ├── Update          - Modify existing adapters
│   ├── Delete          - Remove adapters
│   ├── ManageAll       - Full adapter management (super admin)
│   ├── ViewMetrics     - Access adapter performance metrics
│   ├── TestConnection  - Test adapter connectivity
│   └── ViewLogs        - Access adapter logs
├── Gateway
│   ├── ViewHealth      - View gateway health status
│   ├── ViewConfiguration - View gateway configuration
│   ├── UpdateConfiguration - Modify gateway settings
│   ├── ViewSystemMetrics - Access system-wide metrics
│   ├── Manage          - Full gateway management
│   └── ViewAuditLogs   - Access audit logs
└── Sessions
    ├── View            - View active sessions
    ├── Terminate       - End user sessions
    ├── ViewDetails     - Access session details
    └── ManageRouting   - Configure session routing
```

### Permission Constants

```csharp
// Adapter permissions
public const string ADAPTERS_CREATE = "MCPGateway.Adapters.Create";
public const string ADAPTERS_READ = "MCPGateway.Adapters.Read";
public const string ADAPTERS_UPDATE = "MCPGateway.Adapters.Update";
public const string ADAPTERS_DELETE = "MCPGateway.Adapters.Delete";
public const string ADAPTERS_MANAGE_ALL = "MCPGateway.Adapters.ManageAll";
public const string ADAPTERS_VIEW_METRICS = "MCPGateway.Adapters.ViewMetrics";
public const string ADAPTERS_TEST_CONNECTION = "MCPGateway.Adapters.TestConnection";
public const string ADAPTERS_VIEW_LOGS = "MCPGateway.Adapters.ViewLogs";

// Gateway permissions
public const string GATEWAY_VIEW_HEALTH = "MCPGateway.Gateway.ViewHealth";
public const string GATEWAY_VIEW_CONFIGURATION = "MCPGateway.Gateway.ViewConfiguration";
public const string GATEWAY_UPDATE_CONFIGURATION = "MCPGateway.Gateway.UpdateConfiguration";
public const string GATEWAY_VIEW_SYSTEM_METRICS = "MCPGateway.Gateway.ViewSystemMetrics";
public const string GATEWAY_MANAGE = "MCPGateway.Gateway.Manage";
public const string GATEWAY_VIEW_AUDIT_LOGS = "MCPGateway.Gateway.ViewAuditLogs";

// Session permissions
public const string SESSIONS_VIEW = "MCPGateway.Sessions.View";
public const string SESSIONS_TERMINATE = "MCPGateway.Sessions.Terminate";
public const string SESSIONS_VIEW_DETAILS = "MCPGateway.Sessions.ViewDetails";
public const string SESSIONS_MANAGE_ROUTING = "MCPGateway.Sessions.ManageRouting";
```

### Predefined Roles

#### **MCP Gateway Administrator**
Full access to all MCP Gateway operations:
```csharp
var adminPermissions = MCPGatewayPermissions.DefaultRolePermissions.Administrator;
// Includes all permissions listed above
```

#### **MCP Gateway Operator**
Operational access without destructive operations:
```csharp
var operatorPermissions = new[]
{
    MCPGatewayPermissions.Adapters.Read,
    MCPGatewayPermissions.Adapters.Create,
    MCPGatewayPermissions.Adapters.Update,
    MCPGatewayPermissions.Adapters.ViewMetrics,
    MCPGatewayPermissions.Adapters.TestConnection,
    MCPGatewayPermissions.Gateway.ViewHealth,
    MCPGatewayPermissions.Gateway.ViewConfiguration,
    MCPGatewayPermissions.Gateway.ViewSystemMetrics,
    MCPGatewayPermissions.Sessions.View,
    MCPGatewayPermissions.Sessions.ViewDetails
};
```

#### **MCP Gateway Viewer**
Read-only access for monitoring:
```csharp
var viewerPermissions = new[]
{
    MCPGatewayPermissions.Adapters.Read,
    MCPGatewayPermissions.Adapters.ViewMetrics,
    MCPGatewayPermissions.Gateway.ViewHealth,
    MCPGatewayPermissions.Gateway.ViewConfiguration,
    MCPGatewayPermissions.Gateway.ViewSystemMetrics,
    MCPGatewayPermissions.Sessions.View,
    MCPGatewayPermissions.Sessions.ViewDetails
};
```

#### **MCP Gateway Developer**
Development-focused permissions:
```csharp
var developerPermissions = new[]
{
    MCPGatewayPermissions.Adapters.Read,
    MCPGatewayPermissions.Adapters.Create,
    MCPGatewayPermissions.Adapters.Update,
    MCPGatewayPermissions.Adapters.ViewMetrics,
    MCPGatewayPermissions.Adapters.TestConnection,
    MCPGatewayPermissions.Adapters.ViewLogs,
    MCPGatewayPermissions.Gateway.ViewHealth,
    MCPGatewayPermissions.Gateway.ViewSystemMetrics,
    MCPGatewayPermissions.Sessions.View
};
```

### Permission Implementation

#### **Controller Authorization**

```csharp
[Authorize(MCPGatewayPermissions.Adapters.Create)]
public async Task<MCPAdapterDto> CreateAdapterAsync(CreateMCPAdapterDto input)
{
    // Implementation
}

[Authorize(MCPGatewayPermissions.Adapters.ViewLogs)]
public async Task<List<string>> GetAdapterLogsAsync(string name)
{
    // Implementation
}
```

#### **Service Layer Authorization**

```csharp
[Authorize(MCPGatewayPermissions.Gateway.ViewHealth)]
public async Task<MCPGatewayHealthDto> GetGatewayHealthAsync()
{
    // Implementation
}
```

### Custom Permission Configuration

#### **Adding Custom Permissions**

```csharp
public static class CustomMCPPermissions
{
    public const string GroupName = "CustomMCP";
    
    public static class Analytics
    {
        public const string Default = GroupName + ".Analytics";
        public const string ViewAdvanced = Default + ".ViewAdvanced";
        public const string Export = Default + ".Export";
        public const string Configure = Default + ".Configure";
    }
}

// Register in permission definition provider
var customGroup = context.AddGroup(CustomMCPPermissions.GroupName, L("Permission:CustomMCP"));
var analyticsPermission = customGroup.AddPermission(CustomMCPPermissions.Analytics.Default, L("Permission:Analytics"));
analyticsPermission.AddChild(CustomMCPPermissions.Analytics.ViewAdvanced, L("Permission:Analytics.ViewAdvanced"));
```

#### **Role-Based Assignment**

```csharp
// Assign permissions to roles programmatically
var roleManager = GetRequiredService<IRoleManager>();
var role = await roleManager.FindByNameAsync("MCPAnalyst");

if (role != null)
{
    await _permissionManager.SetAsync(
        CustomMCPPermissions.Analytics.ViewAdvanced,
        RolePermissionValueProvider.ProviderName,
        role.Id.ToString(),
        true);
}
```

---

## 中文文档

### 概述

MCP Gateway集成提供了一个全面的权限系统，允许对适配器管理、监控和网关操作进行细粒度访问控制。

### 权限层次结构

```
MCPGateway（根组）
├── Adapters（适配器）
│   ├── Create（创建）         - 创建新适配器
│   ├── Read（读取）           - 查看适配器信息
│   ├── Update（更新）         - 修改现有适配器
│   ├── Delete（删除）         - 移除适配器
│   ├── ManageAll（全部管理）   - 完整适配器管理（超级管理员）
│   ├── ViewMetrics（查看指标） - 访问适配器性能指标
│   ├── TestConnection（测试连接）- 测试适配器连接性
│   └── ViewLogs（查看日志）    - 访问适配器日志
├── Gateway（网关）
│   ├── ViewHealth（查看健康状态）        - 查看网关健康状态
│   ├── ViewConfiguration（查看配置）     - 查看网关配置
│   ├── UpdateConfiguration（更新配置）   - 修改网关设置
│   ├── ViewSystemMetrics（查看系统指标） - 访问系统级指标
│   ├── Manage（管理）                   - 完整网关管理
│   └── ViewAuditLogs（查看审计日志）     - 访问审计日志
└── Sessions（会话）
    ├── View（查看）             - 查看活动会话
    ├── Terminate（终止）        - 结束用户会话
    ├── ViewDetails（查看详情）   - 访问会话详情
    └── ManageRouting（管理路由） - 配置会话路由
```

### 预定义角色

#### **MCP Gateway管理员**
对所有MCP Gateway操作的完全访问权限

#### **MCP Gateway操作员**
操作访问权限，不包括破坏性操作

#### **MCP Gateway查看者**
用于监控的只读访问权限

#### **MCP Gateway开发者**
面向开发的权限集合

### 权限实现示例

```csharp
[Authorize(MCPGatewayPermissions.Adapters.Create)]
public async Task<MCPAdapterDto> CreateAdapterAsync(CreateMCPAdapterDto input)
{
    // 创建适配器的实现
}

[Authorize(MCPGatewayPermissions.Adapters.ViewLogs)]
public async Task<List<string>> GetAdapterLogsAsync(string name)
{
    // 获取适配器日志的实现
}
```
