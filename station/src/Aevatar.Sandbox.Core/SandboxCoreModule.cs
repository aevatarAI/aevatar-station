using Aevatar.Sandbox.Kubernetes;
using Aevatar.Sandbox.Python.Services;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.Sandbox.Core;

[DependsOn(
    typeof(SandboxKubernetesModule)
)]
public class SandboxCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<PythonSandboxService>();
    }
}