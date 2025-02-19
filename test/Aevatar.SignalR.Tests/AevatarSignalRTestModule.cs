using Aevatar.TestBase;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace Aevatar.SignalR.Tests;

[DependsOn(
    typeof(AevatarTestBaseModule),
    typeof(AbpEventBusModule)
)]
public class AevatarSignalRTestModule : AbpModule
{
    
}