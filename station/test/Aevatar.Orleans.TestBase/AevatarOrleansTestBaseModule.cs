using Aevatar.CQRS;
using Aevatar.CQRS.Provider;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Metadata;
using Orleans.Runtime;
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
    typeof(AevatarCQRSModule)

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
        
        // Register GrainTypeResolver for AgentService dependency injection
        context.Services.AddSingleton<GrainTypeResolver>(sp => sp.GetRequiredService<IClusterClient>().ServiceProvider.GetRequiredService<GrainTypeResolver>());
        context.Services.AddSingleton<IGrainTypeProvider, AttributeGrainTypeProvider>();
        
        context.Services.AddSingleton<ICQRSProvider, CQRSProvider>();
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarApplicationModule>(); });

    }
}
