using Aevatar.Sandbox.Kubernetes.Adapter;
using Aevatar.Sandbox.Kubernetes.Manager;
using k8s;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.Sandbox.Kubernetes;

public class SandboxKubernetesModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var config = KubernetesClientConfiguration.BuildDefaultConfig();
        context.Services.AddSingleton<IKubernetes>(_ => new k8s.Kubernetes(config));
        context.Services.AddSingleton<ISandboxKubernetesClientAdapter, SandboxKubernetesClientAdapter>();
        context.Services.AddSingleton<ISandboxKubernetesManager, SandboxKubernetesManager>();
    }
}