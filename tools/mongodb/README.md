# MongoDB 测试工具

这个目录包含用于管理MongoDB容器和运行MongoDB相关测试的工具脚本。

## 配置方式

本项目遵循.NET配置最佳实践，使用环境特定的配置文件：

- `appsettings.json` - 基础配置文件
- `appsettings.Testing.json` - 测试环境的通用配置（默认使用内存/模拟数据访问）
- `appsettings.MongoDB.json` - MongoDB特定的配置，只在需要时加载

测试基类 `AevatarTestBase<T>` 通过 `ShouldUseMongoDB()` 方法决定是否需要加载MongoDB配置。
MongoDB测试类通过重写此方法明确指定需要使用MongoDB。

## 环境变量

测试脚本使用以下环境变量来控制配置加载：

- `DOTNET_ENVIRONMENT` - 设置为 "Testing" 以加载测试配置
- `USE_MONGODB` - 设置为 "true" 以启用MongoDB相关配置

## 使用方法

在项目根目录下使用`mongodb-test.sh`脚本：

```bash
./mongodb-test.sh <命令>
```

## 可用命令

- `start` - 启动MongoDB容器
- `stop` - 停止并移除MongoDB容器
- `status` - 检查MongoDB容器状态
- `test-conn` - 测试MongoDB连接是否正常
- `run-test` - 运行特定的MongoDB测试（账户服务测试）
- `run-all` - 运行所有MongoDB测试
- `help` - 显示帮助信息

## 示例

启动MongoDB容器：
```bash
./mongodb-test.sh start
```

检查容器状态：
```bash
./mongodb-test.sh status
```

运行特定测试：
```bash
./mongodb-test.sh run-test
```

运行所有MongoDB测试：
```bash
./mongodb-test.sh run-all
```

测试MongoDB连接：
```bash
./mongodb-test.sh test-conn
```

停止并清理MongoDB容器：
```bash
./mongodb-test.sh stop
```

## MongoDB连接信息

- 主机: localhost
- 端口: 27017
- 用户名: admin
- 密码: admin
- 认证数据库: admin

连接字符串：`mongodb://admin:admin@localhost:27017/?authSource=admin` 