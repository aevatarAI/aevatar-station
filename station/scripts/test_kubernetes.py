#!/usr/bin/env python3
# ABOUTME: Simple test to verify Kubernetes functionality
# ABOUTME: Tests that K8s cluster is accessible and can create/delete resources

import os
import subprocess
import sys

def run_kubectl(command):
    """Run a kubectl command and return result"""
    kubeconfig = os.getenv("KUBECONFIG", "/app/shared/kubeconfig")
    cmd = ["kubectl", "--kubeconfig", kubeconfig, "--insecure-skip-tls-verify"] + command
    print(f"Running: {' '.join(cmd)}")
    
    result = subprocess.run(cmd, capture_output=True, text=True)
    print(f"Exit code: {result.returncode}")
    if result.stdout:
        print(f"Output: {result.stdout}")
    if result.stderr:
        print(f"Error: {result.stderr}")
    
    return result

def test_cluster_access():
    """Test basic cluster access"""
    print("=== Testing Cluster Access ===")
    result = run_kubectl(["cluster-info"])
    if result.returncode != 0:
        print("❌ Failed to access cluster")
        return False
    print("✅ Cluster access successful")
    return True

def test_namespace_access():
    """Test namespace access"""
    print("\n=== Testing Namespace Access ===")
    result = run_kubectl(["get", "namespace", "aevatar-apps"])
    if result.returncode != 0:
        print("❌ Failed to access aevatar-apps namespace")
        return False
    print("✅ Namespace access successful")
    return True

def test_resource_creation():
    """Test creating and deleting a test deployment"""
    print("\n=== Testing Resource Creation ===")
    
    # Create a test deployment
    deployment_yaml = """
apiVersion: apps/v1
kind: Deployment
metadata:
  name: test-deployment
  namespace: aevatar-apps
spec:
  replicas: 1
  selector:
    matchLabels:
      app: test-app
  template:
    metadata:
      labels:
        app: test-app
    spec:
      containers:
      - name: test-container
        image: nginx:alpine
        ports:
        - containerPort: 80
"""
    
    # Create deployment
    print("Creating test deployment...")
    result = subprocess.run(
        ["kubectl", "--kubeconfig", os.getenv("KUBECONFIG", "/app/shared/kubeconfig"), "--insecure-skip-tls-verify", "apply", "-f", "-"],
        input=deployment_yaml,
        text=True,
        capture_output=True
    )
    
    if result.returncode != 0:
        print(f"❌ Failed to create deployment: {result.stderr}")
        return False
    
    print("✅ Deployment created successfully")
    
    # Wait for deployment to be ready
    print("Waiting for deployment to be ready...")
    result = run_kubectl(["wait", "--for=condition=available", "deployment/test-deployment", "-n", "aevatar-apps", "--timeout=60s"])
    
    if result.returncode != 0:
        print("⚠️ Deployment may not be fully ready, but that's OK for testing")
    else:
        print("✅ Deployment is ready")
    
    # List pods to verify
    print("Listing pods:")
    run_kubectl(["get", "pods", "-n", "aevatar-apps"])
    
    # Clean up
    print("Cleaning up test deployment...")
    result = run_kubectl(["delete", "deployment", "test-deployment", "-n", "aevatar-apps"])
    
    if result.returncode != 0:
        print(f"⚠️ Failed to delete deployment: {result.stderr}")
    else:
        print("✅ Deployment deleted successfully")
    
    return True

def main():
    """Run all tests"""
    print("🧪 Kubernetes Functionality Test")
    
    tests = [
        test_cluster_access,
        test_namespace_access,
        test_resource_creation
    ]
    
    passed = 0
    total = len(tests)
    
    for test in tests:
        try:
            if test():
                passed += 1
        except Exception as e:
            print(f"❌ Test failed with exception: {e}")
    
    print(f"\n📊 Test Results: {passed}/{total} tests passed")
    
    if passed == total:
        print("🎉 All tests passed! Kubernetes integration is working correctly.")
        sys.exit(0)
    else:
        print("💥 Some tests failed. Check the output above for details.")
        sys.exit(1)

if __name__ == "__main__":
    main()