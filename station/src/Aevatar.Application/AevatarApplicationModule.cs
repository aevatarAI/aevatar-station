using System;
using System.Linq;
using Aevatar.Account;
using Aevatar.ApiRequests;
using Aevatar.Application.Grains;
using Aevatar.Application.Service;
using Aevatar.Service;
using Aevatar.BlobStorings;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.CQRS;
using Aevatar.GAgents.Core;
using Aevatar.Kubernetes;
using Aevatar.Kubernetes.Manager;
using Aevatar.LocalDevelopment;
using Aevatar.Notification;
using Aevatar.Options;
using Aevatar.Plugins;
using Aevatar.Schema;
using Aevatar.Provider;
using Aevatar.WebHook.Deploy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        
        // Register IGAgentFactory for BusinessAgentBase (required for workflow agents)
        context.Services.AddSingleton<IGAgentFactory<IBusinessAgentBase>, GAgentFactory<IBusinessAgentBase>>();
        
        // Register WorkflowViewService explicitly
        context.Services.AddSingleton<IWorkflowViewService, WorkflowViewServicePlus>();
        
        // 配置Schema处理器
        ConfigureSchemaProcessors(context);
        Configure<WebhookDeployOptions>(configuration.GetSection("WebhookDeploy"));
        Configure<AgentOptions>(configuration.GetSection("Agent"));
        Configure<AgentDefaultValuesOptions>(configuration.GetSection("AgentDefaults"));
        Configure<WorkflowAgentFilterOptions>(configuration.GetSection("WorkflowAgentFilter"));
        context.Services.AddTransient<IHostDeployManager, KubernetesHostManager>();
        context.Services.AddTransient<IHostCopyManager, KubernetesHostManager>();
        context.Services.AddSingleton<INotificationHandlerFactory, NotificationProcessorFactory>();
        Configure<HostDeployOptions>(configuration.GetSection("HostDeploy"));
        context.Services.Configure<Aevatar.Options.HostOptions>(configuration.GetSection("Host"));
        
        Configure<AccountOptions>(configuration.GetSection("Account"));
        Configure<ApiRequestOptions>(configuration.GetSection("ApiRequest"));
        Configure<BlobStoringOptions>(configuration.GetSection("BlobStoring"));
        Configure<DebugModeOptions>(configuration.GetSection("DebugMode"));
        
        // 配置 AI 服务提示词选项
        Configure<AIServicePromptOptions>(configuration.GetSection("AIServicePrompt"));

        // Configure local development services
        ConfigureLocalDevelopmentServices(context);
        
        // 配置工作流编排服务
        ConfigureWorkflowOrchestrationServices(context);
    }
    
    /// <summary>
    /// Configure local development specific services
    /// </summary>
    private void ConfigureLocalDevelopmentServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        
        if (hostingEnvironment.IsDevelopment())
        {
            // Override services with local development implementations
            context.Services.AddTransient<IAevatarAccountEmailer, DevLocalAevatarAccountEmailer>();
            context.Services.AddTransient<IHostDeployManager, DefaultHostDeployManager>();
            context.Services.AddTransient<IDeveloperService, LocalDevelopmentDeveloperService>();
        }
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
        
        // Trace management service
        context.Services.AddTransient<ITraceManagementService, TraceManagementService>();
        
        // Register ITraceManager dependency
        context.Services.AddSingleton<Aevatar.Core.Interception.Services.ITraceManager, Aevatar.Core.Interception.Services.TraceManager>();
    }
    
    /// <summary>
    /// Configure dynamic configuration providers and schema processors for dropdown functionality
    /// </summary>
    private void ConfigureSchemaProcessors(ServiceConfigurationContext context)
    {
        // 注册动态配置提供者
        context.Services.AddTransient<IDynamicConfigurationProvider, SystemLLMConfigurationProvider>();
        // context.Services.AddTransient<IDynamicConfigurationProvider, OtherConfigurationProvider>();
        
        // 注册下拉框Schema处理器
        context.Services.AddTransient<IDropDownSchemaProcess, SystemLLMDropDownSchemaProcess>();
        // context.Services.AddTransient<IDropDownSchemaProcess, OtherDropDownSchemaProcess>();
        
        // 注册DynamicDropDownProcessor（协调器）
        context.Services.AddTransient<DynamicDropDownProcessor>();
        
        // 注册DefaultValuesProcessor
        context.Services.AddTransient<DefaultValuesProcessor>();
    }
}
