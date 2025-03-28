using AElf.OpenTelemetry;
using Aevatar.Domain.Grains;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Application.Grains;
using Aevatar.GAgents.AI.Options;
using Aevatar.Options;
using Microsoft.CodeAnalysis.Options;
using Aevatar.PermissionManagement;
using Serilog;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Silo;

[DependsOn(
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAutofacModule),
    typeof(OpenTelemetryModule),
    typeof(AevatarModule),
    typeof(AevatarPermissionManagementModule)
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
        context.Services.AddSignalR().AddOrleans();
        Configure<PermissionManagementOptions>(options =>
        {
            options.IsDynamicPermissionStoreEnabled = true;
        });
        context.Services.Configure<HostOptions>(context.Services.GetConfiguration().GetSection("Host"));
        context.Services.Configure<SystemLLMConfigOptions>(configuration);
    }
    
    
}