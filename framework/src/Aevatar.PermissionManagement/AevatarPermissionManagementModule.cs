using Aevatar.PermissionManagement.Threading;
using Microsoft.Extensions.DependencyInjection;
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
        // Critical fix: Register ICancellationTokenProvider early to prevent NullReferenceException
        context.Services.TryAddSingleton<ICancellationTokenProvider, NullCancellationTokenProvider>();
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<PermissionManagementOptions>(options =>
        {
            options.IsDynamicPermissionStoreEnabled = true;
        });
        // replace the StaticPermissionSaver with OrleansStaticPermissionSaver
        context.Services.Replace(ServiceDescriptor.Transient<IStaticPermissionSaver, OrleansStaticPermissionSaver>());
    }
    public override void PostConfigureServices(ServiceConfigurationContext context)
    {
        // Critical fix: Register ICancellationTokenProvider early to prevent NullReferenceException
        context.Services.TryAddSingleton<ICancellationTokenProvider, NullCancellationTokenProvider>();
    }
}