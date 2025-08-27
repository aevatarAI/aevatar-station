# Aevatar Configuration System Design

## 核心设计理念

### 配置分离原则
- **系统配置**：共享的基础配置，通过模板和ConfigMap管理
- **业务配置**：用户自定义配置，通过HostConfigurationGAgent持久化存储
- **安全配置**：敏感数据通过环境变量和保护键机制管理

### 设计目标
1. **简洁性**：最小化复杂度，单一职责原则
2. **安全性**：敏感配置与公共配置分离，保护系统配置
3. **灵活性**：支持多环境部署，运行时配置更新
4. **一致性**：所有服务使用统一的配置管理模式

## 系统架构

### 配置层次结构 (优先级从高到低)
```
1. 环境变量 (标准.NET环境变量)
2. 共享配置文件 (appsettings.*.Shared.json) - 系统配置
3. 业务配置 (HostConfigurationGAgent) - 增量配置，不能覆盖系统配置
4. 默认配置
```

**重要说明**：业务配置只能做增量添加，不能覆盖系统配置中的现有键值，确保系统核心配置的安全性。

### 核心组件

#### 1. HostConfigurationGAgent
**用途**：用户业务配置的持久化存储

**标识策略**：
```
Grain Key = "{hostId}:{hostType}"
例子：MyApp001:Client, MyApp001:Silo, MyApp001:WebHook
```

**核心功能**：
- `SetBusinessConfigurationJsonAsync()` - 存储JSON配置（增量模式）
- `GetBusinessConfigurationJsonAsync()` - 获取JSON配置  
- `ClearBusinessConfigurationAsync()` - 清除配置

**特点**：
- 基于Orleans事件源模式
- 每个Host-Type组合独立存储
- **增量配置模式**：只能添加新配置项，不能覆盖系统配置
- 支持配置版本控制和审计

#### 2. 安全配置系统
**用途**：系统级配置的安全管理

**核心方法**：
```
AddAevatarSecureConfiguration(systemConfigPath, userConfigPath?)
```

**特性**：
- 自动fallback到标准JSON文件
- 标准环境变量覆盖支持（双下划线表示嵌套）
- 线程安全的配置加载
- 优雅的错误处理

#### 3. Kubernetes集成
**用途**：容器化部署的配置管理

**机制**：
- ConfigMap存储共享配置文件（系统配置）
- Secret存储敏感配置
- 环境变量注入业务配置
- 模板系统支持配置生成
- **配置合并**：业务配置以增量方式与系统配置合并

## 配置流程

### 1. 系统启动配置加载
```
Program.cs启动 
→ AddAevatarSecureConfiguration()加载共享配置
→ AddEnvironmentVariables()加载标准环境变量
→ 配置验证和处理
```

### 2. 业务配置管理流程
```
用户上传JSON配置 
→ BusinessConfigController接收
→ HostConfigurationGAgent持久化存储
→ KubernetesHostManager更新ConfigMap
→ Pod重新加载配置
```

### 3. 部署时配置集成
```
KubernetesHostManager创建Host
→ 从模板生成基础配置（系统配置）
→ 从HostConfigurationGAgent获取业务配置
→ 增量合并生成最终ConfigMap（业务配置不覆盖系统配置）
→ 创建Pod with配置挂载
```

## 配置文件结构

### 系统配置文件
- `appsettings.Shared.json` - 所有服务共享的基础配置
- `appsettings.{ServiceName}.Host.Shared.json` - 服务特定配置
- `business-config.json` - 业务配置（由Agent动态生成）

### 环境变量映射（可覆盖所有配置）
```
配置路径：MongoDB:ConnectionString
环境变量：MongoDB__ConnectionString

配置路径：Features:Cache:Enabled  
环境变量：Features__Cache__Enabled
```

### 业务配置示例（增量配置）
```json
{
  "MyApp": {
    "ApiUrl": "https://api.example.com",
    "Timeout": 30
  },
  "Features": {
    "EnableNewFeature": true
  }
}
```
**注意**：业务配置不能包含系统保护键，如连接字符串、日志配置等。

## 安全机制

### 1. 保护键机制
- 复用现有`ProtectedKeyConfigurationProvider.GetProtectedKeys()`
- **严格禁止**用户通过业务配置覆盖系统敏感配置
- 在业务配置设置时验证键名，拒绝保护键
- 系统配置优先级高于业务配置

### 2. 配置分离与合并策略
- **系统配置**：通过模板和共享文件管理，具有最高优先级
- **业务配置**：通过Agent独立存储，只能增量添加
- **敏感配置**：通过环境变量注入，可覆盖所有配置
- **合并规则**：系统配置 + 增量业务配置 + 环境变量覆盖

### 3. 访问控制
- 业务配置API需要认证授权
- Agent操作记录审计日志
- 配置更新支持操作者追踪

## 服务集成模式

### 所有Host服务统一使用模式
```csharp
builder.Configuration

    .AddAevatarSecureConfiguration(
      new[]
                {
                   systemConfigPath,serviceSpecificConfigPath
                })
    .AddEnvironmentVariables();
```

### 适用服务
- Aevatar.HttpApi.Host
- Aevatar.Silo  
- Aevatar.Developer.Host
- Aevatar.WebHook.Host
- Aevatar.Worker
- Aevatar.AuthServer

## 部署场景

### 1. 开发环境
- 本地配置文件
- 环境变量覆盖
- 实时配置调试

