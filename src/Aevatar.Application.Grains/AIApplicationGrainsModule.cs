using Aevatar.CQRS;
using Aevatar.GAgents.Twitter;
using Aevatar.Neo4JStore;
using Org.BouncyCastle.Asn1.X509.Qualified;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace Aevatar.Application.Grains;

[DependsOn(
    typeof(AbpAutoMapperModule),
    typeof(AbpEventBusModule),
    typeof(AevatarApplicationContractsModule),
    typeof(AevatarCQRSModule),
    typeof(AevatarNeo4JStoreModule),
    typeof(AevatarGAgentsTwitterModule),
    typeof(AevatarGAgentsTelegramModule)
)]
public class AIApplicationGrainsModule : AbpModule
 
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AIApplicationGrainsModule>(); });
    }
}
