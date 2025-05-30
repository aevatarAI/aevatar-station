using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Aevatar.AuthServer.Grants;

[DependsOn(
    typeof(AevatarAuthServerGrantsModule),
    typeof(AevatarTestBaseModule),
    typeof(AbpAutofacModule)
)]
public class AevatarAuthServerGrantsTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        
    }
} 