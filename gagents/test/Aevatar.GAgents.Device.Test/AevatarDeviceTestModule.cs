using Aevatar.GAgents.Device;
using Aevatar.GAgents.Device.Virtual;
using Aevatar.GAgents.TestBase;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Device.Test;

[DependsOn(
    typeof(AevatarGAgentTestBaseModule),
    typeof(AevatarGAgentsVirtualDeviceModule)
)]
public class AevatarDeviceTestModule : AbpModule;
