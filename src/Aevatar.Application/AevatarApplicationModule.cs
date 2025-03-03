using System;
using System.Linq;
using Aevatar.Application.Grains;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.CQRS.Provider;
using Aevatar.Kubernetes;
using Aevatar.Kubernetes.Manager;
using Aevatar.Options;
using Aevatar.Schema;
using Aevatar.WebHook.Deploy;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.Mvc.Dapr;
using Volo.Abp.AutoMapper;
using Volo.Abp.Dapr;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Aevatar;

[DependsOn(
    typeof(AevatarDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(AevatarApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpDaprModule),
    typeof(AbpAspNetCoreMvcDaprModule),
    typeof(AIApplicationGrainsModule),
    typeof(AevatarCQRSModule),
    typeof(AevatarWebhookDeployModule),
    typeof(AevatarKubernetesModule)
)]
public class AevatarApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarApplicationModule>();
        });
        
        var configuration = context.Services.GetConfiguration();
        Configure<NameContestOptions>(configuration.GetSection("NameContest"));
        context.Services.AddSingleton<IGAgentFactory>(sp => new GAgentFactory(context.Services.GetRequiredService<IClusterClient>()));
        context.Services.AddSingleton<IGAgentManager>(sp => new GAgentManager(context.Services.GetRequiredService<IClusterClient>()));
        context.Services.AddSingleton<ISchemaProvider, SchemaProvider>();
        Configure<WebhookDeployOptions>(configuration.GetSection("WebhookDeploy"));
        Configure<AgentOptions>(configuration.GetSection("Agent"));
        context.Services.AddTransient<IHostDeployManager, KubernetesHostManager>();
        Configure<HostDeployOptions>(configuration.GetSection("HostDeploy"));
       
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var cqrsProvider = context.ServiceProvider.GetRequiredService<ICQRSProvider>();
        var hostId = context.GetConfiguration().GetValue<string>("Host:HostId");
        cqrsProvider.SetProjectName(hostId);
    }
}
