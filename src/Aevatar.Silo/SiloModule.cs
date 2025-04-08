using AElf.OpenTelemetry;
using Aevatar.Domain.Grains;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Application.Grains;
using Aevatar.GAgents.AI.Options;
using Aevatar.Options;
using Microsoft.CodeAnalysis.Options;
using Aevatar.PermissionManagement;
using Serilog;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using System.Linq;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Extensions;
using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Aevatar.Silo;

[DependsOn(
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAutofacModule),
    typeof(OpenTelemetryModule),
    typeof(AevatarModule),
    typeof(AevatarPermissionManagementModule),
    typeof(AbpAutoMapperModule)
)]
public class SiloModule : AIApplicationGrainsModule, IDomainGrainsModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<SiloModule>(validate: true);
        });
        
        var configuration = context.Services.GetConfiguration();
        
        ConfigureOrleans(context, configuration);
        
        context.Services.AddHostedService<AevatarHostedService>();
        context.Services.AddSerilog(loggerConfiguration => {},
            true, writeToProviders: true);
        context.Services.AddHttpClient();
        context.Services.AddSignalR().AddOrleans();
        Configure<PermissionManagementOptions>(options =>
        {
            options.IsDynamicPermissionStoreEnabled = true;
        });
        context.Services.Configure<HostOptions>(context.Services.GetConfiguration().GetSection("Host"));
        context.Services.Configure<SystemLLMConfigOptions>(configuration);
    }

    private void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = configuration["Orleans:ClusterId"];
            options.ServiceId = configuration["Orleans:ServiceId"];
        });
        
        context.Services.Configure<SiloMessagingOptions>(options =>
        {
            options.SystemResponseTimeout = TimeSpan.FromMinutes(30);
            options.ResponseTimeout = TimeSpan.FromMinutes(30);
            
            options.MaxRequestProcessingTime = TimeSpan.FromMinutes(10);
        });
        
        // 注释掉未找到的PerformanceTuningOptions类型配置
        // context.Services.Configure<PerformanceTuningOptions>(options =>
        // {
        //     options.DefaultConnectionLimit = 200;
        //     options.MinDotNetThreadPoolSize = Environment.ProcessorCount * 5;
        // });
        
        context.Services.Configure<GrainCollectionOptions>(options =>
        {
            options.CollectionAge = TimeSpan.FromMinutes(10);
            options.DeactivationTimeout = TimeSpan.FromMinutes(1);
        });
    }
}