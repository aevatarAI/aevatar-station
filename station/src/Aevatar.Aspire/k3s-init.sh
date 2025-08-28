#!/bin/bash
# This script initializes the k3s cluster for sandbox code execution

# Wait for k3s to be ready
echo "Waiting for k3s to be ready..."
until kubectl --kubeconfig=./data/k3s/kubeconfig/kubeconfig.yaml get nodes; do
  echo "Waiting for k3s API server..."
  sleep 5
done

# Create sandbox namespace if it doesn't exist
echo "Creating sandbox namespace..."
kubectl --kubeconfig=./data/k3s/kubeconfig/kubeconfig.yaml create namespace sandbox || true

# Apply resource quotas to sandbox namespace
echo "Applying resource quotas to sandbox namespace..."
cat <<EOF | kubectl --kubeconfig=./data/k3s/kubeconfig/kubeconfig.yaml apply -f -
apiVersion: v1
kind: ResourceQuota
metadata:
  name: sandbox-quota
  namespace: sandbox
spec:
  hard:
    requests.cpu: "2"
    requests.memory: 2Gi
    limits.cpu: "4"
    limits.memory: 4Gi
    pods: "10"
    jobs.batch: "5"
EOF

# Apply pod security policies
echo "Applying pod security policies..."
cat <<EOF | kubectl --kubeconfig=./data/k3s/kubeconfig/kubeconfig.yaml apply -f -
apiVersion: v1
kind: LimitRange
metadata:
  name: sandbox-limits
  namespace: sandbox
spec:
  limits:
  - default:
      cpu: 500m
      memory: 512Mi
    defaultRequest:
      cpu: 200m
      memory: 256Mi
    max:
      cpu: 1
      memory: 1Gi
    min:
      cpu: 100m
      memory: 64Mi
    type: Container
EOF

echo "K3s initialization complete!"