using System;
using System.Linq;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Webhook.Extensions;
using Aevatar.Webhook.SDK.Handler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Aevatar.Webhook;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpAspNetCoreSerilogModule)
)]
public class AevatarListenerHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHealthChecks();
        context.Services.AddSingleton<IGAgentFactory,GAgentFactory>();
        
        context.Services.AddHealthChecks();
    }
    
    public void ConfigureServices(IServiceCollection services)
    {
       
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {

    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var handlers = context.ServiceProvider.GetServices<IWebhookHandler>();
        app.UseRouting();
        app.UseHealthChecks("/health");
        var configuration = context.GetConfiguration();
        var webhookId = configuration["Webhook:WebhookId"];
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapWebhookHandlers(handlers,webhookId);
            endpoints.MapHealthChecks($"/{webhookId}/health");
        });
    }
       
    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {

    }
}