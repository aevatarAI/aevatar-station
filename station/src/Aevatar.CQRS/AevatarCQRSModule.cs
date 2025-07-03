using System;
using System.Linq;
using Aevatar.CQRS.Handler;
using Aevatar.CQRS.Provider;
using Aevatar.Options;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.CQRS;

public class AevatarCQRSModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarCQRSModule>(); });
        context.Services.AddAutoMapper(typeof(AISmartCQRSAutoMapperProfile).Assembly);
        context.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(SaveStateBatchCommandHandler).Assembly)
        );
        context.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetStateQueryHandler).Assembly)
        );
        context.Services.AddSingleton<IIndexingService, ElasticIndexingService>();
        context.Services.AddSingleton<ICQRSProvider, CQRSProvider>();
        context.Services.UseElasticIndexingWithMetrics();
        var configuration = context.Services.GetConfiguration();
        ConfigureElasticsearch(context, configuration);
        Configure<ProjectorBatchOptions>(configuration.GetSection("ProjectorBatch"));
    }

    private static void ConfigureElasticsearch(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton<ElasticsearchClient>(sp =>
        {
            var uris = configuration.GetSection("ElasticUris:Uris").Get<string[]>();
            if (uris == null || uris.Length == 0)
            {
                throw new ArgumentNullException("ElasticUris:Uris", "Elasticsearch URIs cannot be null or empty.");
            }

            var nodes = uris.Select(uri => new Uri(uri)).ToArray();
            var connectionPool = new StaticNodePool(nodes);

            var settings = new ElasticsearchClientSettings(connectionPool)
                .EnableHttpCompression();
            return new ElasticsearchClient(settings);
        });
    }
}