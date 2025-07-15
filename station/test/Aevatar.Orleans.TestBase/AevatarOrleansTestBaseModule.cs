using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Plugin;
using Aevatar.CQRS;
using Aevatar.CQRS.Provider;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;


namespace Aevatar;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AevatarCQRSModule),
    typeof(AevatarModule)
)]
public class AevatarOrleansTestBaseModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAuditingOptions>(options =>
        {
            options.IsEnabled = false;
        });
        context.Services.AddSingleton<ClusterFixture>();
        context.Services.AddSingleton<IClusterClient>(sp => context.Services.GetRequiredService<ClusterFixture>().Cluster.Client);
        context.Services.AddSingleton<IGrainFactory>(sp => context.Services.GetRequiredService<ClusterFixture>().Cluster.GrainFactory);
        context.Services.AddSingleton<IGAgentFactory>(sp => new GAgentFactory(context.Services.GetRequiredService<ClusterFixture>().Cluster.Client));
        context.Services.AddSingleton<IGAgentManager>(sp => new GAgentManager(context.Services.GetRequiredService<ClusterFixture>().Cluster.Client, sp.GetRequiredService<IPluginGAgentManager>()));
        context.Services.AddSingleton<ICQRSProvider, CQRSProvider>();
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarApplicationModule>(); });

    }
}
