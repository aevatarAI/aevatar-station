using System.Text;
using System.Text.Json;
using Aevatar.GAgents.Device.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Device.Http;

/// <summary>
/// Virtual Device API Connection - connects to Virtual Device Hub via HTTP API
/// </summary>
public class HttpDeviceConnection : IDeviceConnection
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpDeviceConnection> _logger;
    private readonly HttpDeviceHubOptions _options;
    private readonly Dictionary<string, DeviceProperty> _properties = new();
    private readonly Dictionary<string, DeviceAction> _supportedActions = new();
    private DeviceConnectionStatus _status = DeviceConnectionStatus.Disconnected;
    private DeviceInfo? _deviceInfo;
    private bool _disposed = false;

    public string DeviceId { get; private set; }
    public string DeviceName { get; private set; } = string.Empty;
    public string DeviceType { get; private set; }
    public DeviceConnectionStatus Status => _status;

    public IReadOnlyDictionary<string, DeviceProperty> Properties => _properties;
    public IReadOnlyDictionary<string, DeviceAction> SupportedActions => _supportedActions;

    public event EventHandler<DevicePropertyChangedEventArgs>? PropertyChanged;
    public event EventHandler<DeviceConnectionStatusChangedEventArgs>? StatusChanged;

    public HttpDeviceConnection(
        HttpClient httpClient,
        ILogger<HttpDeviceConnection> logger,
        string deviceId,
        string deviceType,
        HttpDeviceHubOptions? options = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        DeviceId = deviceId;
        DeviceType = deviceType;
        _options = options ?? new HttpDeviceHubOptions();
        
        ConfigureHttpClient();
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Connecting to Virtual Device Hub API: {DeviceId} ({DeviceType})", DeviceId, DeviceType);

        ChangeStatus(DeviceConnectionStatus.Connecting, "Connecting to API");

        try
        {
            // Get device information from API
            _deviceInfo = await GetDeviceInfoFromApiAsync(cancellationToken);
            
            if (_deviceInfo == null)
            {
                ChangeStatus(DeviceConnectionStatus.Error, "Device not found in API");
                return false;
            }

            DeviceName = _deviceInfo.Name;
            
            // Initialize properties and actions based on device info
            await InitializeDeviceCapabilitiesAsync(_deviceInfo, cancellationToken);
            
            ChangeStatus(DeviceConnectionStatus.Connected, "Connected to API");
            _logger.LogInformation("Virtual device connected via API: {DeviceId} ({DeviceName})", DeviceId, DeviceName);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Virtual Device Hub API: {DeviceId}", DeviceId);
            ChangeStatus(DeviceConnectionStatus.Error, $"Connection failed: {ex.Message}");
            return false;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Disconnecting from Virtual Device Hub API: {DeviceId}", DeviceId);
        
        ChangeStatus(DeviceConnectionStatus.Disconnected, "Manually disconnected");
        
        _logger.LogInformation("Virtual device disconnected from API: {DeviceId}", DeviceId);
    }

    public async Task<object?> ReadPropertyAsync(string propertyName, CancellationToken cancellationToken = default)
    {
        if (_status != DeviceConnectionStatus.Connected)
            throw new InvalidOperationException("Device not connected");

        if (!_properties.TryGetValue(propertyName, out var property))
            throw new ArgumentException($"Property '{propertyName}' does not exist", nameof(propertyName));

        if (!property.IsReadable)
            throw new InvalidOperationException($"Property '{propertyName}' is not readable");

        try
        {
            // Get current device status from API
            var statusResponse = await GetDeviceStatusFromApiAsync(cancellationToken);
            
            if (statusResponse?.Data != null && statusResponse.Data.TryGetValue(propertyName.ToLowerInvariant(), out var value))
            {
                property.Value = value;
                property.LastUpdated = DateTime.UtcNow;
                
                _logger.LogDebug("Read property from API: {PropertyName} = {Value}", propertyName, value);
                return value;
            }
            
            _logger.LogWarning("Property {PropertyName} not found in API response", propertyName);
            return property.Value; // Return cached value
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read property from API: {PropertyName}", propertyName);
            return property.Value; // Return cached value
        }
    }

    public async Task<bool> WritePropertyAsync(string propertyName, object value, CancellationToken cancellationToken = default)
    {
        if (_status != DeviceConnectionStatus.Connected)
            throw new InvalidOperationException("Device not connected");

        if (!_properties.TryGetValue(propertyName, out var property))
            throw new ArgumentException($"Property '{propertyName}' does not exist", nameof(propertyName));

        if (!property.IsWritable)
            throw new InvalidOperationException($"Property '{propertyName}' is not writable");

        try
        {
            // Send command to API to update property
            var command = new Dictionary<string, object>
            {
                [propertyName.ToLowerInvariant()] = value
            };

            var success = await SendCommandToApiAsync(command, cancellationToken);
            
            if (success)
            {
                var oldValue = property.Value;
                property.Value = value;
                property.LastUpdated = DateTime.UtcNow;

                PropertyChanged?.Invoke(this, new DevicePropertyChangedEventArgs
                {
                    PropertyName = propertyName,
                    OldValue = oldValue,
                    NewValue = value,
                    ChangedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Property written via API: {PropertyName} = {Value}", propertyName, value);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write property via API: {PropertyName}", propertyName);
            return false;
        }
    }

    public async Task<DeviceActionResult> ExecuteActionAsync(string actionName, Dictionary<string, object>? parameters = null, CancellationToken cancellationToken = default)
    {
        if (_status != DeviceConnectionStatus.Connected)
            throw new InvalidOperationException("Device not connected");

        if (!_supportedActions.TryGetValue(actionName, out var action))
            throw new ArgumentException($"Action '{actionName}' does not exist", nameof(actionName));

        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Executing action via API: {ActionName}", actionName);

            // Prepare command based on action and parameters
            var command = PrepareActionCommand(actionName, parameters);
            
            var success = await SendCommandToApiAsync(command, cancellationToken);
            
            stopwatch.Stop();

            var result = new DeviceActionResult
            {
                IsSuccess = success,
                Result = success ? $"Action {actionName} executed successfully" : $"Action {actionName} failed",
                ExecutedAt = startTime,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };

            _logger.LogInformation("API action execution completed: {ActionName}, success: {Success}, duration: {ElapsedMs}ms",
                actionName, success, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "API action execution failed: {ActionName}", actionName);

            return new DeviceActionResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ExecutedAt = startTime,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<DeviceHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var deviceInfo = await GetDeviceInfoFromApiAsync(cancellationToken);
            
            var isHealthy = deviceInfo?.IsOnline ?? false;
            
            return new DeviceHealthStatus
            {
                IsHealthy = isHealthy,
                StatusDescription = isHealthy ? "Device online and responding" : "Device offline or not responding",
                LastCheckTime = DateTime.UtcNow,
                Details = new Dictionary<string, object>
                {
                    ["IsOnline"] = deviceInfo?.IsOnline ?? false,
                    ["LastUpdated"] = deviceInfo?.LastUpdated ?? DateTime.MinValue,
                    ["ApiConnection"] = _status.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status from API: {DeviceId}", DeviceId);
            
            return new DeviceHealthStatus
            {
                IsHealthy = false,
                StatusDescription = $"Health check failed: {ex.Message}",
                LastCheckTime = DateTime.UtcNow,
                Details = new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["ApiConnection"] = _status.ToString()
                }
            };
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _logger.LogInformation("Virtual device API connection disposed: {DeviceId}", DeviceId);
        }
    }

    #region Private Methods

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        
        if (!string.IsNullOrEmpty(_options.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        }
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Aevatar-GAgent-Device-Virtual/1.0");
    }

    private async Task<DeviceInfo?> GetDeviceInfoFromApiAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/devices/{DeviceId}", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Device not found in API: {DeviceId}", DeviceId);
                    return null;
                }
                
                response.EnsureSuccessStatusCode();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var deviceResponse = JsonSerializer.Deserialize<DeviceResponse>(content);
            
            return deviceResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device info from API: {DeviceId}", DeviceId);
            throw;
        }
    }

    private async Task<StatusResponse?> GetDeviceStatusFromApiAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/devices/{DeviceId}/status", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<StatusResponse>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device status from API: {DeviceId}", DeviceId);
            throw;
        }
    }

    private async Task<bool> SendCommandToApiAsync(Dictionary<string, object> command, CancellationToken cancellationToken)
    {
        try
        {
            var commandRequest = new DeviceCommandRequest { Command = command };
            var json = JsonSerializer.Serialize(commandRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/devices/{DeviceId}/command", content, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Command sent successfully to API: {DeviceId}, command: {Command}", DeviceId, json);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Command failed in API: {DeviceId}, status: {StatusCode}, error: {Error}", 
                    DeviceId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send command to API: {DeviceId}", DeviceId);
            return false;
        }
    }

    private async Task InitializeDeviceCapabilitiesAsync(DeviceInfo deviceInfo, CancellationToken cancellationToken)
    {
        // Initialize properties based on device type and current status
        if (deviceInfo.Status != null)
        {
            foreach (var statusItem in deviceInfo.Status)
            {
                var property = CreatePropertyFromStatus(statusItem.Key, statusItem.Value, deviceInfo.Type);
                _properties[property.Name] = property;
            }
        }

        // Initialize supported actions
        foreach (var commandName in deviceInfo.SupportedCommands)
        {
            var action = CreateActionFromCommand(commandName, deviceInfo.Type);
            _supportedActions[action.Name] = action;
        }

        _logger.LogInformation("Initialized {PropertyCount} properties and {ActionCount} actions for device {DeviceId}",
            _properties.Count, _supportedActions.Count, DeviceId);
    }

    private DeviceProperty CreatePropertyFromStatus(string name, object value, string deviceType)
    {
        var propertyName = ConvertApiNameToPropertyName(name);
        
        return new DeviceProperty
        {
            Name = propertyName,
            DisplayName = propertyName,
            Description = $"Device {propertyName.ToLowerInvariant()} property",
            PropertyType = value?.GetType() ?? typeof(object),
            Value = value,
            IsReadable = true,
            IsWritable = IsWritableProperty(name, deviceType),
            LastUpdated = DateTime.UtcNow
        };
    }

    private DeviceAction CreateActionFromCommand(string commandName, string deviceType)
    {
        return new DeviceAction
        {
            Name = commandName,
            DisplayName = ConvertApiNameToDisplayName(commandName),
            Description = $"Execute {commandName} command",
            Parameters = GetActionParameters(commandName, deviceType)
        };
    }

    private Dictionary<string, object> PrepareActionCommand(string actionName, Dictionary<string, object>? parameters)
    {
        var command = new Dictionary<string, object>();

        // Map common action names to API commands
        switch (actionName.ToLowerInvariant())
        {
            case "turn_on":
            case "turnon":
                command["power"] = true;
                break;
                
            case "turn_off":
            case "turnoff":
                command["power"] = false;
                break;
                
            case "set_brightness":
            case "setbrightness":
                if (parameters?.TryGetValue("brightness", out var brightness) == true)
                    command["brightness"] = brightness;
                break;
                
            case "set_color":
            case "setcolor":
                if (parameters?.TryGetValue("color", out var color) == true)
                    command["color"] = color;
                break;
                
            case "calibrate":
                command["calibrate"] = true;
                break;
                
            default:
                // For other actions, pass parameters directly
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command[param.Key.ToLowerInvariant()] = param.Value;
                    }
                }
                break;
        }

        return command;
    }

    private void ChangeStatus(DeviceConnectionStatus newStatus, string reason)
    {
        var oldStatus = _status;
        _status = newStatus;

        if (oldStatus != newStatus)
        {
            _logger.LogInformation("Virtual device API status change: {DeviceId}, {OldStatus} -> {NewStatus}, reason: {Reason}",
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

    private static string ConvertApiNameToPropertyName(string apiName)
    {
        // Convert API naming (snake_case) to property naming (PascalCase)
        return string.Join("", apiName.Split('_').Select(word => 
            char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }

    private static string ConvertApiNameToDisplayName(string apiName)
    {
        // Convert API naming to display friendly names
        return string.Join(" ", apiName.Split('_').Select(word => 
            char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }

    private static bool IsWritableProperty(string propertyName, string deviceType)
    {
        // Define which properties are writable based on device type
        var writableProperties = deviceType.ToLowerInvariant() switch
        {
            "smart-light" => new[] { "power", "brightness", "color", "color_temperature" },
            "smart-switch" => new[] { "power" },
            "temperature-sensor" => Array.Empty<string>(), // Sensors are typically read-only
            _ => Array.Empty<string>()
        };

        return writableProperties.Contains(propertyName.ToLowerInvariant());
    }

    private static Dictionary<string, DeviceActionParameter> GetActionParameters(string actionName, string deviceType)
    {
        var parameters = new Dictionary<string, DeviceActionParameter>();

        switch (actionName.ToLowerInvariant())
        {
            case "set_brightness":
                parameters["brightness"] = new DeviceActionParameter
                {
                    Name = "brightness",
                    DisplayName = "Brightness",
                    Description = "Brightness percentage (0-100)",
                    ParameterType = typeof(int),
                    IsRequired = true
                };
                break;
                
            case "set_color":
                parameters["color"] = new DeviceActionParameter
                {
                    Name = "color",
                    DisplayName = "Color",
                    Description = "Color in hex format (e.g., #FF0000)",
                    ParameterType = typeof(string),
                    IsRequired = true
                };
                break;
        }

        return parameters;
    }

    #endregion
}
