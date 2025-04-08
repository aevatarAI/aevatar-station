# Aevatar测试指南

本文档提供了关于Aevatar项目测试的详细说明，包括如何运行各种类型的测试以及如何设置测试环境。

## 测试配置

Aevatar项目遵循.NET配置最佳实践，使用环境特定的配置文件：

- `appsettings.json` - 基础配置文件，包含所有环境通用的设置
- `appsettings.Testing.json` - 测试环境的通用配置（默认使用内存/模拟数据访问）
- `appsettings.MongoDB.json` - MongoDB特定的配置，只在MongoDB测试时加载

测试基类 `AevatarTestBase<T>` 通过 `ShouldUseMongoDB()` 方法决定是否需要加载MongoDB配置。
MongoDB测试类通过重写此方法明确指定需要使用MongoDB。

## 运行测试

### 通用单元测试

对于不依赖实际数据库的测试，可以直接运行：

```bash
dotnet test
```

或者针对特定项目：

```bash
dotnet test test/Aevatar.Domain.Tests
```

### MongoDB相关测试

MongoDB测试需要一个可用的MongoDB实例。有两种方式可以运行这些测试：

1. **使用Mongo2Go（仅限单元测试）**：这种方式会自动创建一个临时的MongoDB实例，不需要任何额外配置。

2. **使用Docker容器**：这种方式更接近生产环境，通过以下脚本管理：

```bash
# 启动MongoDB容器
./mongodb-test.sh start

# 运行MongoDB测试
./mongodb-test.sh run-test  # 运行特定测试
./mongodb-test.sh run-all   # 运行所有MongoDB测试

# 检查MongoDB状态
./mongodb-test.sh status

# 测试MongoDB连接
./mongodb-test.sh test-conn

# 停止并清理MongoDB容器
./mongodb-test.sh stop
```

## MongoDB测试设置

### 手动设置环境变量

如果您想手动运行测试并明确指定使用MongoDB，请设置以下环境变量：

```bash
export DOTNET_ENVIRONMENT=Testing
export USE_MONGODB=true
```

然后运行测试：

```bash
dotnet test test/Aevatar.MongoDB.Tests
```

### MongoDB连接信息

使用Docker容器时的MongoDB连接信息：

- 主机: localhost
- 端口: 27017
- 用户名: admin
- 密码: admin
- 认证数据库: admin

连接字符串：`mongodb://admin:admin@localhost:27017/?authSource=admin`

## 测试类型

项目包含以下几种类型的测试：

1. **领域测试**：测试核心领域逻辑，不依赖数据库
2. **应用服务测试**：测试应用服务功能，使用模拟数据访问
3. **MongoDB测试**：测试数据访问层与MongoDB的交互
4. **CQRS测试**：测试命令与查询功能
5. **集成测试**：测试系统各部分的集成

## 特殊测试场景

### Orleans测试

Orleans测试需要特殊的设置，使用 `ClusterFixture` 创建一个本地Orleans集群。相关配置在 `AevatarOrleansTestBaseModule` 中。

### HTTP API测试

API测试使用 `WebApplicationFactory` 创建一个测试服务器。相关配置在 `AevatarWebTestStartup` 中。 