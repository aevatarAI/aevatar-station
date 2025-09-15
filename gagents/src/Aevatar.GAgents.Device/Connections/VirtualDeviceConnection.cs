using System.ComponentModel;
using Aevatar.GAgents.Device.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Device.Connections;

/// <summary>
/// Virtual device connection implementation - for development, testing and demonstration
/// </summary>
public class VirtualDeviceConnection : IDeviceConnection
{
    private readonly ILogger<VirtualDeviceConnection> _logger;
    private readonly Dictionary<string, DeviceProperty> _properties = new();
    private readonly Dictionary<string, DeviceAction> _supportedActions = new();
    private DeviceConnectionStatus _status = DeviceConnectionStatus.Disconnected;
    private readonly Random _random = new();
    private Timer? _simulationTimer;
    private bool _disposed = false;

    public string DeviceId { get; private set; }
    public string DeviceName { get; private set; }
    public string DeviceType { get; private set; }
    public DeviceConnectionStatus Status => _status;

    public IReadOnlyDictionary<string, DeviceProperty> Properties => _properties;
    public IReadOnlyDictionary<string, DeviceAction> SupportedActions => _supportedActions;

    public event EventHandler<DevicePropertyChangedEventArgs>? PropertyChanged;
    public event EventHandler<DeviceConnectionStatusChangedEventArgs>? StatusChanged;

    public VirtualDeviceConnection(
        string deviceId,
        string deviceName,
        string deviceType,
        ILogger<VirtualDeviceConnection> logger,
        Dictionary<string, string>? extendedProperties = null)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        DeviceType = deviceType;
        _logger = logger;

        InitializeVirtualDevice();
        
        // Apply extended properties if provided
        if (extendedProperties != null)
        {
            ApplyExtendedProperties(extendedProperties);
        }
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Connecting to virtual device: {DeviceId} ({DeviceName})", DeviceId, DeviceName);

        ChangeStatus(DeviceConnectionStatus.Connecting, "Connecting");

        // Simulate connection delay
        await Task.Delay(1000, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            ChangeStatus(DeviceConnectionStatus.Disconnected, "Connection cancelled");
            return false;
        }

        // Simulate connection success rate (95%)
        if (_random.NextDouble() < 0.95)
        {
            ChangeStatus(DeviceConnectionStatus.Connected, "Connection successful");
            StartSimulation();
            _logger.LogInformation("Virtual device connected successfully: {DeviceId}", DeviceId);
            return true;
        }
        else
        {
            ChangeStatus(DeviceConnectionStatus.Error, "Connection failed");
            _logger.LogWarning("Virtual device connection failed: {DeviceId}", DeviceId);
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disconnecting virtual device: {DeviceId}", DeviceId);

        StopSimulation();
        ChangeStatus(DeviceConnectionStatus.Disconnected, "Manually disconnected");

        // Simulate disconnect delay
        await Task.Delay(500, cancellationToken);

        _logger.LogInformation("Virtual device disconnected: {DeviceId}", DeviceId);
    }

    public Task<object?> ReadPropertyAsync(string propertyName, CancellationToken cancellationToken = default)
    {
        if (_status != DeviceConnectionStatus.Connected)
            throw new InvalidOperationException("Device not connected");

        if (!_properties.TryGetValue(propertyName, out var property))
            throw new ArgumentException($"Property '{propertyName}' does not exist", nameof(propertyName));

        if (!property.IsReadable)
            throw new InvalidOperationException($"Property '{propertyName}' is not readable");

        _logger.LogDebug("Reading virtual device property: {PropertyName} = {Value}", propertyName, property.Value);

        return Task.FromResult(property.Value);
    }

