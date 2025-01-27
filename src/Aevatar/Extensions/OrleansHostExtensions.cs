using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Plugins.Extensions;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Aevatar.Extensions;

public static class OrleansHostExtensions
{
    public static ISiloBuilder UseAevatar(this ISiloBuilder builder)
    {
        var abpApplication = AbpApplicationFactory.Create<AevatarModule>();
        abpApplication.Initialize();

        return builder
            .UseAevatarPlugins()
            .ConfigureServices(services =>
            {
                foreach (var service in abpApplication.Services)
                {
                    services.Add(service);
                }
            });
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

    public static IClientBuilder UseAevatar(this IClientBuilder builder)
    {
        var abpApplication = AbpApplicationFactory.Create<AevatarModule>();
        abpApplication.Initialize();

        return builder
            .UseAevatarPlugins()
            .ConfigureServices(services =>
        {
            foreach (var service in abpApplication.Services)
            {
                services.Add(service);
            }
        });
    }
}