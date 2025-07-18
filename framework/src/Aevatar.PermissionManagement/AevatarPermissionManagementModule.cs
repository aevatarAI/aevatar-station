using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.PermissionManagement.MongoDB;
using Volo.Abp.Threading;

namespace Aevatar.PermissionManagement;

[DependsOn(
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpPermissionManagementMongoDbModule),
    typeof(AbpThreadingModule)  // Ensure Threading module is loaded
)]
public class AevatarPermissionManagementModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        // Ensure ICancellationTokenProvider is registered early
        context.Services.TryAddSingleton<ICancellationTokenProvider, NullCancellationTokenProvider>();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<PermissionManagementOptions>(options =>
        {
            options.IsDynamicPermissionStoreEnabled = true;
        });
    }
}