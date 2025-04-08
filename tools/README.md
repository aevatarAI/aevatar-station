# Aevatar Station 压力测试工具

此工具用于对Aevatar Station进行性能和压力测试，模拟多用户并发访问场景。

## 功能特点

- 支持多用户并发连接测试
- 批量创建会话和发送消息
- 详细的性能指标统计（响应时间、成功率等）
- 可配置的测试参数
- 支持分批执行以避免瞬时高负载
- 资源管理和连接池控制
- 完善的错误处理和日志记录

## 安装

```bash
# 安装依赖
npm install
```

## 使用方法

### 基本用法

```bash
# 使用默认配置运行测试
npm run benchmark
```

### 带参数运行

```bash
# 自定义用户数量和批次大小
node benchmark.js --userCount=10 --batchSize=5

# 高并发压力测试
npm run benchmark:stress
```

### 可用命令行参数

| 参数名 | 说明 | 默认值 |
|--------|------|--------|
| `--baseUrl` | 服务器基础URL | `https://station-developer-staging.aevatar.ai` |
| `--userCount` | 模拟用户数量 | 1 |
| `--batchSize` | 批处理大小 | 1 |
| `--messageCount` | 每个会话发送的消息数 | 1 |
| `--maxConcurrentConnections` | 最大并发连接数 | 200 |
| `--systemLLM` | 使用的LLM模型 | "OpenAI" |
| `--getSessionIdTimeout` | 获取会话ID超时(毫秒) | 60000 |
| `--connectionTimeout` | 连接超时(毫秒) | 60000 |
| `--messageResponseTimeout` | 消息响应超时(毫秒) | 10000 |

## 测试结果

测试结果将同时输出到控制台和保存在`benchmark-results`目录中。结果包含以下信息：

- 创建会话的成功率和耗时
- 消息处理的成功率和响应时间
- 响应时间的统计分析(最大/最小/平均/中位数/百分位)
- 系统吞吐量指标(每秒处理消息数)

## 开发

### 运行单元测试

```bash
# 运行所有测试
npm test

# 监视模式
npm run test:watch
```

## 测试阶段说明

测试分为三个阶段：

1. **Phase 1**：创建会话
   - 按批次创建指定数量的用户会话
   - 统计会话创建成功率和耗时

2. **Phase 2**：消息处理
   - 向已创建的会话发送测试消息
   - 统计消息处理成功率和响应时间

3. **Phase 3**：清理会话
   - 清理所有创建的会话资源
   - 释放服务器资源

## 故障排除

如果测试过程中遇到问题：

1. 检查网络连接和服务器状态
2. 确认配置参数正确，特别是baseUrl
3. 如需更详细的日志，可以修改代码中的日志级别
4. 查看benchmark-results目录中的测试结果和错误信息 