using Aevatar.Sandbox.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.Sandbox.Core;

public class AevatarSandboxCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        context.Services.Configure<SandboxOptions>(configuration.GetSection("Sandbox:Options"));
        context.Services.Configure<SandboxDispatcherOptions>(configuration.GetSection("Sandbox:Dispatcher"));

        context.Services.AddSingleton<SandboxExecDispatcher>();
    }
}