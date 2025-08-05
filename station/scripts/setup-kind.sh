#!/bin/bash
# ABOUTME: Initialize Kind cluster for regression testing
# ABOUTME: Creates namespace and exports kubeconfig for services

set -e

echo "Setting up Kind cluster for regression testing..."

# Wait for Kind API to be available
echo "Waiting for Kubernetes API..."
for i in {1..60}; do
    if kubectl --server=https://regression-kind-cluster:6443 --insecure-skip-tls-verify cluster-info &>/dev/null; then
        echo "✓ Kubernetes API is ready"
        break
    fi
    echo "Attempt $i/60: Waiting for API..."
    sleep 2
done

# Get cluster credentials
echo "Exporting kubeconfig..."
kubectl --server=https://regression-kind-cluster:6443 --insecure-skip-tls-verify \
    config view --raw > /kubeconfig

# Update kubeconfig to use container name as server
sed -i 's|server:.*|server: https://kind:6443|' /kubeconfig

# Create namespace for test applications
echo "Creating aevatar-apps namespace..."
kubectl --kubeconfig=/kubeconfig create namespace aevatar-apps || true

# Create any required RBAC resources
echo "Setting up RBAC..."
kubectl --kubeconfig=/kubeconfig apply -f - <<EOF
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: aevatar-deployer
rules:
- apiGroups: ["", "apps", "networking.k8s.io"]
  resources: ["deployments", "services", "configmaps", "pods", "ingresses"]
  verbs: ["create", "get", "list", "update", "patch", "delete"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: aevatar-deployer-binding
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: aevatar-deployer
subjects:
- kind: ServiceAccount
  name: default
  namespace: aevatar-apps
EOF

echo "✓ Kind cluster setup complete"
echo "Namespace: aevatar-apps"
echo "Kubeconfig exported to: /kubeconfig"