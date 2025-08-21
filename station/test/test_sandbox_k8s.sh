#!/bin/bash

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# 配置参数
NAMESPACE="sandbox-test"
PYTHON_IMAGE="python:3.11-slim"
MEMORY_LIMIT="256Mi"
CPU_LIMIT="100m"

echo -e "${GREEN}=== Python Sandbox K8s Test ===${NC}"

# 创建测试命名空间
create_namespace() {
    echo -e "\n${YELLOW}Creating test namespace...${NC}"
    kubectl create namespace $NAMESPACE 2>/dev/null || true
}

# 清理资源
cleanup() {
    echo -e "\n${YELLOW}Cleaning up resources...${NC}"
    kubectl delete namespace $NAMESPACE --wait=false 2>/dev/null || true
}

# 等待Job完成
wait_for_job() {
    local job_name=$1
    local timeout=30
    local counter=0
    
    echo -e "\n${YELLOW}Waiting for job completion...${NC}"
    while [ $counter -lt $timeout ]; do
        status=$(kubectl get job $job_name -n $NAMESPACE -o jsonpath='{.status.conditions[?(@.type=="Complete")].status}' 2>/dev/null)
        if [ "$status" == "True" ]; then
            return 0
        fi
        failed=$(kubectl get job $job_name -n $NAMESPACE -o jsonpath='{.status.conditions[?(@.type=="Failed")].status}' 2>/dev/null)
        if [ "$failed" == "True" ]; then
            return 1
        fi
        sleep 1
        ((counter++))
    done
    return 2
}

# 获取Job日志
get_job_logs() {
    local job_name=$1
    local pod_name=$(kubectl get pods -n $NAMESPACE -l job-name=$job_name -o jsonpath='{.items[0].metadata.name}' 2>/dev/null)
    if [ ! -z "$pod_name" ]; then
        kubectl logs $pod_name -n $NAMESPACE
    fi
}

# 运行Python代码测试
run_python_test() {
    local test_name=$1
    local python_code=$2
    local job_name="python-test-${test_name,,}"
    
    echo -e "\n${GREEN}Running test: ${test_name}${NC}"
    
    # 创建Job配置
    cat <<EOF | kubectl apply -n $NAMESPACE -f -
apiVersion: batch/v1
kind: Job
metadata:
  name: $job_name
spec:
  ttlSecondsAfterFinished: 60
  template:
    spec:
      containers:
      - name: python
        image: $PYTHON_IMAGE
        command: ["python", "-c", "$python_code"]
        resources:
          limits:
            memory: $MEMORY_LIMIT
            cpu: $CPU_LIMIT
          requests:
            memory: "128Mi"
            cpu: "50m"
        securityContext:
          allowPrivilegeEscalation: false
          runAsNonRoot: true
          runAsUser: 1000
          capabilities:
            drop:
              - ALL
      restartPolicy: Never
EOF

    # 等待Job完成
    wait_for_job $job_name
    local result=$?
    
    # 获取并显示日志
    echo -e "\n${YELLOW}Test output:${NC}"
    get_job_logs $job_name
    
    # 显示结果
    if [ $result -eq 0 ]; then
        echo -e "\n${GREEN}Test completed successfully${NC}"
    elif [ $result -eq 1 ]; then
        echo -e "\n${RED}Test failed${NC}"
    else
        echo -e "\n${RED}Test timed out${NC}"
    fi
    
    # 清理Job
    kubectl delete job $job_name -n $NAMESPACE --wait=false 2>/dev/null || true
}

# 主测试流程
main() {
    # 清理旧资源并创建新的命名空间
    cleanup
    create_namespace
    
    # 测试1: 基本Python代码
    run_python_test "BasicTest" "print('Hello from Python sandbox!')"
    
    # 测试2: 数学计算
    run_python_test "MathTest" "import math; print(f'Pi is approximately {math.pi:.5f}')"
    
    # 测试3: 异常处理
    run_python_test "ExceptionTest" "try:
    x = 1/0
except Exception as e:
    print(f'Caught error: {e}')"
    
    # 测试4: 文件系统访问（应该失败）
    run_python_test "FileSystemTest" "try:
    with open('/etc/passwd', 'r') as f:
        print(f.read())
except Exception as e:
    print(f'Access denied: {e}')"
    
    # 测试5: CPU密集型操作
    run_python_test "CPUTest" "result = sum(i * i for i in range(1000000)); print(f'Sum of squares: {result}')"
    
    # 测试6: 内存使用
    run_python_test "MemoryTest" "try:
    list_data = list(range(1000000))
    print(f'Created list with {len(list_data)} items')
except Exception as e:
    print(f'Memory error: {e}')"
    
    # 最后清理资源
    cleanup
}

# 运行测试
main