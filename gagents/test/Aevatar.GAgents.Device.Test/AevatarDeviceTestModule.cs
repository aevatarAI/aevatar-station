using Aevatar.GAgents.Device;
using Aevatar.GAgents.TestBase;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Device.Test;

[DependsOn(
    typeof(AevatarGAgentTestBaseModule),
    typeof(AevatarGAgentsDeviceModule)
)]
public class AevatarDeviceTestModule : AbpModule;
