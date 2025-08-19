using System;
using System.Linq;
using Aevatar.Account;
using Aevatar.ApiRequests;
using Aevatar.Application.Grains;
using Aevatar.Application.Service;
using Aevatar.Service;
using Aevatar.Projects;
using Aevatar.BlobStorings;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.Kubernetes;
using Aevatar.Kubernetes.Manager;
using Aevatar.Notification;
using Aevatar.Options;
using Aevatar.Plugins;
using Aevatar.Schema;
using Aevatar.WebHook.Deploy;
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
        Configure<AgentDefaultValuesOptions>(configuration.GetSection("AgentDefaults"));
        context.Services.AddTransient<IHostDeployManager, KubernetesHostManager>();
        context.Services.AddTransient<IHostCopyManager, KubernetesHostManager>();
        context.Services.AddSingleton<INotificationHandlerFactory, NotificationProcessorFactory>();
        Configure<HostDeployOptions>(configuration.GetSection("HostDeploy"));
        context.Services.Configure<HostOptions>(configuration.GetSection("Host"));
        
        Configure<AccountOptions>(configuration.GetSection("Account"));
        Configure<ApiRequestOptions>(configuration.GetSection("ApiRequest"));
        Configure<BlobStoringOptions>(configuration.GetSection("BlobStoring"));
        Configure<DebugModeOptions>(configuration.GetSection("DebugMode"));
        
        // 配置 AI 服务提示词选项
        Configure<AIServicePromptOptions>(configuration.GetSection("AIServicePrompt"));
        
        // 配置工作流编排服务
        ConfigureWorkflowOrchestrationServices(context);
        
        // 配置域名生成服务
        context.Services.AddTransient<ISimpleDomainGenerationService, SimpleDomainGenerationService>();
    }
    
    /// <summary>
    /// Configure workflow orchestration related services
    /// </summary>
    private void ConfigureWorkflowOrchestrationServices(ServiceConfigurationContext context)
    {
        // Unified workflow orchestration service - includes prompt building and JSON validation functionality
        context.Services.AddTransient<IWorkflowOrchestrationService, WorkflowOrchestrationService>();
        
        // Text completion service  
        context.Services.AddTransient<ITextCompletionService, TextCompletionService>();
    }
}
