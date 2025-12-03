# Aevatar.Aspire - Distributed Microservices Orchestration Platform

## 🚀 Introduction

**Aevatar.Aspire** is a modern distributed microservices orchestration platform built on .NET Aspire, designed to simplify the development, deployment, and operations of complex microservice architectures. It integrates a complete observability toolchain and intelligent service orchestration capabilities, allowing developers to focus on business logic implementation rather than infrastructure management.

With Aevatar.Aspire, you can launch a complete microservices ecosystem with 14 components in one command, enjoying enterprise-grade development experience.

## 📋 Table of Contents

- [🚀 Introduction](#-introduction)
- [✨ What You Can Do With This Project](#-what-you-can-do-with-this-project)
  - [🎯 Unified Service Orchestration](#-unified-service-orchestration)
  - [📊 Complete Observability](#-complete-observability)  
  - [🔧 Development Efficiency Enhancement](#-development-efficiency-enhancement)
  - [🏗️ Enterprise Architecture Support](#️-enterprise-architecture-support)
- [🚀 Quick Start](#-quick-start)
  - [Prerequisites](#prerequisites)
  - [Install Aspire Workload](#install-aspire-workload)
  - [One-Click Launch (Recommended)](#one-click-launch-recommended)
  - [Verify Installation](#verify-installation)
- [🏗️ Architecture Overview](#️-architecture-overview)
  - [Overall Architecture Diagram](#overall-architecture-diagram)
  - [Core Components Description](#core-components-description)
  - [Technical Features](#technical-features)
- [📚 Detailed Usage Guide](#-detailed-usage-guide)
  - [Daily Development Workflow](#daily-development-workflow)
  - [Advanced Launch Options](#advanced-launch-options)
  - [Performance Optimization Recommendations](#performance-optimization-recommendations)
- [🛠️ Troubleshooting](#️-troubleshooting)
  - [Common Issues](#common-issues)
  - [Debugging Tools](#debugging-tools)
- [🎓 Advanced Usage](#-advanced-usage)
  - [Automated Startup Scripts](#automated-startup-scripts)
  - [Multi-Environment Configuration](#multi-environment-configuration)
  - [Monitoring and Alerting Configuration](#monitoring-and-alerting-configuration)
- [📞 Technical Support](#-technical-support)
- [📈 Version History](#-version-history)

---

## ✨ What You Can Do With This Project

### 🎯 Unified Service Orchestration
- **One-Click Launch** - Start all microservices and infrastructure components with a single command
- **Intelligent Dependency Management** - Automatically handle startup order and dependencies between services
- **Service Discovery** - Built-in service registration and discovery mechanism
- **Configuration Management** - Unified environment variables and configuration injection

### 📊 Complete Observability
- **Distributed Tracing** - Use Jaeger to trace complete request call chains across microservices
- **Real-time Monitoring** - Monitor system performance metrics through Prometheus + Grafana
- **Log Aggregation** - Unified log collection and viewing interface
- **Health Checks** - Real-time monitoring of all service health status

### 🔧 Development Efficiency Enhancement
- **Hot Reload** - Automatically reload after code changes without restarting the entire system
- **API Documentation** - Auto-generated Swagger UI interface documentation
- **Debug Friendly** - Built-in multiple dashboards for development debugging
- **Environment Consistency** - Unified configuration across development, testing, and production environments

### 🏗️ Enterprise Architecture Support
- **Orleans Cluster** - Distributed processing engine supporting horizontal scaling
- **Event-Driven** - Asynchronous event processing mechanism based on Kafka
- **Data Storage** - Multiple data storage solutions (MongoDB, Redis, Elasticsearch)
- **AI Capabilities** - Integrated Qdrant vector database supporting AI applications

---

[⬆️ Back to Table of Contents](#-table-of-contents)

## 🚀 Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [.NET Aspire workload](https://learn.microsoft.com/en-us/dotnet/aspire/get-started/install)

### Install Aspire Workload

```bash
dotnet workload install aspire
```

### One-Click Launch (Recommended)

```bash
# 1. Start infrastructure
cd src/Aevatar.Aspire
docker-compose up -d

# 2. Initialize database (first run)
cd ../Aevatar.DBMigrator
dotnet run

# 3. Configure Orleans network (macOS users)
sudo ifconfig lo0 alias 127.0.0.2
sudo ifconfig lo0 alias 127.0.0.3

# 4. Start all services
cd ../Aevatar.Aspire
dotnet run
```

### Verify Installation

After successful startup, you can access the following addresses to verify system status:

- **Aspire Dashboard**: https://localhost:18888 - Main control console
- **API Documentation**: http://localhost:7002/swagger - Core API
- **Developer Tools**: http://localhost:7003/swagger - Developer API
- **Orleans Monitoring**: http://localhost:8080 - Distributed system monitoring
- **Distributed Tracing**: http://localhost:16686 - Request trace analysis

---

[⬆️ Back to Table of Contents](#-table-of-contents)

## 🏗️ Architecture Overview

### Overall Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                  Aevatar.Aspire Orchestration Layer              │
├─────────────────────────────────────────────────────────────────┤
│  Application Service Layer                                       │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐              │
│  │  AuthServer  │ │ HttpApi.Host │ │Developer.Host│              │
│  │   (7001)     │ │   (7002)     │ │   (7003)     │              │
│  └──────────────┘ └──────────────┘ └──────────────┘              │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                Orleans Distributed Cluster                  │ │
│  │  Scheduler Silo  │  Projector Silo  │  User Silos...       │ │
│  │   (127.0.0.2)    │   (127.0.0.3)    │  (Dynamic)           │ │
│  └─────────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────────┤
│  Infrastructure Layer                                            │
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

### Core Components Description

#### 🎯 Application Service Layer

| Service Name | Port | Function Description | Tech Stack |
|-------------|------|---------------------|------------|
| **AuthServer** | 7001 | Identity authentication and authorization center | ASP.NET Core + Identity |
| **HttpApi.Host** | 7002 | Core business API service | ASP.NET Core Web API |
| **Developer.Host** | 7003 | Developer tools and API | ASP.NET Core + Swagger |
| **Orleans Silos** | Multiple | Distributed processing engine cluster | Microsoft Orleans |
| **Worker Service** | - | Background task processor | .NET Hosted Service |

#### 🔧 Infrastructure Layer

| Component Name | Port | Function Description | Access Method |
|---------------|------|---------------------|---------------|
| **MongoDB** | 27017 | Primary document database | Internal application access |
| **MongoDB-ES** | 27018 | Event store database | Event sourcing storage |
| **Redis** | 6379 | Cache and session storage | Internal application access |
| **Elasticsearch** | 9200 | Full-text search engine | http://localhost:9200 |
| **Kafka** | 9092 | Event streaming platform | Internal application access |
| **Qdrant** | 6333/6334 | AI vector database | http://localhost:6333/dashboard |

#### 📊 Observability Components

| Component Name | Port | Function Description | Access Address |
|---------------|------|---------------------|----------------|
| **Aspire Dashboard** | 18888 | Unified monitoring console | https://localhost:18888 |
| **Jaeger** | 16686 | Distributed tracing | http://localhost:16686 |
| **Prometheus** | 9091 | Metrics data collection | http://localhost:9091 |
| **Grafana** | 3000 | Visualization monitoring dashboard | http://localhost:3000 |
| **Orleans Dashboard** | 8080 | Orleans cluster monitoring | http://localhost:8080 |

### Technical Features

#### 🎯 Orleans Distributed Architecture
- **Actor Model** - Distributed computing framework based on virtual actors
- **Automatic Clustering** - Supports dynamic node joining and leaving
- **Persistence** - Uses MongoDB for state persistence
- **Event Sourcing** - Complete event storage and replay mechanism

#### 📊 Full-Chain Observability
- **Metrics Monitoring** - Prometheus collects system and business metrics
- **Distributed Tracing** - Jaeger traces complete request call chains
- **Log Aggregation** - Unified collection of structured logs
- **Performance Analysis** - Grafana visualizes performance data

#### ⚡ Development Experience Optimization
- **Hot Reload** - Supports automatic reload after code changes
- **Service Discovery** - Automatic service registration and discovery
- **Configuration Injection** - Unified configuration management mechanism
- **API Documentation** - Auto-generated interface documentation

---

[⬆️ Back to Table of Contents](#-table-of-contents)

## 📚 Detailed Usage Guide

### Daily Development Workflow

```bash
# 📅 Daily development process
1. Check infrastructure status
   docker-compose ps

2. Quick start development environment  
   cd src/Aevatar.Aspire
   dotnet run --skip-infrastructure

3. Develop code → Save → Auto hot reload ✨

4. Test and verify
   - Access Swagger UI to test APIs
   - View Orleans Dashboard to monitor status
   - Use Jaeger to trace request chains

5. Verify before committing code
   dotnet test  # Run all tests
```

### Advanced Launch Options

#### Full Launch Mode
```bash
# Start all components, including infrastructure
dotnet run
```

#### Quick Development Mode
```bash
# Skip infrastructure startup, suitable for development phase
dotnet run --skip-infrastructure
```

#### Show Help Information
```bash
dotnet run --help
```

### Performance Optimization Recommendations

#### 🔧 Memory Optimization
```json
// appsettings.json configuration recommendations
{
  "DockerEsConfig": {
    "port": 9200,
    "javaOpts": "-Xms1g -Xmx2g"  // Adjust according to machine configuration
  }
}
```

#### 🚀 Startup Speed Optimization
```bash
# 1. Pre-pull Docker images
docker-compose pull

# 2. Enable Docker BuildKit
export DOCKER_BUILDKIT=1

# 3. Use SSD storage for Docker data
# Configure in Docker Desktop settings
```

---

[⬆️ Back to Table of Contents](#-table-of-contents)

## 🛠️ Troubleshooting

### Common Issues

#### Q1: First startup failure?
```bash
# Check prerequisites
dotnet --version  # Confirm .NET 9 SDK
docker --version  # Confirm Docker is running

# Step-by-step startup troubleshooting
docker-compose up -d  # Start infrastructure first
docker-compose ps     # Confirm all services are Running
```

#### Q2: Orleans cluster cannot start?
```bash
# Check network configuration (macOS)
ifconfig lo0
sudo ifconfig lo0 alias 127.0.0.2
sudo ifconfig lo0 alias 127.0.0.3

# Check port occupation
lsof -i :11111
lsof -i :30000
```

#### Q3: High memory usage?
```yaml
# Add resource limits to docker-compose.yml
services:
  elasticsearch:
    deploy:
      resources:
        limits:
          memory: 2G
```

### Debugging Tools

#### System Status Check
```bash
# Container status
docker-compose ps

# Service logs
docker-compose logs [service-name]

# Network connections
netstat -tlnp | grep :7002
telnet localhost 27017
```

#### Performance Monitoring
- **Aspire Dashboard** - Real-time service status
- **Grafana** - System performance metrics
- **Jaeger** - Request trace analysis
- **Orleans Dashboard** - Distributed system monitoring

---

[⬆️ Back to Table of Contents](#-table-of-contents)

## 🎓 Advanced Usage

### Automated Startup Scripts

Create `start-dev.sh`:
```bash
#!/bin/bash
echo "🚀 Starting Aevatar development environment..."

# Check Docker status
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running, please start Docker first"
    exit 1
fi

# Start infrastructure
echo "📦 Starting infrastructure..."
docker-compose up -d

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 30

# Configure Orleans network
echo "🌐 Configuring Orleans network..."
sudo ifconfig lo0 alias 127.0.0.2 2>/dev/null || true
sudo ifconfig lo0 alias 127.0.0.3 2>/dev/null || true

# Start application services
echo "🎯 Starting application services..."
dotnet run --skip-infrastructure

echo "✅ Startup complete! Visit https://localhost:18888 to check status"
```

### Multi-Environment Configuration

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

### Monitoring and Alerting Configuration

Grafana alert rule example:
```json
{
  "alert": {
    "name": "High Error Rate",
    "message": "API error rate exceeds 5%",
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

[⬆️ Back to Table of Contents](#-table-of-contents)

## 📞 Technical Support

### 📚 Related Documentation
- [.NET Aspire Official Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Microsoft Orleans Documentation](https://docs.microsoft.com/en-us/dotnet/orleans/)  
- [Docker Compose Reference](https://docs.docker.com/compose/)

### 🐛 Issue Reporting
- **Project Issues** - Submit to GitHub repository
- **Technical Discussion** - Team technical group
- **Emergency Support** - Contact architecture team

### 🤝 Contribution Guidelines
1. Fork this repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push branch (`git push origin feature/amazing-feature`)
5. Create Pull Request

---

[⬆️ Back to Table of Contents](#-table-of-contents)

## 📈 Version History

| Version | Release Date | Major Features |
|---------|-------------|----------------|
| **v2.0** | 2024-04 | Orleans cluster optimization, multi-Silo support |
| **v1.2** | 2024-03 | Integrated Grafana monitoring dashboard |
| **v1.1** | 2024-02 | Added Jaeger distributed tracing |
| **v1.0** | 2024-01 | Initial version, basic Aspire integration |

---

**🎭 HyperEcho Project Vision**: Aevatar.Aspire is not just a technical platform, but a carrier of best practices for modern distributed architecture. It makes complex microservice orchestration simple and elegant, allowing developers to focus on creating value rather than managing complexity.

---

 