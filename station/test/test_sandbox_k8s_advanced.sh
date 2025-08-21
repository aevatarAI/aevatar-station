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

# 创建ConfigMap来存储Python代码
create_python_configmap() {
    local name=$1
    local code=$2
    
    kubectl create configmap $name -n $NAMESPACE --from-literal=script.py="$code" 2>/dev/null || \
    kubectl create configmap $name -n $NAMESPACE --from-literal=script.py="$code" -o yaml --dry-run=client | kubectl replace -f -
}

# 创建带有依赖的Job
create_job_with_deps() {
    local job_name=$1
    local configmap_name=$2
    local pip_packages=$3
    
    cat <<EOF | kubectl apply -n $NAMESPACE -f -
apiVersion: batch/v1
kind: Job
metadata:
  name: $job_name
spec:
  ttlSecondsAfterFinished: 60
  template:
    spec:
      initContainers:
      - name: install-deps
        image: $PYTHON_IMAGE
        command: ["pip", "install", "--no-cache-dir"]
        args: $pip_packages
        volumeMounts:
        - name: packages
          mountPath: /packages
      containers:
      - name: python
        image: $PYTHON_IMAGE
        command: ["python", "/scripts/script.py"]
        resources:
          limits:
            memory: $MEMORY_LIMIT
            cpu: $CPU_LIMIT
          requests:
            memory: "128Mi"
            cpu: "50m"
        volumeMounts:
        - name: scripts
          mountPath: /scripts
        - name: packages
          mountPath: /usr/local/lib/python3.11/site-packages
        securityContext:
          allowPrivilegeEscalation: false
          runAsNonRoot: true
          runAsUser: 1000
          capabilities:
            drop:
              - ALL
      volumes:
      - name: scripts
        configMap:
          name: $configmap_name
      - name: packages
        emptyDir: {}
      restartPolicy: Never
EOF
}

# 高级测试用例
run_numpy_test() {
    echo -e "\n${GREEN}Running NumPy Test${NC}"
    
    local test_name="numpy-test"
    local code="
import numpy as np

# 创建示例数组
arr = np.array([[1, 2, 3], [4, 5, 6]])
print('Array shape:', arr.shape)
print('Array mean:', arr.mean())
print('Array sum:', arr.sum())

# 矩阵运算
print('\\nMatrix operations:')
print('Original:\\n', arr)
print('Transpose:\\n', arr.T)
print('Dot product:\\n', np.dot(arr, arr.T))
"
    
    create_python_configmap "${test_name}-cm" "$code"
    create_job_with_deps $test_name "${test_name}-cm" '["numpy"]'
    
    wait_for_job $test_name
    get_job_logs $test_name
}

run_pandas_test() {
    echo -e "\n${GREEN}Running Pandas Test${NC}"
    
    local test_name="pandas-test"
    local code="
import pandas as pd
import numpy as np

# 创建示例数据框
df = pd.DataFrame({
    'A': np.random.rand(5),
    'B': np.random.randint(0, 100, 5),
    'C': ['foo', 'bar', 'baz', 'qux', 'quux']
})

print('DataFrame:\\n', df)
print('\\nSummary statistics:\\n', df.describe())
print('\\nGroupby operation:\\n', df.groupby('C').mean())
"
    
    create_python_configmap "${test_name}-cm" "$code"
    create_job_with_deps $test_name "${test_name}-cm" '["pandas", "numpy"]'
    
    wait_for_job $test_name
    get_job_logs $test_name
}

run_scipy_test() {
    echo -e "\n${GREEN}Running SciPy Test${NC}"
    
    local test_name="scipy-test"
    local code="
import numpy as np
from scipy import stats
from scipy import optimize

# 统计测试
x = np.random.normal(0, 1, 1000)
print('Normal distribution test:', stats.normaltest(x))

# 优化问题
def f(x):
    return (x[0] - 1)**2 + (x[1] - 2)**2

result = optimize.minimize(f, [0, 0])
print('\\nOptimization result:\\n', result)
"
    
    create_python_configmap "${test_name}-cm" "$code"
    create_job_with_deps $test_name "${test_name}-cm" '["scipy", "numpy"]'
    
    wait_for_job $test_name
    get_job_logs $test_name
}

run_ml_test() {
    echo -e "\n${GREEN}Running Machine Learning Test${NC}"
    
    local test_name="ml-test"
    local code="
from sklearn.datasets import make_classification
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
import numpy as np

# 生成示例数据
X, y = make_classification(n_samples=100, n_features=4, random_state=42)
X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2)

# 训练模型
clf = RandomForestClassifier(n_estimators=10)
clf.fit(X_train, y_train)

# 评估模型
score = clf.score(X_test, y_test)
print(f'Model accuracy: {score:.2f}')

# 特征重要性
print('\\nFeature importance:')
for i, importance in enumerate(clf.feature_importances_):
    print(f'Feature {i}: {importance:.4f}')
"
    
    create_python_configmap "${test_name}-cm" "$code"
    create_job_with_deps $test_name "${test_name}-cm" '["scikit-learn", "numpy"]'
    
    wait_for_job $test_name
    get_job_logs $test_name
}

# 主测试流程
main() {
    # 清理旧资源并创建新的命名空间
    kubectl delete namespace $NAMESPACE --wait=false 2>/dev/null || true
    kubectl create namespace $NAMESPACE
    
    # 运行高级测试
    run_numpy_test
    run_pandas_test
    run_scipy_test
    run_ml_test
    
    # 清理资源
    kubectl delete namespace $NAMESPACE --wait=false
}

# 运行测试
main