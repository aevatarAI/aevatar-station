using System.Reflection;
using Aevatar.Plugins;
using Aevatar.Plugins.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Metadata;
using Orleans.Serialization;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace Aevatar.Extensions;

public static class OrleansHostExtensions
{
    public static ISiloBuilder UseAevatar(this ISiloBuilder builder)
    {
        var abpApplication = AbpApplicationFactory.Create<AevatarModule>();
        abpApplication.Initialize();

        return builder
            .ConfigureServices(services =>
            {
                AsyncHelper.RunSync(() => LoadPluginsAsync(services, abpApplication));

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

    private static async Task LoadPluginsAsync(IServiceCollection services, IAbpApplicationWithInternalServiceProvider application)
    {
        var unitOfWorkManager = application.Services.GetRequiredService<IUnitOfWorkManager>();
        using var uow = unitOfWorkManager.Begin(requiresNew: true);
        var configuration = application.Services.GetConfiguration();
        var pluginConfig = configuration.GetSection("Plugins");
        var tenantId = pluginConfig["TenantId"];
        if (tenantId.IsNullOrEmpty()) return;
        var tenantPluginCodeRepository = application.Services.GetRequiredService<ITenantPluginCodeRepository>();
        var tenantIdGuid = Guid.Parse(tenantId);
        var pluginCodeGAgentPrimaryKeys =
            await tenantPluginCodeRepository.GetGAgentPrimaryKeysByTenantIdAsync(tenantIdGuid);
        if (pluginCodeGAgentPrimaryKeys == null) return;
        var pluginCodeStorageRepository = application.Services.GetRequiredService<IPluginCodeStorageRepository>();
        var pluginCodes =
            await pluginCodeStorageRepository.GetPluginCodesByGAgentPrimaryKeys(pluginCodeGAgentPrimaryKeys);
        var assemblies = pluginCodes.Select(Assembly.Load).DistinctBy(assembly => assembly.FullName);
        await uow.CompleteAsync();
        services.AddSerializer(options =>
        {
            foreach (var assembly in assemblies)
            {
                options.AddAssembly(assembly);
            }
        });
    }

    public static IClientBuilder UseAevatar(this IClientBuilder builder)
    {
        var abpApplication = AbpApplicationFactory.Create<AevatarModule>();
        abpApplication.Initialize();

        return builder
            .ConfigureServices(services =>
        {
            foreach (var service in abpApplication.Services)
            {
                services.Add(service);
            }
        });
    }
}