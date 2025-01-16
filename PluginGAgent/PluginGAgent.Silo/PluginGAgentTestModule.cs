using Microsoft.Extensions.DependencyInjection;
using PluginGAgent.Silo;
using Serilog;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
namespace AISmart.Silo;

[DependsOn(
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule)
)]
public class PluginGAgentTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<PluginGAgentTestModule>(); });
        context.Services.AddHostedService<PluginGAgentTestHostedService>();
        var configuration = context.Services.GetConfiguration();
        context.Services.AddSerilog(loggerConfiguration => {},
            true, writeToProviders: true);
        context.Services.AddHttpClient();
    }
}