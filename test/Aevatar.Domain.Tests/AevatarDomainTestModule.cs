using Volo.Abp.Modularity;

namespace Aevatar;

[DependsOn(
    typeof(AevatarDomainModule),
    typeof(AevatarTestBaseModule)
)]
public class AevatarDomainTestModule : AbpModule
{

}
