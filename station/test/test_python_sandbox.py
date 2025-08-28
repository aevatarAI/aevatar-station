#!/usr/bin/env python3
"""
Python Sandbox Execution Test Script
This script tests various scenarios for Python code execution in the sandbox environment.
"""

import json
import time
import requests
import argparse
from typing import Dict, Any

class SandboxTester:
    def __init__(self, base_url: str = "http://localhost:5000"):
        self.base_url = base_url.rstrip('/')
        self.headers = {
            "Content-Type": "application/json"
        }

    def execute_code(self, code: str, timeout: int = 30, resources: Dict[str, str] = None) -> Dict[str, Any]:
        """Execute Python code in sandbox and return the result."""
        payload = {
            "language": "python",
            "code": code,
            "timeout": timeout
        }
        if resources:
            payload["resources"] = resources

        response = requests.post(
            f"{self.base_url}/api/sandbox/execute",
            headers=self.headers,
            json=payload
        )
        return response.json()

    def execute_async(self, code: str, timeout: int = 30) -> str:
        """Execute code asynchronously and return execution ID."""
        payload = {
            "language": "python",
            "code": code,
            "timeout": timeout
        }
        response = requests.post(
            f"{self.base_url}/api/sandbox/execute-async",
            headers=self.headers,
            json=payload
        )
        return response.json()["executionId"]

    def get_status(self, execution_id: str) -> Dict[str, Any]:
        """Get execution status by ID."""
        response = requests.get(
            f"{self.base_url}/api/sandbox/status/{execution_id}",
            headers=self.headers
        )
        return response.json()

    def get_logs(self, execution_id: str) -> str:
        """Get execution logs by ID."""
        response = requests.get(
            f"{self.base_url}/api/sandbox/logs/{execution_id}",
            headers=self.headers
        )
        return response.text

def run_basic_tests(tester: SandboxTester):
    """Run basic functionality tests."""
    print("\n=== Running Basic Tests ===")
    
    # Test 1: Hello World
    print("\nTest 1: Hello World")
    result = tester.execute_code('print("Hello, World!")')
    print(f"Result: {json.dumps(result, indent=2)}")

    # Test 2: Basic Math
    print("\nTest 2: Basic Math")
    result = tester.execute_code('''
x = 10
y = 20
print(f"Sum: {x + y}")
print(f"Product: {x * y}")
    ''')
    print(f"Result: {json.dumps(result, indent=2)}")

def run_numpy_tests(tester: SandboxTester):
    """Run tests with NumPy package."""
    print("\n=== Running NumPy Tests ===")

    # Test 1: Basic Array Operations
    print("\nTest 1: Basic Array Operations")
    result = tester.execute_code('''
import numpy as np
arr = np.array([1, 2, 3, 4, 5])
print(f"Array: {arr}")
print(f"Mean: {arr.mean()}")
print(f"Sum: {arr.sum()}")
    ''')
    print(f"Result: {json.dumps(result, indent=2)}")

    # Test 2: Matrix Operations
    print("\nTest 2: Matrix Operations")
    result = tester.execute_code('''
import numpy as np
matrix = np.array([[1, 2], [3, 4]])
print(f"Matrix:\\n{matrix}")
print(f"Transpose:\\n{matrix.T}")
print(f"Determinant: {np.linalg.det(matrix)}")
    ''')
    print(f"Result: {json.dumps(result, indent=2)}")

def run_resource_tests(tester: SandboxTester):
    """Run resource limit tests."""
    print("\n=== Running Resource Tests ===")

    # Test 1: CPU Intensive
    print("\nTest 1: CPU Intensive")
    result = tester.execute_code('''
def fibonacci(n):
    if n <= 1:
        return n
    return fibonacci(n-1) + fibonacci(n-2)

print(fibonacci(35))
    ''', resources={"cpu": "100m", "memory": "128Mi"})
    print(f"Result: {json.dumps(result, indent=2)}")

    # Test 2: Memory Intensive
    print("\nTest 2: Memory Intensive")
    result = tester.execute_code('''
import numpy as np
try:
    # Try to allocate a large array
    arr = np.zeros((10000, 10000))
    print(f"Shape: {arr.shape}")
    print(f"Size in MB: {arr.nbytes / 1024 / 1024}")
except Exception as e:
    print(f"Error: {e}")
    ''', resources={"cpu": "100m", "memory": "256Mi"})
    print(f"Result: {json.dumps(result, indent=2)}")

def run_async_tests(tester: SandboxTester):
    """Run asynchronous execution tests."""
    print("\n=== Running Async Tests ===")

    # Test 1: Long Running Task
    print("\nTest 1: Long Running Task")
    code = '''
import time
for i in range(5):
    print(f"Step {i}")
    time.sleep(1)
print("Task completed")
    '''
    
    execution_id = tester.execute_async(code)
    print(f"Execution ID: {execution_id}")

    # Poll for status
    for _ in range(10):
        status = tester.get_status(execution_id)
        print(f"Status: {json.dumps(status, indent=2)}")
        if status.get("isCompleted"):
            break
        time.sleep(1)

    # Get logs
    logs = tester.get_logs(execution_id)
    print(f"Logs:\n{logs}")

def run_security_tests(tester: SandboxTester):
    """Run security boundary tests."""
    print("\n=== Running Security Tests ===")

    # Test 1: File System Access
    print("\nTest 1: File System Access")
    result = tester.execute_code('''
try:
    with open('/etc/passwd', 'r') as f:
        print(f.read())
except Exception as e:
    print(f"Error: {e}")
    ''')
    print(f"Result: {json.dumps(result, indent=2)}")

    # Test 2: Network Access
    print("\nTest 2: Network Access")
    result = tester.execute_code('''
try:
    import requests
    response = requests.get('https://api.github.com')
    print(f"Status Code: {response.status_code}")
except Exception as e:
    print(f"Error: {e}")
    ''')
    print(f"Result: {json.dumps(result, indent=2)}")

def main():
    parser = argparse.ArgumentParser(description='Test Python Sandbox Execution')
    parser.add_argument('--url', default='http://localhost:5000',
                      help='Base URL of the sandbox API (default: http://localhost:5000)')
    parser.add_argument('--test', choices=['all', 'basic', 'numpy', 'resource', 'async', 'security'],
                      default='all', help='Specify which tests to run')
    args = parser.parse_args()

    tester = SandboxTester(args.url)

    test_map = {
        'basic': run_basic_tests,
        'numpy': run_numpy_tests,
        'resource': run_resource_tests,
        'async': run_async_tests,
        'security': run_security_tests
    }

    if args.test == 'all':
        for test_func in test_map.values():
            test_func(tester)
    else:
        test_map[args.test](tester)

if __name__ == "__main__":
    main()