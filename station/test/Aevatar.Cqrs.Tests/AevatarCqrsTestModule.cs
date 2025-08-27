using Aevatar.CQRS;
using Aevatar.CQRS.Handler;
using Aevatar.Mock;
using Aevatar.Options;
using Aevatar.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using ChatConfigOptions = Aevatar.Options.ChatConfigOptions;

namespace Aevatar.Cqrs.Tests;

[DependsOn(
    typeof(AbpEventBusModule),
    typeof(AevatarOrleansTestBaseModule),
    typeof(AevatarCQRSModule)
)]
public class AevatarCqrsTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarCqrsTestModule>(); });
        var configuration = context.Services.GetConfiguration();
        Configure<ChatConfigOptions>(configuration.GetSection("Chat"));
        //Configure<ProjectorBatchOptions>(options => { options.BatchSize = 1; });
        context.Services.AddMemoryCache();
        context.Services.AddSingleton<IIndexingService, MockElasticIndexingService>();
        context.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetStateQueryHandler).Assembly)
        );
        context.Services.AddSingleton<ICqrsService, CqrsService>();
    }
}