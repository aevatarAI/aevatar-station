using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.Core.Extensions;
using Aevatar.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using Volo.Abp.MongoDB;
using Volo.Abp.Uow;

namespace Aevatar;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpMongoDbModule),
    typeof(AbpDddDomainModule),
    typeof(AbpUnitOfWorkModule),
    typeof(AevatarPluginsModule)
)]
public class AevatarModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddConventionalRegistrar(new AevatarDefaultConventionalRegistrar());
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarModule>(); });
        Configure<AevatarOptions>(context.Services.GetConfiguration().GetSection("Aevatar"));
        
        // Register Core services including state publisher and event observer manager
        context.Services.AddCoreServices();
        
        context.Services.AddSingleton<IGAgentManager, GAgentManager>();
        context.Services.AddTransient<IGAgentFactory, GAgentFactory>();
        context.Services.AddTransient<IPluginGAgentManager, PluginGAgentManager>();
        context.Services.AddSingleton<IConfigureGrainTypeComponents, ConfigureAevatarGrainActivator>();
        context.Services.AddSingleton<IStateDispatcher, StateDispatcher>();
        context.Services.AddTransient(typeof(IArtifactGAgent<,,>), typeof(ArtifactGAgent<,,>));
    }
}