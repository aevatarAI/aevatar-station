using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Plugins.Extensions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Aevatar.Extensions;

public static class OrleansHostExtensions
{
    public static ISiloBuilder UseAevatar(this ISiloBuilder builder)
    {
        return builder.ConfigureServices(services =>
            {
                services.AddSingleton<IGAgentManager, GAgentManager>();
                services.AddSingleton<IGAgentFactory, GAgentFactory>();
                services.AddSingleton<IConfigureGrainTypeComponents, ConfigureAevatarGrainActivator>();
            })
            .UseAevatarPlugins();
    }

    public static ISiloBuilder UseAevatar<TAbpModule>(this ISiloBuilder builder) where TAbpModule : AbpModule
    {
        var abpApplication = AbpApplicationFactory.Create<TAbpModule>();
        abpApplication.Initialize();

        return builder
            .UseAevatar()
            .ConfigureServices(services =>
            {
                foreach (var service in abpApplication.Services)
                {
                    services.Add(service);
                }
            });
    }

    public static IHostBuilder UseAevatar(this IHostBuilder builder)
    {
        var abpApplication = AbpApplicationFactory.Create<AevatarModule>(options =>
        {
            options.UseAutofac();
        });
        abpApplication.Initialize();

        return builder
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .UseOrleans((context, siloBuilder) =>
            {
                siloBuilder
                    .UseLocalhostClustering()
                    .ConfigureServices(services =>
                    {
                        var autofacContainer = abpApplication.ServiceProvider.GetRequiredService<ILifetimeScope>();
                        services.AddSingleton(autofacContainer);
                        foreach (var service in abpApplication.Services)
                        {
                            services.Add(service);
                        }
                    });
            })
            .ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
            {
                containerBuilder.Populate(abpApplication.Services);
            });
    }
    
    public static IClientBuilder UseAevatar(this IClientBuilder builder)
    {
        return builder.ConfigureServices(services =>
        {
            services.AddSingleton<IGAgentManager, GAgentManager>();
            services.AddSingleton<IGAgentFactory, GAgentFactory>();
            services.AddSingleton<IConfigureGrainTypeComponents, ConfigureAevatarGrainActivator>();
        });
    }
}