using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule)
)]
public class AevatarModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarModule>(); });
        context.Services.AddSingleton<IGAgentManager, GAgentManager>();
        context.Services.AddSingleton<IGAgentFactory, GAgentFactory>();
    }
}