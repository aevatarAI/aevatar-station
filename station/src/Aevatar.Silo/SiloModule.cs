using AElf.OpenTelemetry;
using Aevatar.Domain.Grains;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Application.Grains;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.Executor;
using Aevatar.GAgents.MCP;
using Aevatar.GAgents.MCP.Services;
using Aevatar.GAgents.PsiOmni.Interfaces;
using Aevatar.GAgents.PsiOmni.Plugins;
using Aevatar.GAgents.PsiOmni.Plugins.Services;
using Aevatar.GAgents.SemanticKernel.Extensions;
using Aevatar.Options;
using Aevatar.Silo.Grains.Activation;
using Aevatar.Silo.IdGeneration;
using Aevatar.Silo.TypeDiscovery;
using Aevatar.PermissionManagement;
using Aevatar.Plugins;
using Aevatar.Silo.Extensions;
using Microsoft.Extensions.Configuration;
using Serilog;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.Aws;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;

namespace Aevatar.Silo;

[DependsOn(
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpAutofacModule),
    typeof(OpenTelemetryModule),
    typeof(AevatarModule),
    typeof(AevatarPluginsModule),
    typeof(AevatarPermissionManagementModule),
    typeof(AbpBlobStoringAwsModule),
    typeof(AevatarGAgentsMCPModule)
)]
public class SiloModule : AIApplicationGrainsModule, IDomainGrainsModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<SiloModule>(); });
        context.Services.AddHostedService<AevatarHostedService>();
        context.Services.AddHostedService<MCPWhitelistInitializationService>();
        var configuration = context.Services.GetConfiguration();
        //add dependencies here
        context.Services.AddSerilog(loggerConfiguration => { },
            true, writeToProviders: true);
        context.Services.AddHttpClient();
        context.Services.AddSignalR().AddOrleans();
        Configure<PermissionManagementOptions>(options => { options.IsDynamicPermissionStoreEnabled = true; });

        context.Services.AddTransient<IStateTypeDiscoverer, StateTypeDiscoverer>();
        context.Services.AddTransient<IDeterministicIdGenerator, MD5DeterministicIdGenerator>();
        context.Services.AddTransient<IProjectionGrainActivator, ProjectionGrainActivator>();

        context.Services.Configure<HostOptions>(context.Services.GetConfiguration().GetSection("Host"));
        context.Services.Configure<SystemLLMConfigOptions>(configuration);

        Configure<AbpBlobStoringOptions>(options =>
        {
            options.Containers.ConfigureDefault(container =>
            {
                var configSection = configuration.GetSection("AwsS3");
                container.UseAws(o =>
                {
                    o.AccessKeyId = configSection.GetValue<string>("AccessKeyId", "None");
                    o.SecretAccessKey = configSection.GetValue<string>("SecretAccessKey", "None");
                    o.Region = configSection.GetValue<string>("Region", "None");
                    o.ContainerName = configSection.GetValue<string>("ContainerName", "None");
                });
            });
        });
        
        // Configure health check options
        context.Services.Configure<HealthCheckOptions>(configuration.GetSection("HealthCheck"));

        // Add Orleans health checks
        context.Services.AddOrleansHealthChecks();

        context.Services.AddTransient<IGAgentExecutor, GAgentExecutor>();
        context.Services.AddTransient<IGAgentService, GAgentService>();

        context.Services.AddSemanticKernel();
        context.Services.AddSingleton<IKernelFactory, KernelFactory>();
        context.Services.AddSingleton<IKernelFunctionRegistry, KernelFunctionRegistry>();

        // Register web search services
        context.Services.AddHttpClient<WebContentFetcher>();
        context.Services.AddSingleton<IWebContentFetcher, WebContentFetcher>();

        // Register all search engines
        context.Services
            .AddSingleton<ISearchEngine, GoogleSearchEngine>(); // GoogleSearchEngine now uses built-in GoogleTextSearch
        context.Services.AddHttpClient<DuckDuckGoSearchEngine>();
        context.Services.AddHttpClient<BingSearchEngine>();
        context.Services.AddSingleton<ISearchEngine, DuckDuckGoSearchEngine>();
        context.Services.AddSingleton<ISearchEngine, BingSearchEngine>();

        // Register main web search service
        context.Services.AddSingleton<IWebSearchService, WebSearchService>();
    }
}