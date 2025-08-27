using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Aevatar.WebHook.Deploy;

public class AevatarWebhookDeployModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        services.AddTransient(typeof(IHostDeployManager), typeof(DefaultHostDeployManager));
    }
}