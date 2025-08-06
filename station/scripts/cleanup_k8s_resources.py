#!/usr/bin/env python3
# ABOUTME: Cleanup Kubernetes resources created during regression testing
# ABOUTME: Removes all test resources from aevatar-apps namespace

import os
import subprocess
import logging

logger = logging.getLogger(__name__)

def cleanup_k8s_resources():
    """Clean up all K8s resources created during tests"""
    kubeconfig = os.getenv("KUBECONFIG", "/app/kubeconfig")
    namespace = "aevatar-apps"
    
    if not os.path.exists(kubeconfig):
        logger.warning(f"Kubeconfig not found at {kubeconfig}")
        return
    
    try:
        # Delete all resources in the test namespace
        logger.info(f"Cleaning up resources in namespace: {namespace}")
        
        resources = [
            "deployments",
            "services", 
            "configmaps",
            "pods",
            "ingresses"
        ]
        
        for resource in resources:
            cmd = [
                "kubectl", "--kubeconfig", kubeconfig,
                "--insecure-skip-tls-verify",
                "delete", resource, "--all",
                "-n", namespace,
                "--timeout=30s"
            ]
            
            logger.info(f"Deleting all {resource}...")
            result = subprocess.run(cmd, capture_output=True, text=True)
            
            if result.returncode == 0:
                logger.info(f"✓ Deleted {resource}: {result.stdout}")
            else:
                logger.warning(f"Failed to delete {resource}: {result.stderr}")
                
    except Exception as e:
        logger.error(f"Error during K8s cleanup: {e}")

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)
    cleanup_k8s_resources()