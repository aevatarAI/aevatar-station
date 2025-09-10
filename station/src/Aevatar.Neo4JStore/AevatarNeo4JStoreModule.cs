using Aevatar.Neo4JStore.Options;
using Microsoft.Extensions.Configuration;
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
        var configurationSection = configuration.GetSection("Neo4j");
        context.Services.Configure<Neo4JConfigOptions>(configuration.GetSection("Neo4j"));
        
        var options = new Neo4JConfigOptions();
        configurationSection.Bind(options);
        context.Services.AddSingleton<IDriver>(_ => GraphDatabase.Driver(
                options.Uri,
                AuthTokens.Basic(
                    options.User,
                    options.Password),
                o => o.WithMaxConnectionPoolSize(options.MaxConnectionPoolSize)
                    .WithConnectionTimeout(TimeSpan.FromMilliseconds(options.ConnectionTimeout))
                    .WithConnectionAcquisitionTimeout(TimeSpan.FromMilliseconds(options.ConnectionAcquisitionTimeout))
                    .WithMaxConnectionLifetime(TimeSpan.FromMilliseconds(options.MaxConnectionLifetime))
            )
        );
    }
}