using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.PermissionManagement.MongoDB;

namespace Aevatar.PermissionManagement;

[DependsOn(
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpPermissionManagementMongoDbModule)
)]
public class AevatarPermissionManagementModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IPermissionInfoProvider, PermissionInfoProvider>();
    }
}