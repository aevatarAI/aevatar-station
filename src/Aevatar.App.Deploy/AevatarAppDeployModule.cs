using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.App.Deploy;

public class AevatarAppDeployModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        services.AddTransient(typeof(IAppDeployManager), typeof(DefaultAppDeployManager));
    }
}