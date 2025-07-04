using AElf.OpenTelemetry;
using Aevatar.Domain.Grains;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Application.Grains;
using Aevatar.GAgents.AI.Options;
using Aevatar.Options;
using Aevatar.Silo.Grains.Activation;
using Aevatar.Silo.IdGeneration;
using Aevatar.Silo.TypeDiscovery;
using Aevatar.PermissionManagement;
using Aevatar.Plugins;
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
    typeof(AbpBlobStoringAwsModule)
)]
public class SiloModule : AIApplicationGrainsModule, IDomainGrainsModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<SiloModule>(); });
        context.Services.AddHostedService<AevatarHostedService>();
        var configuration = context.Services.GetConfiguration();
        //add dependencies here
        context.Services.AddSerilog(loggerConfiguration => {},
            true, writeToProviders: true);
        context.Services.AddHttpClient();
        context.Services.AddSignalR().AddOrleans();
        Configure<PermissionManagementOptions>(options =>
        {
            options.IsDynamicPermissionStoreEnabled = true;
        });
        
        context.Services.AddTransient<IStateTypeDiscoverer, StateTypeDiscoverer>();
        context.Services.AddTransient<IDeterministicIdGenerator, MD5DeterministicIdGenerator>();
        context.Services.AddTransient<IProjectionGrainActivator, ProjectionGrainActivator>();
        
        context.Services.Configure<HostOptions>(context.Services.GetConfiguration().GetSection("Host"));
        context.Services.Configure<SystemLLMConfigOptions>(configuration);
        
        // Configure<AbpBlobStoringOptions>(options =>
        // {
        //     options.Containers.ConfigureDefault(container =>
        //     {
        //         var configSection = configuration.GetSection("AwsS3");
        //         container.UseAws(o =>
        //         {
        //             o.AccessKeyId = configSection.GetValue<string>("AccessKeyId");
        //             o.SecretAccessKey = configSection.GetValue<string>("SecretAccessKey");
        //             o.Region = configSection.GetValue<string>("Region");
        //             o.ContainerName = configSection.GetValue<string>("ContainerName");
        //         }); 
        //     });
        // });
    }
}