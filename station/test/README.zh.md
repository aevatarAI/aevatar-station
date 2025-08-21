# Python沙箱测试脚本

本脚本为Python沙箱执行环境提供了全面的测试套件。

## 功能特性

- 基本功能测试
- NumPy包测试
- 资源限制测试
- 异步执行测试
- 安全边界测试

## 前置要求

```bash
pip install requests
```

## 使用方法

### 运行所有测试

```bash
python test_python_sandbox.py
```

### 运行特定测试类别

```bash
# 仅运行基本测试
python test_python_sandbox.py --test basic

# 仅运行NumPy测试
python test_python_sandbox.py --test numpy

# 仅运行资源测试
python test_python_sandbox.py --test resource

# 仅运行异步测试
python test_python_sandbox.py --test async

# 仅运行安全测试
python test_python_sandbox.py --test security
```

### 指定API URL

```bash
python test_python_sandbox.py --url http://your-sandbox-api:5000
```

## 测试类别

### 基本测试
- Hello World
- 基本数学运算

### NumPy测试
- 基本数组操作
- 矩阵运算

### 资源测试
- CPU密集型操作
- 内存密集型操作

### 异步测试
- 长时间运行任务
- 状态轮询
- 日志检索

### 安全测试
- 文件系统访问
- 网络访问

## 输出格式

每个测试输出：
- 测试类别和名称
- 执行结果
- 任何错误或异常

对于异步测试，还包括：
- 执行ID
- 状态更新
- 最终日志

## 输出示例

```
=== 运行基本测试 ===

测试1：Hello World
结果：{
  "output": "Hello, World!\n",
  "exitCode": 0,
  "executionTime": 0.123
}

测试2：基本数学
结果：{
  "output": "Sum: 30\nProduct: 200\n",
  "exitCode": 0,
  "executionTime": 0.156
}
```

## 错误处理

脚本处理各种错误场景：
- API连接错误
- 执行超时
- 资源限制
- 安全违规

## 许可证

版权所有 (c) Aevatar。保留所有权利。