    public Task<bool> WritePropertyAsync(string propertyName, object value,
        CancellationToken cancellationToken = default)
    {
        if (_status != DeviceConnectionStatus.Connected)
            throw new InvalidOperationException("Device not connected");

        if (!_properties.TryGetValue(propertyName, out var property))
            throw new ArgumentException($"Property '{propertyName}' does not exist", nameof(propertyName));

        if (!property.IsWritable)
            throw new InvalidOperationException($"Property '{propertyName}' is not writable");

        // Validate value type
        if (value != null && !property.PropertyType.IsAssignableFrom(value.GetType()))
        {
            // Try type conversion
            try
            {
                value = Convert.ChangeType(value, property.PropertyType);
            }
            catch
            {
                throw new ArgumentException(
                    $"Value type mismatch, expected {property.PropertyType.Name}, actual {value.GetType().Name}");
            }
        }

        // Validate numeric range
        if (property.MinValue != null || property.MaxValue != null)
        {
            if (value is IComparable comparable)
            {
                if (property.MinValue != null && comparable.CompareTo(property.MinValue) < 0)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"Value less than minimum {property.MinValue}");

                if (property.MaxValue != null && comparable.CompareTo(property.MaxValue) > 0)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"Value greater than maximum {property.MaxValue}");
            }
        }

        var oldValue = property.Value;
        property.Value = value;
        property.LastUpdated = DateTime.UtcNow;

        _logger.LogInformation("Writing virtual device property: {PropertyName} = {Value} (old value: {OldValue})",
            propertyName, value, oldValue);

        // Trigger property change event
        PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
        {
            PropertyName = propertyName,
            OldValue = oldValue,
            NewValue = value,
            ChangedAt = DateTime.UtcNow
        });

        return Task.FromResult(true);
    }

    public Task<DeviceActionResult> ExecuteActionAsync(string actionName, Dictionary<string, object>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (_status != DeviceConnectionStatus.Connected)
            throw new InvalidOperationException("Device not connected");

        if (!_supportedActions.TryGetValue(actionName, out var action))
            throw new ArgumentException($"Action '{actionName}' does not exist", nameof(actionName));

        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Executing virtual device action: {ActionName}", actionName);

            // Validate parameters
            parameters ??= new Dictionary<string, object>();
            foreach (var param in action.Parameters)
            {
                if (param.Value.IsRequired && !parameters.ContainsKey(param.Key))
                    throw new ArgumentException($"Missing required parameter: {param.Key}");
            }

            // Simulate action execution
            var result = SimulateActionExecution(actionName, parameters);

            stopwatch.Stop();

            _logger.LogInformation("Virtual device action execution completed: {ActionName}, duration: {ElapsedMs}ms",
                actionName, stopwatch.ElapsedMilliseconds);

            return Task.FromResult(new DeviceActionResult
            {
                IsSuccess = true,
                Result = result,
                ExecutedAt = startTime,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Virtual device action execution failed: {ActionName}", actionName);

            return Task.FromResult(new DeviceActionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ExecutedAt = startTime,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            });
        }
    }

    public Task<DeviceHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var isHealthy =
            _status == DeviceConnectionStatus.Connected && _random.NextDouble() > 0.1; // 10% probability unhealthy

        var healthStatus = new DeviceHealthStatus
        {
            IsHealthy = isHealthy,
            StatusDescription = isHealthy ? "Device running normally" : "Device has warnings",
            LastCheckTime = DateTime.UtcNow,
            Details = new Dictionary<string, object>
            {
                ["ConnectionStatus"] = _status.ToString(),
                ["Uptime"] = DateTime.UtcNow - (DateTime.UtcNow.AddHours(-1)), // Simulate uptime
                ["MemoryUsage"] = $"{_random.Next(30, 80)}%",
                ["CPUUsage"] = $"{_random.Next(5, 25)}%",
                ["Temperature"] = $"{_random.Next(35, 65)}°C"
            }
        };

        return Task.FromResult(healthStatus);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopSimulation();
            _disposed = true;
            _logger.LogInformation("Virtual device connection disposed: {DeviceId}", DeviceId);
        }
    }

    private void InitializeVirtualDevice()
    {
        // Initialize different properties and actions based on device type
        switch (DeviceType.ToLowerInvariant())
        {
            case "smartlight":
            case "smart light":
                InitializeSmartLight();
                break;

            case "temperaturesensor":
            case "temperature sensor":
                InitializeTemperatureSensor();
                break;

            case "thermostat":
                InitializeThermostat();
                break;

            default:
                InitializeGenericDevice();
                break;
        }
    }

    private void InitializeSmartLight()
    {
        // Smart light properties
        _properties["Power"] = new DeviceProperty
        {
            Name = "Power",
            DisplayName = "Power Status",
            Description = "Light bulb power on/off status",
            PropertyType = typeof(bool),
            Value = false,
            IsReadable = true,
            IsWritable = true
        };

        _properties["Brightness"] = new DeviceProperty
        {
            Name = "Brightness",
            DisplayName = "Brightness",
            Description = "Light bulb brightness percentage",
            PropertyType = typeof(int),
            Value = 100,
            IsReadable = true,
            IsWritable = true,
            Unit = "%",
            MinValue = 0,
            MaxValue = 100
        };

        _properties["Color"] = new DeviceProperty
        {
            Name = "Color",
            DisplayName = "Color",
            Description = "Light bulb color (RGB hex)",
            PropertyType = typeof(string),
            Value = "#FFFFFF",
            IsReadable = true,
            IsWritable = true
        };

        // Smart light actions
        _supportedActions["TurnOn"] = new DeviceAction
        {
            Name = "TurnOn",
            DisplayName = "Turn On",
            Description = "Turn on the smart light bulb"
        };

        _supportedActions["TurnOff"] = new DeviceAction
        {
            Name = "TurnOff",
            DisplayName = "Turn Off",
            Description = "Turn off the smart light bulb"
        };

        _supportedActions["SetBrightness"] = new DeviceAction
        {
            Name = "SetBrightness",
            DisplayName = "Set Brightness",
            Description = "Set light bulb brightness",
            Parameters = new Dictionary<string, DeviceActionParameter>
            {
                ["brightness"] = new DeviceActionParameter
                {
                    Name = "brightness",
                    DisplayName = "Brightness Value",
                    Description = "Brightness percentage (0-100)",
                    ParameterType = typeof(int),
                    IsRequired = true
                }
            }
        };
    }

    private void InitializeTemperatureSensor()
    {
        // Temperature sensor properties
        _properties["Temperature"] = new DeviceProperty
        {
            Name = "Temperature",
            DisplayName = "Temperature",
            Description = "Current ambient temperature",
            PropertyType = typeof(double),
            Value = 25.0,
            IsReadable = true,
            IsWritable = false,
            Unit = "°C",
            MinValue = -40.0,
            MaxValue = 85.0
        };

        _properties["Humidity"] = new DeviceProperty
        {
            Name = "Humidity",
            DisplayName = "Humidity",
            Description = "Current ambient humidity",
            PropertyType = typeof(double),
            Value = 60.0,
            IsReadable = true,
            IsWritable = false,
            Unit = "%",
            MinValue = 0.0,
            MaxValue = 100.0
        };

        // Temperature sensor actions
        _supportedActions["Calibrate"] = new DeviceAction
        {
            Name = "Calibrate",
            DisplayName = "Calibrate",
            Description = "Calibrate temperature sensor"
        };
    }

    private void InitializeThermostat()
    {
        // Thermostat properties
        _properties["CurrentTemperature"] = new DeviceProperty
        {
            Name = "CurrentTemperature",
            DisplayName = "Current Temperature",
            Description = "Current indoor temperature",
            PropertyType = typeof(double),
            Value = 22.0,
            IsReadable = true,
            IsWritable = false,
            Unit = "°C"
        };

        _properties["TargetTemperature"] = new DeviceProperty
        {
            Name = "TargetTemperature",
            DisplayName = "Target Temperature",
            Description = "Target indoor temperature",
            PropertyType = typeof(double),
            Value = 24.0,
            IsReadable = true,
            IsWritable = true,
            Unit = "°C",
            MinValue = 10.0,
            MaxValue = 35.0
        };

        _properties["Mode"] = new DeviceProperty
        {
            Name = "Mode",
            DisplayName = "Working Mode",
            Description = "Thermostat working mode",
            PropertyType = typeof(string),
            Value = "Auto",
            IsReadable = true,
            IsWritable = true
        };

        // Thermostat actions
        _supportedActions["SetMode"] = new DeviceAction
        {
            Name = "SetMode",
            DisplayName = "Set Mode",
            Description = "Set thermostat working mode",
            Parameters = new Dictionary<string, DeviceActionParameter>
            {
                ["mode"] = new DeviceActionParameter
                {
                    Name = "mode",
                    DisplayName = "Working Mode",
                    Description = "Auto/Heat/Cool/Off",
                    ParameterType = typeof(string),
                    IsRequired = true
                }
            }
        };
    }

    private void InitializeGenericDevice()
    {
        // Generic device properties
        _properties["Status"] = new DeviceProperty
        {
            Name = "Status",
            DisplayName = "Status",
            Description = "Device status",
            PropertyType = typeof(string),
            Value = "Ready",
            IsReadable = true,
            IsWritable = false
        };

        _properties["Value"] = new DeviceProperty
        {
            Name = "Value",
            DisplayName = "Value",
            Description = "Device value",
            PropertyType = typeof(double),
            Value = 0.0,
            IsReadable = true,
            IsWritable = true
        };

        // Generic device actions
        _supportedActions["Reset"] = new DeviceAction
        {
            Name = "Reset",
            DisplayName = "Reset",
            Description = "Reset device"
        };
    }

    private void ChangeStatus(DeviceConnectionStatus newStatus, string reason)
    {
        var oldStatus = _status;
        _status = newStatus;

        if (oldStatus != newStatus)
        {
            _logger.LogInformation(
                "Virtual device status change: {DeviceId}, {OldStatus} -> {NewStatus}, reason: {Reason}",
                DeviceId, oldStatus, newStatus, reason);

            StatusChanged?.Invoke(this, new DeviceConnectionStatusChangedEventArgs
            {
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow,
                Reason = reason
            });
        }
    }

    private void StartSimulation()
    {
        // Start simulation timer to periodically update sensor data
        _simulationTimer = new Timer(SimulateDeviceData, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));
    }

    private void StopSimulation()
    {
        _simulationTimer?.Dispose();
        _simulationTimer = null;
    }

    private void SimulateDeviceData(object? state)
    {
        if (_status != DeviceConnectionStatus.Connected) return;

        try
        {
            // Simulate sensor data changes
            foreach (var property in _properties.Values.Where(p => p.IsReadable && !p.IsWritable))
            {
                var oldValue = property.Value;
                object? newValue = null;

                switch (property.Name)
                {
                    case "Temperature":
                        if (oldValue is double temp)
                        {
                            newValue = Math.Round(temp + (_random.NextDouble() - 0.5) * 2, 1);
                            newValue = Math.Max(-40, Math.Min(85, (double)newValue));
                        }

                        break;

                    case "Humidity":
                        if (oldValue is double humidity)
                        {
                            newValue = Math.Round(humidity + (_random.NextDouble() - 0.5) * 5, 1);
                            newValue = Math.Max(0, Math.Min(100, (double)newValue));
                        }

                        break;

                    case "CurrentTemperature":
                        if (oldValue is double currentTemp &&
                            _properties.TryGetValue("TargetTemperature", out var targetProp))
                        {
                            if (targetProp.Value is double targetTemp)
                            {
                                var diff = targetTemp - currentTemp;
                                var change = Math.Sign(diff) * Math.Min(Math.Abs(diff), 0.5);
                                newValue = Math.Round(currentTemp + change + (_random.NextDouble() - 0.5) * 0.2, 1);
                            }
                        }

                        break;
                }

                if (newValue != null && !Equals(oldValue, newValue))
                {
                    property.Value = newValue;
                    property.LastUpdated = DateTime.UtcNow;

                    PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                    {
                        PropertyName = property.Name,
                        OldValue = oldValue,
                        NewValue = newValue,
                        ChangedAt = DateTime.UtcNow
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while simulating device data: {DeviceId}", DeviceId);
        }
    }

    private object? SimulateActionExecution(string actionName, Dictionary<string, object> parameters)
    {
        switch (actionName)
        {
            case "TurnOn":
                if (_properties.TryGetValue("Power", out var powerProp))
                {
                    var oldValue = powerProp.Value;
                    powerProp.Value = true;
                    powerProp.LastUpdated = DateTime.UtcNow;

                    PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                    {
                        PropertyName = "Power",
                        OldValue = oldValue,
                        NewValue = true,
                        ChangedAt = DateTime.UtcNow
                    });
                }

                return "Light bulb turned on";

            case "TurnOff":
                if (_properties.TryGetValue("Power", out var powerOffProp))
                {
                    var oldValue = powerOffProp.Value;
                    powerOffProp.Value = false;
                    powerOffProp.LastUpdated = DateTime.UtcNow;

                    PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                    {
                        PropertyName = "Power",
                        OldValue = oldValue,
                        NewValue = false,
                        ChangedAt = DateTime.UtcNow
                    });
                }

                return "Light bulb turned off";

            case "SetBrightness":
                if (parameters.TryGetValue("brightness", out var brightnessObj) &&
                    _properties.TryGetValue("Brightness", out var brightnessProp))
                {
                    var brightness = Convert.ToInt32(brightnessObj);
                    var oldValue = brightnessProp.Value;
                    brightnessProp.Value = brightness;
                    brightnessProp.LastUpdated = DateTime.UtcNow;

                    PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                    {
                        PropertyName = "Brightness",
                        OldValue = oldValue,
                        NewValue = brightness,
                        ChangedAt = DateTime.UtcNow
                    });
                }

                return $"Brightness set to {parameters["brightness"]}%";

            case "SetMode":
                if (parameters.TryGetValue("mode", out var modeObj) &&
                    _properties.TryGetValue("Mode", out var modeProp))
                {
                    var mode = modeObj.ToString();
                    var oldValue = modeProp.Value;
                    modeProp.Value = mode;
                    modeProp.LastUpdated = DateTime.UtcNow;

                    PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                    {
                        PropertyName = "Mode",
                        OldValue = oldValue,
                        NewValue = mode,
                        ChangedAt = DateTime.UtcNow
                    });
                }

                return $"Mode set to {parameters["mode"]}";

            case "Calibrate":
                // Simulate calibration delay
                Thread.Sleep(2000);
                return "Sensor calibration completed";

            case "Reset":
                // Reset all writable properties to default values
                foreach (var prop in _properties.Values.Where(p => p.IsWritable))
                {
                    var oldValue = prop.Value;
                    object? defaultValue = prop.PropertyType.IsValueType
                        ? Activator.CreateInstance(prop.PropertyType)
                        : null;

                    prop.Value = defaultValue;
                    prop.LastUpdated = DateTime.UtcNow;

                    PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                    {
                        PropertyName = prop.Name,
                        OldValue = oldValue,
                        NewValue = defaultValue,
                        ChangedAt = DateTime.UtcNow
                    });
                }

                return "Device reset";

            default:
                return $"Action {actionName} execution completed";
        }
    }

    /// <summary>
    /// Apply extended properties from configuration to override default values
    /// </summary>
    private void ApplyExtendedProperties(Dictionary<string, string> extendedProperties)
    {
        foreach (var kvp in extendedProperties)
        {
            var propertyName = kvp.Key;
            var propertyValueString = kvp.Value;

            if (_properties.TryGetValue(propertyName, out var property))
            {
                try
                {
                    // Convert string value to the appropriate type
                    var convertedValue = Convert.ChangeType(propertyValueString, property.PropertyType);
                    property.Value = convertedValue;
                    property.LastUpdated = DateTime.UtcNow;
                    
                    _logger.LogDebug("Applied extended property: {PropertyName} = {Value} ({Type})", 
                        propertyName, convertedValue, property.PropertyType.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert extended property {PropertyName} value '{Value}' to type {Type}", 
                        propertyName, propertyValueString, property.PropertyType.Name);
                }
            }
            else
            {
                _logger.LogDebug("Extended property {PropertyName} not found in device properties", propertyName);
            }
        }
    }
}