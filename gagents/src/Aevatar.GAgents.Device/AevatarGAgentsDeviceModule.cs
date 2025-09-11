using Aevatar.GAgents.Basic;
using Aevatar.GAgents.Common;
using Aevatar.GAgents.Device.Connections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Modularity;

namespace Aevatar.GAgents.Device;

/// <summary>
/// Aevatar设备GAgent模块
/// </summary>
[DependsOn(
    typeof(AevatarGAgentsCommonModule)
)]
public class AevatarGAgentsDeviceModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        
        // 注册虚拟设备连接服务
        services.AddTransient<VirtualDeviceConnection>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<VirtualDeviceConnection>>();
            return new VirtualDeviceConnection("default", "Default Device", "Generic", logger);
        });
        
        // 注册设备连接工厂服务（如果需要的话）
        services.AddSingleton<IDeviceConnectionFactory, DeviceConnectionFactory>();
        
        // 可以在这里注册其他设备相关的服务
        // 例如：设备发现服务、设备管理服务等
    }
}

/// <summary>
/// 设备连接工厂接口
/// </summary>
public interface IDeviceConnectionFactory
{
    /// <summary>
    /// 创建虚拟设备连接
    /// </summary>
    /// <param name="deviceId">设备ID</param>
    /// <param name="deviceName">设备名称</param>
    /// <param name="deviceType">设备类型</param>
    /// <returns>设备连接实例</returns>
    VirtualDeviceConnection CreateVirtualConnection(string deviceId, string deviceName, string deviceType);
}

/// <summary>
/// 设备连接工厂实现
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
        return new VirtualDeviceConnection(deviceId, deviceName, deviceType, logger);
    }
}
