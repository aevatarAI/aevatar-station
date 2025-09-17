using System.Collections.Generic;
using Aevatar.GAgents.Device.Abstractions;
using Aevatar.GAgents.Device.State;
using Aevatar.GAgents.Device.Virtual;
using Aevatar.GAgents.TestBase;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.GAgents.Device.Test;

public abstract class AevatarDeviceTestBase : AevatarGAgentTestBase<AevatarDeviceTestModule>
{
    /// <summary>
    /// Create test device connection configuration for smart light
    /// </summary>
    protected static DeviceConnectionConfig CreateSmartLightConfig(string deviceId = "test-smart-light")
    {
        return new DeviceConnectionConfig
        {
            DeviceId = deviceId,
            DeviceName = "Test Smart Light",
            DeviceType = "SmartLight",
            ConnectionString = "virtual://test",
            ExtendedProperties = new Dictionary<string, string>
            {
                ["Power"] = "false",
                ["Brightness"] = "50",
                ["Color"] = "#FFFFFF"
            }
        };
    }
    
    /// <summary>
    /// Create test device connection configuration for temperature sensor
    /// </summary>
    protected static DeviceConnectionConfig CreateTemperatureSensorConfig(string deviceId = "test-temp-sensor")
    {
        return new DeviceConnectionConfig
        {
            DeviceId = deviceId,
            DeviceName = "Test Temperature Sensor",
            DeviceType = "TemperatureSensor",
            ConnectionString = "virtual://test",
            ExtendedProperties = new Dictionary<string, string>
            {
                ["Temperature"] = "22.5",
                ["Humidity"] = "45.0"
            }
        };
    }
    
    /// <summary>
    /// Get device connection factory service
    /// </summary>
    protected IDeviceConnectionFactory GetDeviceConnectionFactory()
    {
        return GetRequiredService<IDeviceConnectionFactory>();
    }
}
