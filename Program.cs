using System;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aevatar.K8sTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Aevatar K8s Connection Test");
            Console.WriteLine("================================");

            try
            {
                // 1. 测试K8s配置加载
                Console.WriteLine("📋 1. Loading K8s Configuration...");
                var config = KubernetesClientConfiguration.BuildDefaultConfig();
                var client = new Kubernetes(config);
                Console.WriteLine("✅ K8s client created successfully");
                Console.WriteLine($"   - API Server: {config.Host}");
                
                // 2. 测试集群连接
                Console.WriteLine("\n🔗 2. Testing cluster connection...");
                var version = await client.VersionAsync();
                Console.WriteLine("✅ Connected to K8s cluster successfully");
                Console.WriteLine($"   - Server Version: {version.GitVersion}");
                Console.WriteLine($"   - Platform: {version.Platform}");

                // 3. 测试Pod列表获取
                Console.WriteLine("\n📦 3. Testing Pod listing...");
                var pods = await client.CoreV1.ListPodForAllNamespacesAsync();
                Console.WriteLine($"✅ Found {pods.Items.Count} pods in cluster");
                
                // 显示前5个Pod
                Console.WriteLine("   Sample pods:");
                for (int i = 0; i < Math.Min(5, pods.Items.Count); i++)
                {
                    var pod = pods.Items[i];
                    Console.WriteLine($"     - {pod.Metadata.Name} ({pod.Metadata.NamespaceProperty}) - {pod.Status.Phase}");
                }

                // 4. 测试Deployment列表获取
                Console.WriteLine("\n🚢 4. Testing Deployment listing...");
                var deployments = await client.AppsV1.ListDeploymentForAllNamespacesAsync();
                Console.WriteLine($"✅ Found {deployments.Items.Count} deployments in cluster");
                
                // 显示前3个Deployment
                Console.WriteLine("   Sample deployments:");
                for (int i = 0; i < Math.Min(3, deployments.Items.Count); i++)
                {
                    var deployment = deployments.Items[i];
                    var replicas = deployment.Status?.Replicas ?? 0;
                    var readyReplicas = deployment.Status?.ReadyReplicas ?? 0;
                    Console.WriteLine($"     - {deployment.Metadata.Name} ({deployment.Metadata.NamespaceProperty}) - {readyReplicas}/{replicas} ready");
                }

                // 5. 测试ConfigMap操作
                Console.WriteLine("\n⚙️  5. Testing ConfigMap operations...");
                var configMaps = await client.CoreV1.ListConfigMapForAllNamespacesAsync();
                Console.WriteLine($"✅ Found {configMaps.Items.Count} configmaps in cluster");

                // 6. 模拟Cross-URL数据
                Console.WriteLine("\n🌐 6. Mock Cross-URL Data Test...");
                var mockCrossUrls = new[]
                {
                    "https://api.example.com",
                    "https://webhook.test.com",
                    "https://cors.allowed.com"
                };
                
                Console.WriteLine("✅ Mock Cross-URL data:");
                foreach (var url in mockCrossUrls)
                {
                    Console.WriteLine($"     - {url}");
                }

                // 7. 模拟服务状态
                Console.WriteLine("\n📊 7. Mock Service Status Test...");
                Console.WriteLine("✅ Mock service status for clientId 'test-client':");
                Console.WriteLine("     - Status: Running");
                Console.WriteLine("     - Ready Replicas: 1/1");
                Console.WriteLine("     - Last Restart: 2024-12-30T02:30:00Z");
                Console.WriteLine("     - Uptime: 15 minutes");

                Console.WriteLine("\n🎉 All K8s tests passed successfully!");
                Console.WriteLine("✅ Your local K8s cluster is ready for Aevatar services");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ K8s test failed: {ex.Message}");
                Console.WriteLine($"   Exception Type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine("\n🔧 Troubleshooting tips:");
                Console.WriteLine("   1. Make sure Docker Desktop is running");
                Console.WriteLine("   2. Enable Kubernetes in Docker Desktop settings");
                Console.WriteLine("   3. Check if kubectl works: kubectl cluster-info");
                Console.WriteLine("   4. Verify kubeconfig: ~/.kube/config");
                return;
            }
        }
    }
} 