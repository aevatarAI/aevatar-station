using System;
using System.Linq;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Handler;
using Aevatar.CQRS.Provider;
using Aevatar.Options;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
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
            cfg.RegisterServicesFromAssembly(typeof(SaveGEventCommandHandler).Assembly)
        );
        context.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(SendEventCommandHandler).Assembly)
        );
        context.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetStateQueryHandler).Assembly)
        );
        context.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetGEventQueryHandler).Assembly)
        );
        context.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetUserInstanceAgentsHandler).Assembly)
        );
        context.Services.AddSingleton<IIndexingService, ElasticIndexingService>();
        context.Services.AddSingleton<IEventDispatcher, CQRSProvider>();
        context.Services.AddSingleton<ICQRSProvider, CQRSProvider>();
        var configuration = context.Services.GetConfiguration();
        ConfigureElasticsearch(context, configuration);
        Configure<ProjectorBatchOptions>(configuration.GetSection("ProjectorBatch"));
    }

    private static void ConfigureElasticsearch(
        ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton<IElasticClient>(sp =>
        {
            var uris = configuration.GetSection("ElasticUris:Uris").Get<string[]>();
            if (uris == null || uris.Length == 0)
            {
                throw new ArgumentNullException("ElasticUris:Uris", "Elasticsearch URIs cannot be null or empty.");
            }

            var settings =
                new ConnectionSettings(new StaticConnectionPool(uris.Select(uri => new Uri(uri)).ToArray()))
                    .DefaultFieldNameInferrer(fieldName =>
                        char.ToLowerInvariant(fieldName[0]) + fieldName[1..])
                    .DisableDirectStreaming()
                    .EnableDebugMode();
            return new ElasticClient(settings);
        });
    }
}