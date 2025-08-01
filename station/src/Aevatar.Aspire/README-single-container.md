# Aevatar Aspire 单容器部署指南

这个Dockerfile创建了一个包含所有Aevatar.Aspire服务的单容器，替代了原来的docker-compose多容器架构。

## 🏗️ 架构概述

单容器包含以下服务：

### 数据存储服务
- **MongoDB** (主实例): 端口 27017
- **MongoDB ES** (事件存储): 端口 27018  
- **Redis**: 端口 6379
- **Elasticsearch**: 端口 9200

### 消息队列
- **Zookeeper**: 端口 2181
- **Kafka**: 端口 9092

### AI/向量数据库
- **Qdrant**: 端口 6333 (HTTP), 6334 (gRPC)

### 监控与追踪
- **Jaeger**: 端口 16686 (UI), 14250 (Collector), 4317/4318 (OTLP)
- **OpenTelemetry Collector**: 端口 4315 (gRPC), 9090 (Metrics)
- **Prometheus**: 端口 9091
- **Grafana**: 端口 3000
- **Blackbox Exporter**: 端口 9115

## 🚀 快速开始

### 1. 构建镜像

```bash
cd station/src/Aevatar.Aspire
docker build -t aevatar/aspire:all-in-one -f Dockerfile.all-in-one .
```

### 2. 运行容器（使用默认配置）

```bash
docker run -d \
    --name aevatar-all-in-one \
    --restart unless-stopped \
    -p 27017:27017 -p 27018:27018 -p 6379:6379 \
    -p 9092:9092 -p 2181:2181 -p 9200:9200 \
    -p 6333:6333 -p 6334:6334 -p 16686:16686 \
    -p 4317:4317 -p 4318:4318 -p 4315:4315 \
    -p 9090:9090 -p 9091:9091 -p 3000:3000 -p 9115:9115 \
    -v /path/to/data:/data \
    aevatar/aspire:all-in-one
```

### 3. 使用示例脚本

```bash
# 使用提供的示例脚本
./docker-run-example.sh
```

## ⚙️ 配置管理

### 挂载外部配置

容器支持通过挂载目录提供自定义配置：

```bash
docker run -d \
    --name aevatar-all-in-one \
    -v /path/to/data:/data \
    -v /path/to/config:/external-config:ro \
    aevatar/aspire:all-in-one
```

### 支持的配置文件

在 `/external-config` 目录中放置以下配置文件来覆盖默认配置：

| 配置文件 | 说明 | 默认路径 |
|---------|------|----------|
| `mongod.conf` | MongoDB主实例配置 | `/etc/mongod.conf` |
| `mongod-es.conf` | MongoDB ES实例配置 | `/etc/mongod-es.conf` |
| `redis.conf` | Redis配置 | `/etc/redis/redis.conf` |
| `elasticsearch.yml` | Elasticsearch配置 | `/etc/elasticsearch/elasticsearch.yml` |
| `server.properties` | Kafka配置 | `/opt/kafka/config/server.properties` |
| `zookeeper.properties` | Zookeeper配置 | `/opt/kafka/config/zookeeper.properties` |
| `qdrant.yaml` | Qdrant配置 | `/opt/qdrant/config/production.yaml` |
| `otel-collector-config.yaml` | OpenTelemetry配置 | `/etc/otel-collector-config.yaml` |
| `prometheus.yml` | Prometheus配置 | `/etc/prometheus/prometheus.yml` |
| `grafana.ini` | Grafana配置 | `/etc/grafana/grafana.ini` |
| `datasource.yaml` | Grafana数据源配置 | `/etc/grafana/provisioning/datasources/datasource.yaml` |
| `blackbox.yml` | Blackbox Exporter配置 | `/etc/blackbox/blackbox.yml` |

### 配置示例

**MongoDB配置示例** (`mongod.conf`):
```yaml
storage:
  dbPath: /data/mongodb
net:
  port: 27017
  bindIp: 0.0.0.0
systemLog:
  destination: file
  path: /var/log/mongodb.log
  logAppend: true
```

**Redis配置示例** (`redis.conf`):
```
bind 0.0.0.0
port 6379
dir /data/redis
maxmemory 1gb
maxmemory-policy allkeys-lru
```

## 🔍 监控与管理

### 访问服务UI

- **Grafana**: http://localhost:3000 (admin/admin)
- **Jaeger**: http://localhost:16686
- **Prometheus**: http://localhost:9091
- **Elasticsearch**: http://localhost:9200

### 容器管理命令

```bash
# 查看服务状态
docker exec aevatar-all-in-one supervisorctl status

# 重启特定服务
docker exec aevatar-all-in-one supervisorctl restart mongodb

# 查看日志
docker logs -f aevatar-all-in-one

# 查看特定服务日志
docker exec aevatar-all-in-one tail -f /var/log/mongodb.log
```

## 🧪 测试验证

### 自动化测试

```bash
# 运行完整测试套件
./build-and-test.sh
```

### 手动测试

```bash
# 测试MongoDB连接
docker exec aevatar-all-in-one mongosh --port 27017 --eval "db.runCommand('ismaster')"

# 测试Redis连接  
docker exec aevatar-all-in-one redis-cli ping

# 测试Elasticsearch
curl http://localhost:9200/_cluster/health

# 测试Qdrant
curl http://localhost:6333/health
```

## 📊 数据持久化

重要数据目录：
- `/data/mongodb` - MongoDB主数据库
- `/data/mongodb-es` - MongoDB事件存储
- `/data/redis` - Redis持久化数据
- `/data/elasticsearch` - Elasticsearch索引
- `/data/kafka` - Kafka日志
- `/data/zookeeper` - Zookeeper数据
- `/data/qdrant` - Qdrant向量数据
- `/data/prometheus` - Prometheus时序数据
- `/data/grafana` - Grafana配置和仪表板

## 🔧 故障排除

### 常见问题

1. **服务启动失败**
   ```bash
   # 检查具体服务状态
   docker exec aevatar-all-in-one supervisorctl status
   
   # 查看错误日志
   docker exec aevatar-all-in-one cat /var/log/supervisord.log
   ```

2. **端口冲突**
   - 确保主机上没有其他服务占用相同端口
   - 可以修改端口映射：`-p 27018:27017` (将主机27018映射到容器27017)

3. **内存不足**
   - Elasticsearch默认使用512MB堆内存
   - 可以通过环境变量调整：`-e ES_JAVA_OPTS="-Xms256m -Xmx256m"`

4. **权限问题**
   ```bash
   # 检查数据目录权限
   docker exec aevatar-all-in-one ls -la /data
   
   # 修复权限
   docker exec aevatar-all-in-one chown -R aevatar:aevatar /data
   ```

## 🎯 性能优化

### 资源限制

```bash
docker run -d \
    --name aevatar-all-in-one \
    --memory=4g \
    --cpus=2 \
    aevatar/aspire:all-in-one
```

### 生产环境建议

1. **分离数据卷**：为不同服务使用独立的数据卷
2. **网络配置**：使用自定义网络而不是默认bridge
3. **日志管理**：配置日志轮转和外部日志收集
4. **监控告警**：设置Prometheus告警规则
5. **备份策略**：定期备份关键数据目录

## 📝 更新日志

- **v1.0**: 初始版本，支持12个服务的单容器部署
- **v1.1**: 支持外部配置文件挂载，提高配置灵活性 