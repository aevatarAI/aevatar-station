# MCP Gateway Integration Documentation

## English Documentation

### Overview

Welcome to the Aevatar MCP Gateway Integration documentation. This comprehensive guide covers the integration of Microsoft MCP Gateway with the Aevatar multi-agent system, providing scalable, session-aware routing and enterprise-ready management capabilities for Model Context Protocol (MCP) servers.

### What's New

The MCP Gateway integration transforms Aevatar's MCP connectivity from direct connections to a centralized gateway architecture, offering:

- **Scalable MCP Server Management**: Dynamic scaling in Kubernetes environments
- **Session-Aware Routing**: Consistent routing with session affinity
- **Enterprise-Ready Controls**: Comprehensive API management and monitoring
- **Seamless Migration**: Backward compatibility with existing configurations

### Documentation Structure

#### 📋 **[Architecture Overview](./architecture-overview.md)**
- System architecture and component relationships
- Connection flow and routing strategies
- State management and event sourcing
- Integration patterns and benefits

#### 🔌 **[API Reference](./api-reference.md)**
- Complete REST API documentation
- Request/response examples
- Error handling and status codes
- Authentication and authorization

#### ⚙️ **[Configuration Guide](./configuration-guide.md)**
- Configuration options and parameters
- Environment-specific setups
- Validation rules and best practices
- Example configurations

#### 👨‍💻 **[Developer Guide](./developer-guide.md)**
- Getting started with development
- Creating custom MCP agents
- Testing strategies and examples
- Advanced usage patterns

#### 🚀 **[Deployment Guide](./deployment-guide.md)**
- Local development deployment
- Production deployment to Azure
- Kubernetes configuration
- Monitoring and operations

#### 🔐 **[Permissions Guide](./permissions-guide.md)**
- Permission system overview
- Role-based access control
- Custom permission configuration
- Security best practices

#### 🔧 **[Troubleshooting Guide](./troubleshooting-guide.md)**
- Common issues and solutions
- Debugging tools and techniques
- Performance optimization
- Health monitoring

### Quick Navigation

| Topic | English | 中文 |
|-------|---------|------|
| Getting Started | [Developer Guide](./developer-guide.md#getting-started) | [开发者指南](./developer-guide.md#快速开始) |
| API Endpoints | [API Reference](./api-reference.md#api-endpoints) | [API参考](./api-reference.md#api端点) |
| Configuration | [Configuration Guide](./configuration-guide.md#configuration-options) | [配置指南](./configuration-guide.md#配置选项) |
| Deployment | [Deployment Guide](./deployment-guide.md#local-development-deployment) | [部署指南](./deployment-guide.md#本地开发部署) |
| Permissions | [Permissions Guide](./permissions-guide.md#permission-hierarchy) | [权限指南](./permissions-guide.md#权限层次结构) |
| Troubleshooting | [Troubleshooting Guide](./troubleshooting-guide.md#common-issues-and-solutions) | [故障排除指南](./troubleshooting-guide.md#常见问题和解决方案) |

### Key Features

#### **Gateway-First Architecture**
- Intelligent connection routing with fallback mechanisms
- Automatic session management and persistence
- Enterprise-grade security and access control

#### **Comprehensive API Management**
- RESTful APIs for complete adapter lifecycle management
- Real-time monitoring and health checks
- Detailed logging and metrics collection

#### **Developer-Friendly Integration**
- Seamless integration with existing Aevatar agents
- Backward compatibility with direct connections
- Extensive testing and validation tools

### Support and Community

- **GitHub Issues**: [Report bugs and feature requests](https://github.com/aevatarAI/aevatar-station/issues)
- **Documentation**: This comprehensive guide and API reference
- **Examples**: Sample configurations and implementation patterns

---

## 中文文档

### 概述

欢迎使用Aevatar MCP Gateway集成文档。本综合指南涵盖了Microsoft MCP Gateway与Aevatar多智能体系统的集成，为模型上下文协议(MCP)服务器提供可扩展的、会话感知的路由和企业级管理功能。

### 新特性

MCP Gateway集成将Aevatar的MCP连接从直连转换为集中式网关架构，提供：

- **可扩展的MCP服务器管理**: 在Kubernetes环境中的动态扩展
- **会话感知路由**: 具有会话亲和性的一致路由
- **企业级控制**: 全面的API管理和监控
- **无缝迁移**: 与现有配置的向后兼容性

### 文档结构

#### 📋 **[架构概览](./architecture-overview.md)**
- 系统架构和组件关系
- 连接流程和路由策略
- 状态管理和事件溯源
- 集成模式和优势

#### 🔌 **[API参考](./api-reference.md)**
- 完整的REST API文档
- 请求/响应示例
- 错误处理和状态码
- 身份验证和授权

#### ⚙️ **[配置指南](./configuration-guide.md)**
- 配置选项和参数
- 特定环境设置
- 验证规则和最佳实践
- 示例配置

#### 👨‍💻 **[开发者指南](./developer-guide.md)**
- 开发入门
- 创建自定义MCP智能体
- 测试策略和示例
- 高级使用模式

#### 🚀 **[部署指南](./deployment-guide.md)**
- 本地开发部署
- 生产环境Azure部署
- Kubernetes配置
- 监控和运维

#### 🔐 **[权限指南](./permissions-guide.md)**
- 权限系统概述
- 基于角色的访问控制
- 自定义权限配置
- 安全最佳实践

#### 🔧 **[故障排除指南](./troubleshooting-guide.md)**
- 常见问题和解决方案
- 调试工具和技术
- 性能优化
- 健康监控

### 核心特性

#### **网关优先架构**
- 具有回退机制的智能连接路由
- 自动会话管理和持久化
- 企业级安全和访问控制

#### **全面的API管理**
- 完整适配器生命周期管理的RESTful API
- 实时监控和健康检查
- 详细的日志记录和指标收集

#### **开发者友好的集成**
- 与现有Aevatar智能体的无缝集成
- 与直连的向后兼容性
- 广泛的测试和验证工具

### 支持和社区

- **GitHub Issues**: [报告错误和功能请求](https://github.com/aevatarAI/aevatar-station/issues)
- **文档**: 本综合指南和API参考
- **示例**: 示例配置和实现模式
