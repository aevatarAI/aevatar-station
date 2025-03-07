using Aevatar.Neo4JStore.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.Neo4JStore;

public class AevatarNeo4JStoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarNeo4JStoreModule>();
        });
        
        var configuration = context.Services.GetConfiguration();
        context.Services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(
            configuration["Neo4j:Uri"],
            AuthTokens.Basic(
                configuration["Neo4j:User"],
                configuration["Neo4j:Password"]
            )
        ));
    }
}