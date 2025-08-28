# Python Sandbox Test Script

This script provides a comprehensive test suite for the Python sandbox execution environment.

## Features

- Basic functionality tests
- NumPy package tests
- Resource limit tests
- Asynchronous execution tests
- Security boundary tests

## Prerequisites

```bash
pip install requests
```

## Usage

### Run All Tests

```bash
python test_python_sandbox.py
```

### Run Specific Test Category

```bash
# Run basic tests only
python test_python_sandbox.py --test basic

# Run NumPy tests only
python test_python_sandbox.py --test numpy

# Run resource tests only
python test_python_sandbox.py --test resource

# Run async tests only
python test_python_sandbox.py --test async

# Run security tests only
python test_python_sandbox.py --test security
```

### Specify API URL

```bash
python test_python_sandbox.py --url http://your-sandbox-api:5000
```

## Test Categories

### Basic Tests
- Hello World
- Basic Math Operations

### NumPy Tests
- Basic Array Operations
- Matrix Operations

### Resource Tests
- CPU Intensive Operations
- Memory Intensive Operations

### Async Tests
- Long Running Tasks
- Status Polling
- Log Retrieval

### Security Tests
- File System Access
- Network Access

## Output Format

Each test outputs:
- Test category and name
- Execution result
- Any errors or exceptions

For async tests, also includes:
- Execution ID
- Status updates
- Final logs

## Example Output

```
=== Running Basic Tests ===

Test 1: Hello World
Result: {
  "output": "Hello, World!\n",
  "exitCode": 0,
  "executionTime": 0.123
}

Test 2: Basic Math
Result: {
  "output": "Sum: 30\nProduct: 200\n",
  "exitCode": 0,
  "executionTime": 0.156
}
```

## Error Handling

The script handles various error scenarios:
- API connection errors
- Execution timeouts
- Resource limits
- Security violations

## License

Copyright (c) Aevatar. All rights reserved.