using Aevatar.Webhook.SDK.Handler;
using Aevatar.Webhook.Template.Handler;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.Webhook.Template;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpAspNetCoreSerilogModule)
)]
public class AevatarWebHookTemplateModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AevatarWebHookTemplateModule>(); });
        var services = context.Services;
        services.AddSingleton<IWebhookHandler, TelegramWebhookHandler>();
    }
}