### 2. 容器部署
- 配置文件作为Volume挂载
- 敏感数据通过标准环境变量
- 多环境配置支持

### 3. Kubernetes部署  
- ConfigMap管理共享配置
- Secret管理敏感配置
- 支持配置热更新

## 工作流程条件化配置打包

### 设计需求
不同CI/CD工作流程对业务配置的包含需求不同：
- **回归测试环境**：需要包含业务配置进行完整功能测试
- **生产部署环境**：应排除预置业务配置，避免与运行时配置冲突

### 解决方案：环境变量条件化控制

#### 1. 代码层面控制
通过修改 `AddAevatarSecureConfiguration` 扩展方法，实现环境变量控制：

```csharp
public static IConfigurationBuilder AddAevatarSecureConfiguration(
    this IConfigurationBuilder builder,
    string systemConfigPath,
    string businessConfigPath = "appsettings.business.json",
    bool optional = true)
{
    // 业务配置默认支持，使用动态保护键机制防止覆盖系统配置
    // 1. 加载默认 appsettings.json（必需）
    // 2. 加载系统配置（可选）
    // 3. 加载业务配置（可选）
    // 4. 加载临时配置（仅在环境变量 ENABLE_EPHEMERAL_CONFIG=true 时）
    return builder.AddAevatarSecureConfiguration(systemConfigPath, businessConfigPath, optional: optional);
}
```

#### 2. 工作流程差异化配置

**回归测试工作流程** (regression-test-ephemeral-env.yaml)：
```yaml
- name: Build and push
  uses: docker/build-push-action@v5
  with:
    context: .
    push: true
    build-args: |
      servicename=${{ matrix.servicename }}
    platforms: linux/amd64
    tags: ${{ steps.meta.outputs.tags }}
    labels: ${{ steps.meta.outputs.labels }}
  env:
    ENABLE_EPHEMERAL_CONFIG: "true"  # 启用临时配置覆盖
```

**生产部署工作流程** (package-staging.yml)：
```yaml
- name: Build and push
  uses: docker/build-push-action@v5
  with:
    context: .
    push: true
    build-args: |
      servicename=${{ matrix.servicename }}
    platforms: linux/amd64
    tags: ${{ steps.meta.outputs.tags }}
    labels: ${{ steps.meta.outputs.labels }}
  # 不设置 ENABLE_EPHEMERAL_CONFIG，业务配置启用但无法覆盖系统配置
```

#### 3. 配置分离策略
- **默认业务配置支持**：业务配置默认启用，使用动态保护键机制确保安全
- **临时配置控制**：通过 `ENABLE_EPHEMERAL_CONFIG` 环境变量控制是否允许临时配置覆盖
- **统一镜像**：同一镜像可用于测试和生产，通过环境变量区分行为
- **向后兼容**：保持原有配置文件约定和加载顺序

#### 4. 最佳实践
- **环境变量控制**：
  - EPHEMERAL测试环境：设置 `ENABLE_EPHEMERAL_CONFIG=true` 启用临时配置覆盖
  - 生产环境：不设置该变量，业务配置默认启用但不允许覆盖系统配置
- **配置文件约定**：
  - 系统配置：`appsettings.{host}.json`（如 `appsettings.Station.json`）
  - 业务配置：`appsettings.business.json`（默认支持）
  - 临时配置：`appsettings.ephemeral.json`（仅测试环境启用）
- **系统配置优先**：业务配置永远不能覆盖系统保护键
- **运行时动态**：生产环境通过HostConfigurationGAgent动态管理业务配置

#### 5. 服务覆盖范围
所有Host服务均自动支持该机制：
- Aevatar.HttpApi.Host
- Aevatar.Silo  
- Aevatar.Developer.Host
- Aevatar.WebHook.Host
- Aevatar.Worker
- Aevatar.AuthServer

### 集成优势
- **零维护成本**：无需修改Dockerfile或各服务代码
- **统一控制**：单一环境变量控制所有服务行为
- **生产安全**：生产环境默认禁用静态业务配置
- **测试便利**：测试环境可轻松启用预置配置

## 优势总结

### 技术优势
- **性能**：基于Grain Key的O(1)访问，天然分片
- **可靠性**：Orleans事件源保证数据持久化
- **扩展性**：支持水平扩展和功能扩展

### 运维优势
- **部署灵活**：支持多种部署模式
- **配置管理**：集中化配置管理
- **故障恢复**：配置版本控制和回滚支持

### 安全优势
- **权限控制**：分层的配置访问控制
- **数据分离**：敏感配置与业务配置分离  
- **增量安全**：业务配置只能增加，不能覆盖系统配置
- **审计支持**：完整的配置变更审计链

## 实现状态

### 已完成
✅ 核心Agent实现（HostConfigurationGAgent）  
✅ 安全配置系统（AevatarSecureConfigurationExtensions）  
✅ 所有Host服务集成  
✅ Kubernetes部署集成  
✅ 业务配置API（BusinessConfigController）  
✅ ConfigMap自动更新机制  

### 系统成熟度
- **架构设计**：完整且经过验证
- **代码实现**：核心功能已实现
- **集成测试**：通过编译和基础功能验证
- **生产就绪**：支持实际部署使用

这个配置系统实现了简洁、安全、灵活的设计目标，为Aevatar Station平台提供了统一、可靠的配置管理能力。