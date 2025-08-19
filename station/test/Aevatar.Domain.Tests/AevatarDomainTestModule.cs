using Aevatar.MongoDB;
using Aevatar.Options;
using Volo.Abp.Modularity;

namespace Aevatar;

[DependsOn(
    typeof(AevatarDomainModule),
    typeof(AevatarTestBaseModule),
    typeof(AevatarMongoDbTestModule)
)]
public class AevatarDomainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<UsersOptions>(options => { options.AdminPassword = "ABC@123a"; });
    }
}
