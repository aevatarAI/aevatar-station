#!/bin/bash
# ABOUTME: Initialize K3s cluster for regression testing
# ABOUTME: Creates namespace and exports kubeconfig for services

set -e

echo "Setting up K3s cluster for regression testing..."

# Wait for kubeconfig to be available
echo "Waiting for kubeconfig to be ready..."
for i in {1..60}; do
    if [ -s "/shared/kubeconfig" ]; then
        echo "✓ Kubeconfig found and has content"
        break
    fi
    echo "Attempt $i/60: Waiting for kubeconfig..."
    sleep 2
done

if [ ! -s "/shared/kubeconfig" ]; then
    echo "✗ Kubeconfig not found or empty after 60 attempts"
    exit 1
fi

# Update server URL to use container name for internal Docker network access
sed -i 's|server: https://127.0.0.1:6443|server: https://k3s:6443|' /shared/kubeconfig

# Test cluster connectivity
echo "Testing cluster connectivity..."
kubectl --kubeconfig=/shared/kubeconfig --insecure-skip-tls-verify cluster-info

# Create namespace for test applications
echo "Creating aevatar-apps namespace..."
kubectl --kubeconfig=/shared/kubeconfig --insecure-skip-tls-verify create namespace aevatar-apps || true

# Create any required RBAC resources
echo "Setting up RBAC..."
kubectl --kubeconfig=/shared/kubeconfig --insecure-skip-tls-verify apply -f - <<EOF
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

# Create host-accessible kubeconfig for the run script
cp /shared/kubeconfig /shared/kubeconfig-host
sed -i 's|server: https://k3s:6443|server: https://127.0.0.1:6443|' /shared/kubeconfig-host

echo "✓ K3s cluster setup complete"
echo "Namespace: aevatar-apps"
echo "Kubeconfig ready at: /kubeconfig"
echo "Host kubeconfig ready at: /shared/kubeconfig-host"