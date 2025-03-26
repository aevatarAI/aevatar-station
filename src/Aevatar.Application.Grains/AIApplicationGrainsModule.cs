using Aevatar.CQRS;
using Aevatar.Neo4JStore;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using MineAiFun.Silo;

namespace Aevatar.Application.Grains;

[DependsOn(
    typeof(AbpAutoMapperModule),
    typeof(AbpEventBusModule),
    typeof(AevatarApplicationContractsModule),
    typeof(AevatarCQRSModule),
    typeof(AevatarNeo4JStoreModule),
    typeof(MineAiFunGAgentsModule)
)]
public class AIApplicationGrainsModule : AbpModule
 
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AIApplicationGrainsModule>(); });
    }
}