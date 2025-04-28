using Aevatar.Application.Grains;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace Aevatar;

[DependsOn(
    typeof(AIApplicationGrainsModule),
    typeof(AbpEventBusModule),
    typeof(AevatarOrleansTestBaseModule)
)]
public class AevatarApplicationGrainsTestModule : AbpModule
{
}