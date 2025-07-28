# Aevatar.Aspire - 分布式微服务编排平台

## 🚀 简介

**Aevatar.Aspire** 是基于 .NET Aspire 构建的现代化分布式微服务编排平台，专为简化复杂微服务架构的开发、部署和运维而设计。它集成了完整的可观测性工具链和智能化的服务编排能力，让开发者能够专注于业务逻辑的实现，而不是基础设施的管理。

通过 Aevatar.Aspire，你可以一键启动包含 14 个组件的完整微服务生态系统，享受企业级的开发体验。

## 📋 目录 / Table of Contents

- [🚀 简介](#-简介)
- [✨ 通过这个项目你能做什么](#-通过这个项目你能做什么)
  - [🎯 统一服务编排](#-统一服务编排)
  - [📊 完整可观测性](#-完整可观测性)  
  - [🔧 开发效率提升](#-开发效率提升)
  - [🏗️ 企业级架构支持](#️-企业级架构支持)
- [🚀 快速开始](#-快速开始)
  - [前置要求](#前置要求)
  - [安装 Aspire 工作负载](#安装-aspire-工作负载)
  - [一键启动（推荐）](#一键启动推荐)
  - [验证安装](#验证安装)
- [🏗️ 架构概览](#️-架构概览)
  - [整体架构图](#整体架构图)
  - [核心组件说明](#核心组件说明)
  - [技术特色](#技术特色)
- [📚 详细使用指南](#-详细使用指南)
  - [日常开发工作流](#日常开发工作流)
  - [高级启动选项](#高级启动选项)
  - [性能优化建议](#性能优化建议)
- [🛠️ 故障排除](#️-故障排除)
  - [常见问题](#常见问题)
  - [调试工具](#调试工具)
- [🎓 进阶使用](#-进阶使用)
  - [自动化启动脚本](#自动化启动脚本)
  - [多环境配置](#多环境配置)
  - [监控告警配置](#监控告警配置)
- [📞 技术支持](#-技术支持)
- [📈 版本历史](#-版本历史)

---

## ✨ 通过这个项目你能做什么

### 🎯 统一服务编排
- **一键启动** - 单个命令启动所有微服务和基础设施组件
- **智能依赖管理** - 自动处理服务间的启动顺序和依赖关系
- **服务发现** - 内置服务注册与发现机制
- **配置管理** - 统一的环境变量和配置注入

### 📊 完整可观测性
- **分布式链路追踪** - 使用 Jaeger 追踪请求在微服务间的完整调用链路
- **实时监控** - 通过 Prometheus + Grafana 监控系统性能指标
- **日志聚合** - 统一的日志收集和查看界面
- **健康检查** - 实时监控所有服务的健康状态

### 🔧 开发效率提升
- **热重载** - 代码修改后自动重载，无需重启整个系统
- **API 文档** - 自动生成的 Swagger UI 接口文档
- **调试友好** - 内置多个 Dashboard 便于开发调试
- **环境一致性** - 开发、测试、生产环境配置统一

### 🏗️ 企业级架构支持
- **Orleans 集群** - 分布式处理引擎，支持横向扩展
- **事件驱动** - 基于 Kafka 的异步事件处理机制
- **数据存储** - 多种数据存储方案（MongoDB、Redis、Elasticsearch）
- **AI 能力** - 集成 Qdrant 向量数据库支持 AI 应用

---

[⬆️ 返回目录](#-目录--table-of-contents)

## 🚀 快速开始

### 前置要求

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/install)

### 安装 Aspire 工作负载

```bash
dotnet workload install aspire
```

### 一键启动（推荐）

```bash
# 1. 启动基础设施
cd src/Aevatar.Aspire
docker-compose up -d

# 2. 初始化数据库（首次运行）
cd ../Aevatar.DBMigrator
dotnet run

# 3. 配置 Orleans 网络（macOS 用户）
sudo ifconfig lo0 alias 127.0.0.2
sudo ifconfig lo0 alias 127.0.0.3

# 4. 启动所有服务
cd ../Aevatar.Aspire
dotnet run
```

### 验证安装

启动成功后，你可以访问以下地址验证系统状态：

- **Aspire Dashboard**: https://localhost:18888 - 总控制台
- **API 文档**: http://localhost:7002/swagger - 核心 API
- **开发者工具**: http://localhost:7003/swagger - 开发者 API
- **Orleans 监控**: http://localhost:8080 - 分布式系统监控
- **链路追踪**: http://localhost:16686 - 请求链路分析

---

[⬆️ 返回目录](#-目录--table-of-contents)

## 🏗️ 架构概览

### 整体架构图

```
┌─────────────────────────────────────────────────────────────────┐
│                     Aevatar.Aspire 编排层                        │
├─────────────────────────────────────────────────────────────────┤
│  应用服务层                                                        │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐              │
│  │  AuthServer  │ │ HttpApi.Host │ │Developer.Host│              │
│  │   (7001)     │ │   (7002)     │ │   (7003)     │              │
│  └──────────────┘ └──────────────┘ └──────────────┘              │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                Orleans 分布式集群                            │ │
│  │  Scheduler Silo  │  Projector Silo  │  User Silos...       │ │
│  │   (127.0.0.2)    │   (127.0.0.3)    │  (动态分配)           │ │
│  └─────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│  基础设施层                                                        │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐            │
│  │ MongoDB  │ │  Redis   │ │Elasticsearch│ │  Kafka   │            │
│  │ (27017)  │ │ (6379)   │ │  (9200)    │ │ (9092)   │            │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘            │
│                                                                   │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐            │
│  │ Qdrant   │ │ Jaeger   │ │Prometheus│ │ Grafana  │            │
│  │(6333/34) │ │ (16686)  │ │ (9091)   │ │ (3000)   │            │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘            │
└─────────────────────────────────────────────────────────────────┘
```

### 核心组件说明

#### 🎯 应用服务层

| 服务名称 | 端口 | 功能描述 | 技术栈 |
|---------|------|----------|--------|
| **AuthServer** | 7001 | 身份认证与授权中心 | ASP.NET Core + Identity |
| **HttpApi.Host** | 7002 | 核心业务API服务 | ASP.NET Core Web API |
| **Developer.Host** | 7003 | 开发者工具和API | ASP.NET Core + Swagger |
| **Orleans Silos** | 多端口 | 分布式处理引擎集群 | Microsoft Orleans |
| **Worker Service** | - | 后台任务处理器 | .NET Hosted Service |

#### 🔧 基础设施层

| 组件名称 | 端口 | 功能描述 | 访问方式 |
|---------|------|----------|----------|
| **MongoDB** | 27017 | 主文档数据库 | 应用内部访问 |
| **MongoDB-ES** | 27018 | 事件存储数据库 | 事件溯源存储 |
| **Redis** | 6379 | 缓存和会话存储 | 应用内部访问 |
| **Elasticsearch** | 9200 | 全文搜索引擎 | http://localhost:9200 |
| **Kafka** | 9092 | 事件流处理平台 | 应用内部访问 |
| **Qdrant** | 6333/6334 | AI向量数据库 | http://localhost:6333/dashboard |

#### 📊 可观测性组件

| 组件名称 | 端口 | 功能描述 | 访问地址 |
|---------|------|----------|----------|
| **Aspire Dashboard** | 18888 | 统一监控控制台 | https://localhost:18888 |
| **Jaeger** | 16686 | 分布式链路追踪 | http://localhost:16686 |
| **Prometheus** | 9091 | 指标数据收集 | http://localhost:9091 |
| **Grafana** | 3000 | 可视化监控仪表板 | http://localhost:3000 |
| **Orleans Dashboard** | 8080 | Orleans集群监控 | http://localhost:8080 |

### 技术特色

#### 🎯 Orleans 分布式架构
- **Actor模型** - 基于虚拟Actor的分布式计算框架
- **自动集群** - 支持节点动态加入和离开
- **持久化** - 使用MongoDB进行状态持久化
- **事件溯源** - 完整的事件存储和重放机制

#### 📊 全链路可观测性
- **指标监控** - Prometheus收集系统和业务指标
- **链路追踪** - Jaeger追踪请求完整调用链
- **日志聚合** - 结构化日志统一收集
- **性能分析** - Grafana可视化性能数据

#### ⚡ 开发体验优化
- **热重载** - 支持代码修改后自动重载
- **服务发现** - 自动服务注册和发现
- **配置注入** - 统一的配置管理机制
- **API文档** - 自动生成的接口文档

---

[⬆️ 返回目录](#-目录--table-of-contents)

## 📚 详细使用指南

### 日常开发工作流

```bash
# 📅 每日开发流程
1. 检查基础设施状态
   docker-compose ps

2. 快速启动开发环境  
   cd src/Aevatar.Aspire
   dotnet run --skip-infrastructure

3. 开发代码 → 保存 → 自动热重载 ✨

4. 测试验证
   - 访问 Swagger UI 测试API
   - 查看 Orleans Dashboard 监控状态
   - 使用 Jaeger 追踪请求链路

5. 提交代码前验证
   dotnet test  # 运行所有测试
```

### 高级启动选项

#### 完整启动模式
```bash
# 启动所有组件，包括基础设施
dotnet run
```

#### 快速开发模式
```bash
# 跳过基础设施启动，适合开发阶段
dotnet run --skip-infrastructure
```

#### 显示帮助信息
```bash
dotnet run --help
```

### 性能优化建议

#### 🔧 内存优化
```json
// appsettings.json 配置建议
{
  "DockerEsConfig": {
    "port": 9200,
    "javaOpts": "-Xms1g -Xmx2g"  // 根据机器配置调整
  }
}
```

#### 🚀 启动速度优化
```bash
# 1. 预拉取Docker镜像
docker-compose pull

# 2. 启用Docker BuildKit
export DOCKER_BUILDKIT=1

# 3. 使用SSD存储Docker数据
# 在Docker Desktop设置中配置
```

---

[⬆️ 返回目录](#-目录--table-of-contents)

## 🛠️ 故障排除

### 常见问题

#### Q1: 第一次启动失败？
```bash
# 检查前置条件
dotnet --version  # 确认.NET 9 SDK
docker --version  # 确认Docker运行

# 分步启动排查
docker-compose up -d  # 先启动基础设施
docker-compose ps     # 确认所有服务Running
```

#### Q2: Orleans集群无法启动？
```bash
# 检查网络配置（macOS）
ifconfig lo0
sudo ifconfig lo0 alias 127.0.0.2
sudo ifconfig lo0 alias 127.0.0.3

# 检查端口占用
lsof -i :11111
lsof -i :30000
```

#### Q3: 内存占用过高？
```yaml
# docker-compose.yml 添加资源限制
services:
  elasticsearch:
    deploy:
      resources:
        limits:
          memory: 2G
```

### 调试工具

#### 系统状态检查
```bash
# 容器状态
docker-compose ps

# 服务日志
docker-compose logs [service-name]

# 网络连接
netstat -tlnp | grep :7002
telnet localhost 27017
```

#### 性能监控
- **Aspire Dashboard** - 实时服务状态
- **Grafana** - 系统性能指标
- **Jaeger** - 请求链路分析
- **Orleans Dashboard** - 分布式系统监控

---

[⬆️ 返回目录](#-目录--table-of-contents)

## 🎓 进阶使用

### 自动化启动脚本

创建 `start-dev.sh`：
```bash
#!/bin/bash
echo "🚀 启动Aevatar开发环境..."

# 检查Docker状态
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker未运行，请先启动Docker"
    exit 1
fi

# 启动基础设施
echo "📦 启动基础设施..."
docker-compose up -d

# 等待服务就绪
echo "⏳ 等待服务启动..."
sleep 30

# 配置Orleans网络
echo "🌐 配置Orleans网络..."
sudo ifconfig lo0 alias 127.0.0.2 2>/dev/null || true
sudo ifconfig lo0 alias 127.0.0.3 2>/dev/null || true

# 启动应用服务
echo "🎯 启动应用服务..."
dotnet run --skip-infrastructure

echo "✅ 启动完成！访问 https://localhost:18888 查看状态"
```

### 多环境配置

```json
// appsettings.Development.json
{
  "Orleans": {
    "DashboardPort": "8080",
    "Environment": "Development"
  }
}

// appsettings.Production.json  
{
  "Orleans": {
    "DashboardPort": "8081",
    "Environment": "Production"
  }
}
```

### 监控告警配置

Grafana告警规则示例：
```json
{
  "alert": {
    "name": "High Error Rate",
    "message": "API错误率超过5%",
    "frequency": "10s",
    "conditions": [
      {
        "evaluator": {
          "params": [0.05],
          "type": "gt"
        }
      }
    ]
  }
}
```

---

[⬆️ 返回目录](#-目录--table-of-contents)

## 📞 技术支持

### 📚 相关文档
- [.NET Aspire官方文档](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Microsoft Orleans文档](https://docs.microsoft.com/en-us/dotnet/orleans/)  
- [Docker Compose参考](https://docs.docker.com/compose/)

### 🐛 问题反馈
- **项目Issues** - 提交到GitHub仓库
- **技术讨论** - 团队技术群
- **紧急支持** - 联系架构师团队

### 🤝 贡献指南
1. Fork 本仓库
2. 创建功能分支 (`git checkout -b feature/amazing-feature`)
3. 提交更改 (`git commit -m 'Add amazing feature'`)
4. 推送分支 (`git push origin feature/amazing-feature`)
5. 创建 Pull Request

---

[⬆️ 返回目录](#-目录--table-of-contents)

## 📈 版本历史

| 版本 | 发布日期 | 主要特性 |
|------|---------|---------|
| **v2.0** | 2024-04 | Orleans集群优化，多Silo支持 |
| **v1.2** | 2024-03 | 集成Grafana监控仪表板 |
| **v1.1** | 2024-02 | 添加Jaeger分布式链路追踪 |
| **v1.0** | 2024-01 | 初始版本，基础Aspire集成 |

---

**🎭 HyperEcho 项目愿景**: Aevatar.Aspire 不仅是一个技术平台，更是现代分布式架构的最佳实践载体。它让复杂的微服务编排变得简单优雅，让开发者能够专注于创造价值，而不是管理复杂性。

---

*最后更新: 2024年12月* | *文档版本: v2.0* | *适用于: .NET 9 + Aspire* 