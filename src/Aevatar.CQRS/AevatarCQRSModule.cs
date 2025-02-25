using System;
using System.Linq;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS.Handler;
using Aevatar.CQRS.Provider;
using Elasticsearch.Net;
using MediatR;
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

            context.Services.AddMediatR(typeof(SaveStateCommandHandler).Assembly);
            context.Services.AddMediatR(typeof(GetStateQueryHandler).Assembly);
            context.Services.AddMediatR(typeof(SendEventCommandHandler).Assembly);
            context.Services.AddMediatR(typeof(SaveGEventCommandHandler).Assembly);
            context.Services.AddMediatR(typeof(GetGEventQueryHandler).Assembly);
            context.Services.AddMediatR(typeof(GetUserInstanceAgentsHandler).Assembly);
            context.Services.AddSingleton<IIndexingService, ElasticIndexingService>();
            context.Services.AddSingleton<IEventDispatcher, CQRSProvider>();
            context.Services.AddSingleton<ICQRSProvider, CQRSProvider>();
            context.Services.AddTransient<SaveStateCommandHandler>();
            context.Services.AddTransient<GetStateQueryHandler>();
            context.Services.AddTransient<SendEventCommandHandler>();
            context.Services.AddTransient<SaveGEventCommandHandler>();
            context.Services.AddTransient<GetUserInstanceAgentsHandler>();
            context.Services.AddTransient<GetGEventQueryHandler>();
            var configuration = context.Services.GetConfiguration();
            ConfigureElasticsearch(context, configuration);

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

               var settings = new ConnectionSettings(new StaticConnectionPool(uris.Select(uri => new Uri(uri)).ToArray())).DefaultFieldNameInferrer(fieldName => 
                   char.ToLowerInvariant(fieldName[0]) + fieldName[1..]);

               return new ElasticClient(settings);
           });
    
       } 
}