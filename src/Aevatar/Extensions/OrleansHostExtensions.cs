using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Plugins.Extensions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Volo.Abp;

namespace Aevatar.Extensions;

public static class OrleansHostExtensions
{
    public static ISiloBuilder UseAevatar(this ISiloBuilder builder)
    {
        var abpApplication = AbpApplicationFactory.Create<AevatarModule>(options =>
        {
            options.UseAutofac();
        });
        abpApplication.Initialize();

        return builder.ConfigureServices(services =>
            {
                services.AddSingleton<IGAgentManager, GAgentManager>();
                services.AddSingleton<IGAgentFactory, GAgentFactory>();
                services.AddSingleton<IConfigureGrainTypeComponents, ConfigureAevatarGrainActivator>();
                var autofacContainer = abpApplication.ServiceProvider.GetRequiredService<ILifetimeScope>();
                services.AddSingleton(autofacContainer);
                foreach (var service in abpApplication.Services)
                {
                    services.Add(service);
                }
            })
            .UseAevatarPlugins();
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