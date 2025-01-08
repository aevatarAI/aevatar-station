using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.CombinationAgent;

[DependsOn(
    typeof(AbpAutoMapperModule),
    typeof(AevatarApplicationContractsModule)
)]
public class AevatarCombinationAgentModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarCombinationAgentModule>(); });
    }
}