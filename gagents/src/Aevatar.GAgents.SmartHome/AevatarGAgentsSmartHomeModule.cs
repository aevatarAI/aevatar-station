using Aevatar.GAgents.Device;
using Aevatar.GAgents.Device.Http;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.SmartHome;

/// <summary>
/// Smart Home GAgents Module
/// </summary>
[DependsOn(
    typeof(AevatarGAgentsDeviceModule),
    typeof(AevatarGAgentsHttpDeviceModule)
)]
public class AevatarGAgentsSmartHomeModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Configure smart home specific services if needed
    }
}
