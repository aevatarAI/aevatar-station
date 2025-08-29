using Aevatar.GAgents.PumpFun.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.PumpFun;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AevatarGAgentsPumpFunModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarGAgentsPumpFunModule>();
        });
        var configuration = context.Services.GetConfiguration();
        Configure<PumpfunOptions>(configuration.GetSection("Pumpfun")); 

    }
}