using System;
using System.Linq;
using Aevatar.Account;
using Aevatar.ApiRequests;
using Aevatar.Application.Grains;
using Aevatar.BlobStorings;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.Kubernetes;
using Aevatar.Kubernetes.Manager;
using Aevatar.Notification;
using Aevatar.Options;
using Aevatar.Plugins;
using Aevatar.Profiles;
using Aevatar.Schema;
using Aevatar.Service;
using Aevatar.WebHook.Deploy;
using Aevatar.Application.Contracts.DailyPush;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.Mvc.Dapr;
using Volo.Abp.AutoMapper;
using Volo.Abp.BlobStoring;
using Volo.Abp.Dapr;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.EventBus;
using Volo.Abp.VirtualFileSystem;
using Aevatar.Application.Service;

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
    typeof(AevatarKubernetesModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpEventBusModule),
    typeof(AevatarPluginsModule),
    typeof(AevatarModule),
    typeof(AbpBlobStoringModule)
)]
public class AevatarApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AevatarApplicationModule>();
        });
        
        Configure<AbpVirtualFileSystemOptions>(options =>
        {
            options.FileSets.AddEmbedded<AevatarApplicationModule>();
        });
        
        var configuration = context.Services.GetConfiguration();
        Configure<NameContestOptions>(configuration.GetSection("NameContest"));
        context.Services.AddSingleton<ISchemaProvider, SchemaProvider>();
        Configure<WebhookDeployOptions>(configuration.GetSection("WebhookDeploy"));
        Configure<AgentOptions>(configuration.GetSection("Agent"));
        context.Services.AddTransient<IHostDeployManager, KubernetesHostManager>();
        context.Services.AddSingleton<INotificationHandlerFactory, NotificationProcessorFactory>();
        Configure<HostDeployOptions>(configuration.GetSection("HostDeploy"));
        context.Services.Configure<HostOptions>(configuration.GetSection("Host"));
        context.Services.Configure<AppleAuthOption>(configuration.GetSection("AppleAuth"));
		context.Services.AddTransient<IProfileService, ProfileService>();
        context.Services.AddTransient<Aevatar.Profiles.IProfileService, Aevatar.Service.ProfileService>();

        Configure<AccountOptions>(configuration.GetSection("Account"));
        Configure<ApiRequestOptions>(configuration.GetSection("ApiRequest"));
        Configure<BlobStoringOptions>(configuration.GetSection("BlobStoring"));
        Configure<ThumbnailOptions>(configuration.GetSection("Thumbnail"));
        Configure<GoogleAnalyticsOptions>(configuration.GetSection("GoogleAnalytics"));
        Configure<FirebaseAnalyticsOptions>(configuration.GetSection("FirebaseAnalytics"));
        context.Services.AddSingleton<IIpLocationService, IpLocationService>();
        
        // Daily Push Service
        context.Services.AddTransient<IDailyPushService, DailyPushService>();

    }
}
