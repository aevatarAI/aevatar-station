using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Device.Virtual;

[DependsOn(
    typeof(AevatarGAgentsDeviceModule)
)]
public class AevatarGAgentsVirtualDeviceModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        
        // Register device connection factory service (if needed)
        services.AddSingleton<IDeviceConnectionFactory, DeviceConnectionFactory>();
        
        // Can register other device-related services here
        // For example: device discovery service, device management service, etc.
    }
}

/// <summary>
/// Device connection factory interface
/// </summary>
public interface IDeviceConnectionFactory
{
    /// <summary>
    /// Create virtual device connection
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <param name="deviceName">Device name</param>
    /// <param name="deviceType">Device type</param>
    /// <returns>Device connection instance</returns>
    VirtualDeviceConnection CreateVirtualConnection(string deviceId, string deviceName, string deviceType);
}

/// <summary>
/// Device connection factory implementation
/// </summary>
public class DeviceConnectionFactory : IDeviceConnectionFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DeviceConnectionFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public VirtualDeviceConnection CreateVirtualConnection(string deviceId, string deviceName, string deviceType)
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<VirtualDeviceConnection>>();
        return new VirtualDeviceConnection(deviceId, deviceName, deviceType, logger, null);
    }
}