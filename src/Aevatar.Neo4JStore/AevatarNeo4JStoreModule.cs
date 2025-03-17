using Aevatar.Neo4JStore.Options;
using Microsoft.Extensions.DependencyInjection;
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
        context.Services.Configure<Neo4JConfigOptions>(configuration.GetSection("Neo4j"));
        context.Services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(
                configuration["Neo4j:Uri"],
                AuthTokens.Basic(
                    configuration["Neo4j:User"],
                    configuration["Neo4j:Password"]),
                o => o.WithMaxConnectionPoolSize(2)
                    .WithConnectionTimeout(TimeSpan.FromMilliseconds(10))
                    .WithConnectionAcquisitionTimeout(TimeSpan.FromMilliseconds(10))
                    .WithMaxConnectionLifetime(TimeSpan.FromMilliseconds(10))
            )
        );
    }
}