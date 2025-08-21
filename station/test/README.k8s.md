# Kubernetes Python Sandbox Test Scripts

This directory contains scripts for testing Python code execution in Kubernetes sandbox environments.

## Scripts

1. `test_sandbox_k8s.sh` - Basic sandbox tests
   - Basic Python execution
   - Math operations
   - Exception handling
   - File system access restrictions
   - CPU intensive operations
   - Memory usage tests

2. `test_sandbox_k8s_advanced.sh` - Advanced tests with dependencies
   - NumPy array operations
   - Pandas data analysis
   - SciPy scientific computing
   - Machine learning with scikit-learn

## Prerequisites

- Kubernetes cluster (or local K3d/Kind)
- kubectl configured
- Python 3.11 base image available

## Usage

### Basic Tests

```bash
# Make script executable
chmod +x test_sandbox_k8s.sh

# Run basic tests
./test_sandbox_k8s.sh
```

### Advanced Tests

```bash
# Make script executable
chmod +x test_sandbox_k8s_advanced.sh

# Run advanced tests
./test_sandbox_k8s_advanced.sh
```

## Test Environment

The tests run in a dedicated namespace `sandbox-test` with:
- Resource limits (CPU: 100m, Memory: 256Mi)
- Security context restrictions
- No persistent storage
- No network access (by default)

## Test Categories

### Basic Tests

1. Basic Python Code
   - Simple print statement
   - Basic syntax verification

2. Math Operations
   - Basic calculations
   - Math module usage

3. Exception Handling
   - Division by zero
   - Error catching

4. Security Tests
   - File system access attempts
   - Permission restrictions

5. Resource Usage
   - CPU intensive operations
   - Memory allocation tests

### Advanced Tests

1. NumPy Tests
   - Array operations
   - Matrix calculations
   - Linear algebra

2. Pandas Tests
   - DataFrame creation
   - Data analysis
   - Statistical operations

3. SciPy Tests
   - Statistical tests
   - Optimization problems
   - Scientific calculations

4. Machine Learning Tests
   - Data generation
   - Model training
   - Prediction and evaluation

## Output Format

Each test provides:
- Test name and description
- Execution status
- Output/logs
- Error messages (if any)
- Resource usage statistics

## Cleanup

Both scripts automatically clean up:
- Test namespace
- Jobs and pods
- ConfigMaps
- Volumes

## Troubleshooting

### Common Issues

1. Pod startup failures
   ```bash
   kubectl describe pod -n sandbox-test
   ```

2. Resource limits
   ```bash
   kubectl top pod -n sandbox-test
   ```

3. Job status
   ```bash
   kubectl get jobs -n sandbox-test
   ```

### Logs

```bash
# Get logs for specific job
kubectl logs job/job-name -n sandbox-test

# Get all pods in namespace
kubectl get pods -n sandbox-test
```

## License

Copyright (c) Aevatar. All rights reserved.