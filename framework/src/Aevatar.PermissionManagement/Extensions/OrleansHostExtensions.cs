using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Hosting;
using Volo.Abp;

namespace Aevatar.PermissionManagement.Extensions;

public static class OrleansHostExtensions
{
    public static ISiloBuilder UseAevatarPermissionManagement(this ISiloBuilder builder)
    {
        var abpApplication = AbpApplicationFactory.Create<AevatarPermissionManagementModule>();
        abpApplication.Initialize();
        return builder
            .AddIncomingGrainCallFilter<PermissionCheckFilter>()
            .ConfigureServices(services =>
            {
                services.TryAdd(abpApplication.Services);
            });
    }
}