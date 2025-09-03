using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Router;

[DependsOn(
    typeof(AbpAutoMapperModule)
)]
public class AevatarGAgentsRouterModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarGAgentsRouterModule>();
        });
    }
}