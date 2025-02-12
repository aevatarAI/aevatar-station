using AElf.OpenTelemetry;
using Aevatar.AI.Options;
using Aevatar.Domain.Grains;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Application.Grains;
using Serilog;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Aevatar.AI.Extensions;
using Aevatar.AI.Options;

namespace Aevatar.Silo;

[DependsOn(
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAutofacModule),
    typeof(OpenTelemetryModule)
)]
public class SiloModule : AIApplicationGrainsModule, IDomainGrainsModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<SiloModule>(); });
        context.Services.AddHostedService<AevatarHostedService>();
        var configuration = context.Services.GetConfiguration();
        //add dependencies here
        context.Services.AddSerilog(loggerConfiguration => {},
            true, writeToProviders: true);
        context.Services.AddHttpClient();
        
        Configure<AzureOpenAIConfig>(configuration.GetSection("AIServices:AzureOpenAI"));
        context.Services.AddSemanticKernel()
            .AddAzureOpenAI();
    }
}