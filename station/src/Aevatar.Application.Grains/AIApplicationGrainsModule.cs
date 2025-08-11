using Aevatar.CQRS;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP;
using Aevatar.GAgents.Twitter;
using Aevatar.Neo4JStore;
using Microsoft.Extensions.DependencyInjection;
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
    typeof(AevatarGAgentsMCPModule)
)]
public class AIApplicationGrainsModule : AbpModule
 
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IGAgentExecutor, GAgentExecutor>();
        context.Services.AddTransient<IGAgentService, GAgentService>();
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AIApplicationGrainsModule>(); });
    }
}