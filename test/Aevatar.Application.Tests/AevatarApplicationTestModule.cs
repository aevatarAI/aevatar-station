using System;
using Aevatar.CQRS.Handler;
using Aevatar.Kubernetes.Manager;
using Aevatar.Options;
using Aevatar.WebHook.Deploy;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Volo.Abp.Auditing;
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
        context.Services.AddSingleton<IElasticClient>(provider =>
        {
            var settings = new ConnectionSettings(new Uri("http://127.0.0.1:9200"))
                .DefaultIndex("cqrs").DefaultFieldNameInferrer(fieldName =>
                    char.ToLowerInvariant(fieldName[0]) + fieldName[1..]);
            return new ElasticClient(settings);
        });
        context.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetStateQueryHandler).Assembly)
        );
        context.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetGEventQueryHandler).Assembly)
        );
        context.Services.AddTransient<IHostDeployManager, DefaultHostDeployManager>();
    }
}