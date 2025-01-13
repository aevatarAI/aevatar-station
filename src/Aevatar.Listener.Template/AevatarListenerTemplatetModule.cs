
using Aevatar.Listener.Handler;
using Aevatar.Listener.SDK.Handler;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.Listener;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpAspNetCoreSerilogModule)
)]
public class AevatarListenerTemplatetModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarListenerTemplatetModule>(); });
        var services = context.Services;
        services.AddSingleton<IWebhookHandler, TelegramWebhookHandler>();
    }
}