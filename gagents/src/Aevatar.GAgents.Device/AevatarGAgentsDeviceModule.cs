using Aevatar.GAgents.Common;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Device;

/// <summary>
/// Aevatar Device GAgent Module
/// </summary>
[DependsOn(
    typeof(AevatarGAgentsCommonModule)
)]
public class AevatarGAgentsDeviceModule : AbpModule
{

}