using AElf.OpenTelemetry;
using Aevatar.CQRS;
using Aevatar.Domain.Grains;
using Aevatar.Silo;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace Aevatar.Daipp.Silo;

[DependsOn(
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAutofacModule),
    typeof(OpenTelemetryModule),
    typeof(AbpEventBusModule),
    typeof(AevatarCQRSModule)
)]
public class SiloDaippModule : AbpModule, IDomainGrainsModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<SiloDaippModule>(); });
        context.Services.AddHostedService<AevatarHostedService>();
        var configuration = context.Services.GetConfiguration();
        //add dependencies here
        context.Services.AddSerilog(loggerConfiguration => {},
            true, writeToProviders: true);
        context.Services.AddHttpClient();
    }
}