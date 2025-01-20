using Aevatar.TestBase;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Tests;

[DependsOn(
    typeof(AevatarTestBaseModule),
    typeof(AbpEventBusModule)
)]
public class AevatarGAgentsTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarGAgentsTestModule>(); });
        context.Services.AddSingleton(new ApplicationPartManager());
    }
}