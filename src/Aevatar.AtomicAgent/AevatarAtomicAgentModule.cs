using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.AtomicAgent;

[DependsOn(
    typeof(AbpAutoMapperModule),
    typeof(AevatarApplicationContractsModule)
)]
public class AevatarAtomicAgentModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarAtomicAgentModule>(); });
    }
}