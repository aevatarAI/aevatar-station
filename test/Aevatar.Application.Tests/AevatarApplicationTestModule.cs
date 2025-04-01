using System;
using Aevatar.CQRS.Handler;
using Aevatar.Kubernetes.Manager;
using Aevatar.Options;
using Aevatar.WebHook.Deploy;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace Aevatar;

[DependsOn(
    typeof(AevatarApplicationModule),
    typeof(AbpEventBusModule),
    typeof(AevatarOrleansTestBaseModule),
    typeof(AevatarDomainTestModule)
)]
public class AevatarApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarApplicationModule>(); });
        var configuration = context.Services.GetConfiguration();
        Configure<ChatConfigOptions>(configuration.GetSection("Chat"));

        context.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetStateQueryHandler).Assembly)
        );
        context.Services.AddTransient<IHostDeployManager, DefaultHostDeployManager>();
    }
}