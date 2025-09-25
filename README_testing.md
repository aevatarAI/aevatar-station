# MCP Services Testing Scripts

这个目录包含了用于测试Aevatar MCP服务基础设施的完整测试脚本集合。

## 🌌 测试环境信息

- **Auth Server**: http://env-a30ba821.auth-station-testing.aevatar.ai
- **API Server**: http://env-a30ba821.station-testing.aevatar.ai  
- **MCP Gateway**: http://env-a30ba821.mcp-testing.aevatar.ai

### 已部署的MCP服务器

| 服务器名称 | URL | 功能描述 |
|-----------|-----|---------|
| Time Server | http://env-a30ba821.mcp-testing.aevatar.ai/adapters/time/mcp | 时间相关操作 |
| Fetch Server | http://env-a30ba821.mcp-testing.aevatar.ai/adapters/fetch/mcp | HTTP请求和数据获取 |
| Filesystem Server | http://env-a30ba821.mcp-testing.aevatar.ai/adapters/filesystem/mcp | 文件系统操作 |
| Git Server | http://env-a30ba821.mcp-testing.aevatar.ai/adapters/git/mcp | Git仓库操作 |
| Memory Server | http://env-a30ba821.mcp-testing.aevatar.ai/adapters/memory/mcp | 内存存储和检索 |
| Sequential Thinking | http://env-a30ba821.mcp-testing.aevatar.ai/adapters/sequentialthinking/mcp | 顺序思维链处理 |

## 📋 测试脚本说明

### 1. `quick_test.sh` - 快速健康检查

**用途**: 快速验证所有服务的基本可用性

**运行方式**:
```bash
./quick_test.sh
```

**测试内容**:
- ✅ 所有服务器的健康状态检查
- ✅ MCP服务器的基本工具列表获取
- ✅ API端点的快速响应测试

**预期输出**:
```
=== Quick MCP Services Health Check ===

Testing Auth Server... ✓ OK (HTTP 200)
Testing API Server... ✓ OK (HTTP 200)
Testing MCP Gateway... ✓ OK (HTTP 200)
Testing Time Server... ✓ OK (HTTP 200)
...

=== Testing MCP Tools List ===

Getting tools from time... ✓ 3 tools
Getting tools from fetch... ✓ 5 tools
...
```

### 2. `test_mcp_services.sh` - 完整服务测试

**用途**: 全面测试所有MCP服务的完整功能

**运行方式**:
```bash
./test_mcp_services.sh
```

**测试内容**:
- 🔍 健康状态检查
- 🤝 MCP协议初始化测试
- 🔧 工具列表获取和验证
- 📁 资源列表检查
- 📊 详细的测试报告生成

**特性**:
- 彩色输出，易于阅读
- 详细的错误报告
- 成功率统计
- JSON响应验证

### 3. `test_mcp_tools.sh` - 工具功能详细测试

**用途**: 深入测试每个MCP服务器的具体工具功能

**运行方式**:
```bash
./test_mcp_tools.sh
```

**测试内容**:
- 🔧 每个服务器的工具详细列表
- ⚡ 实际工具调用测试
- 📝 工具响应内容验证
- 🎯 特定服务器的专项测试

**专项测试包括**:
- **Time Server**: 当前时间获取、时区转换
- **Fetch Server**: HTTP GET请求测试
- **Filesystem Server**: 目录列表操作
- **Git Server**: Git状态检查
- **Memory Server**: 记忆存储和检索
- **Sequential Thinking**: 思维链处理

## 🛠️ 依赖要求

所有脚本都需要以下工具：

```bash
# macOS
brew install curl jq

# Ubuntu/Debian
sudo apt-get install curl jq

# CentOS/RHEL
sudo yum install curl jq
```

## 🚀 快速开始

1. **快速验证所有服务**:
   ```bash
   ./quick_test.sh
   ```

2. **完整服务测试**:
   ```bash
   ./test_mcp_services.sh
   ```

3. **深入工具测试**:
   ```bash
   ./test_mcp_tools.sh
   ```

## 📊 测试报告示例

### 成功输出示例
```
=== Test Report Summary ===
Total Tests: 24
Passed: 22
Failed: 2
Success Rate: 91%

Successful Tests:
  ✓ Auth Server Health: HTTP 200, 0.234s
  ✓ Time Server Tools List: 3 tools available
  ✓ Memory Server MCP Initialize: Valid MCP initialization response
  ...
```

### 失败输出示例
```
Failed Tests:
  ✗ Git Server Health: HTTP 503
  ✗ Fetch Server Tools List: HTTP 404
```

## 🔧 故障排除

### 常见问题

1. **连接超时**:
   - 检查网络连接
   - 验证服务器URL是否正确
   - 确认服务器是否正在运行

2. **JSON解析错误**:
   - 服务器可能返回非标准响应
   - 检查服务器日志获取详细错误信息

3. **工具调用失败**:
   - 某些工具可能需要特定参数
   - 检查MCP服务器的具体实现

### 调试模式

在任何脚本前添加 `bash -x` 可以看到详细的执行过程：

```bash
bash -x ./test_mcp_services.sh
```

## 🎯 测试策略建议

1. **开发阶段**: 使用 `quick_test.sh` 进行快速验证
2. **集成测试**: 使用 `test_mcp_services.sh` 进行全面测试
3. **功能验证**: 使用 `test_mcp_tools.sh` 验证具体工具功能
4. **CI/CD集成**: 将这些脚本集成到自动化测试流程中

## 📈 扩展测试

如需添加新的测试用例：

1. 在相应脚本中添加新的测试函数
2. 更新 `MCP_SERVERS` 数组（如果有新服务器）
3. 添加特定的工具测试逻辑
4. 更新此文档

## 🔗 相关链接

- [MCP Protocol Specification](https://spec.modelcontextprotocol.io/)
- [Aevatar Documentation](https://github.com/aevatarAI/aevatar-station)
- [MCP Gateway Implementation](https://github.com/aevatarAI/mcp-gateway)

---

**注意**: 这些测试脚本设计为非侵入性的，只进行读取操作和安全的测试调用。在生产环境中使用时请谨慎。


