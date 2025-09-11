using System.ComponentModel;
using System.Text.Json;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.Device.Abstractions;
using Aevatar.GAgents.Device.Events;
using Aevatar.GAgents.Device.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Orleans;

namespace Aevatar.GAgents.Device.GAgents;

/// <summary>
/// Device GAgent interface
/// </summary>
public interface IDeviceGAgent<TDeviceConnection> : IStateGAgent<DeviceGAgentState>, IAIGAgent
    where TDeviceConnection : IDeviceConnection
{
    /// <summary>
    /// Get device connection instance
    /// </summary>
    /// <returns>Device connection instance</returns>
    Task<TDeviceConnection?> GetDeviceConnectionAsync();
    
    /// <summary>
    /// Initialize device connection
    /// </summary>
    /// <param name="connectionConfig">Connection configuration</param>
    /// <returns>Whether initialization was successful</returns>
    Task<bool> InitializeDeviceConnectionAsync(DeviceConnectionConfig connectionConfig);
    
    /// <summary>
    /// Get device status summary
    /// </summary>
    /// <returns>Device status summary</returns>
    Task<DeviceStatusSummary> GetDeviceStatusSummaryAsync();
    
    /// <summary>
    /// Enable or disable device monitoring
    /// </summary>
    /// <param name="enabled">Whether to enable</param>
    /// <returns>Whether operation was successful</returns>
    Task<bool> SetDeviceMonitoringAsync(bool enabled);
}

/// <summary>
/// Device GAgent base class - provides basic functionality for interacting with physical or virtual devices
/// </summary>
/// <typeparam name="TDeviceConnection">Device connection type</typeparam>
[Description("Smart device agent providing device connection, status monitoring, property read/write and operation execution, supports device management through AI assistant")]
public abstract class DeviceGAgentBase<TDeviceConnection> : 
    AIGAgentBase<DeviceGAgentState, DeviceGAgentStateLogEvent>, 
    IDeviceGAgent<TDeviceConnection>
    where TDeviceConnection : class, IDeviceConnection
{
    protected TDeviceConnection? DeviceConnection { get; private set; }
    private IDisposable? _monitoringTimer;
    private readonly object _connectionLock = new();

    public override async Task<string> GetDescriptionAsync()
    {
        var deviceType = DeviceConnection?.DeviceType ?? "Unknown Device";
        var deviceName = DeviceConnection?.DeviceName ?? "Device";
        return $"Smart {deviceType} agent ({deviceName}) - Provides device connection management, status monitoring, property read/write and operation execution, supports intelligent device control through AI assistant";
    }

    protected override async Task OnAIGAgentActivateAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("DeviceGAgent {GrainId} is activating", this.GetGrainId());
        await base.OnAIGAgentActivateAsync(cancellationToken);
        
        // If there's connection config in state, try to reconnect device
        if (State.ConnectionConfig != null)
        {
            await TryReconnectDeviceAsync();
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("DeviceGAgent {GrainId} is deactivating, reason: {Reason}", this.GetGrainId(), reason);
        
        // Stop monitoring
        _monitoringTimer?.Dispose();
        
        // Disconnect device
        if (DeviceConnection != null)
        {
            try
            {
                await DeviceConnection.DisconnectAsync(cancellationToken);
                DeviceConnection.Dispose();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error occurred while disconnecting device");
            }
        }
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    /// <summary>
    /// Subclasses need to implement this method to create specific device connection instances
    /// </summary>
    /// <param name="config">Device connection configuration</param>
    /// <returns>Device connection instance</returns>
    protected abstract Task<TDeviceConnection> CreateDeviceConnectionAsync(DeviceConnectionConfig config);

    public Task<TDeviceConnection?> GetDeviceConnectionAsync()
    {
        return Task.FromResult(DeviceConnection);
    }

    public async Task<bool> InitializeDeviceConnectionAsync(DeviceConnectionConfig connectionConfig)
    {
        try
        {
            lock (_connectionLock)
            {
                // If there's already a connection, disconnect first
                DeviceConnection?.Dispose();
                DeviceConnection = null;
            }

            // Create new device connection
            var connection = await CreateDeviceConnectionAsync(connectionConfig);
            
            // Subscribe to device events
            connection.PropertyChanged += OnDevicePropertyChanged;
            connection.StatusChanged += OnDeviceConnectionStatusChanged;
            
            // Try to connect to device
            var connected = await connection.ConnectAsync();
            
            if (connected)
            {
                lock (_connectionLock)
                {
                    DeviceConnection = connection;
                }
                
                // Update state
                RaiseEvent(new DeviceConnectionInitializedLogEvent
                {
                    Config = connectionConfig,
                    DeviceId = connection.DeviceId,
                    DeviceName = connection.DeviceName,
                    DeviceType = connection.DeviceType,
                    ConnectedAt = DateTime.UtcNow
                });
                
                await ConfirmEvents();
                
                // Register device actions as Semantic Kernel functions
                await RegisterDeviceActionsAsKernelFunctionsAsync();
                
                Logger.LogInformation("Device connection initialized successfully: {DeviceId} ({DeviceName})", 
                    connection.DeviceId, connection.DeviceName);
                
                return true;
            }
            else
            {
                connection.Dispose();
                Logger.LogWarning("Device connection failed: {DeviceId}", connectionConfig.DeviceId);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while initializing device connection: {DeviceId}", connectionConfig.DeviceId);
            return false;
        }
    }

    public async Task<DeviceStatusSummary> GetDeviceStatusSummaryAsync()
    {
        if (DeviceConnection == null)
        {
            return new DeviceStatusSummary
            {
                DeviceId = State.DeviceId,
                DeviceName = State.DeviceName,
                DeviceType = State.DeviceType,
                IsConnected = false,
                Status = DeviceConnectionStatus.Disconnected,
                LastUpdated = DateTime.UtcNow,
                ErrorMessage = "Device not connected"
            };
        }

        try
        {
            var healthStatus = await DeviceConnection.GetHealthStatusAsync();
            
            return new DeviceStatusSummary
            {
                DeviceId = DeviceConnection.DeviceId,
                DeviceName = DeviceConnection.DeviceName,
                DeviceType = DeviceConnection.DeviceType,
                IsConnected = DeviceConnection.Status == DeviceConnectionStatus.Connected,
                Status = DeviceConnection.Status,
                IsHealthy = healthStatus.IsHealthy,
                LastUpdated = DateTime.UtcNow,
                Properties = DeviceConnection.Properties.ToDictionary(
                    p => p.Key, 
                    p => p.Value.Value
                ),
                HealthDetails = healthStatus.Details
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while getting device status: {DeviceId}", DeviceConnection.DeviceId);
            
            return new DeviceStatusSummary
            {
                DeviceId = DeviceConnection.DeviceId,
                DeviceName = DeviceConnection.DeviceName,
                DeviceType = DeviceConnection.DeviceType,
                IsConnected = false,
                Status = DeviceConnectionStatus.Error,
                LastUpdated = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> SetDeviceMonitoringAsync(bool enabled)
    {
        if (enabled && State.MonitoringEnabled != enabled)
        {
            // Start monitoring timer
            _monitoringTimer = this.RegisterGrainTimer(
                async (token) => await PerformDeviceMonitoringAsync(token),
                new GrainTimerCreationOptions
                {
                    DueTime = TimeSpan.FromSeconds(5),
                    Period = TimeSpan.FromSeconds(State.MonitoringIntervalSeconds),
                    Interleave = true
                }
            );
            
            Logger.LogInformation("Device monitoring started, interval: {Interval} seconds", State.MonitoringIntervalSeconds);
        }
        else if (!enabled && _monitoringTimer != null)
        {
            // Stop monitoring timer
            _monitoringTimer.Dispose();
            _monitoringTimer = null;
            
            Logger.LogInformation("Device monitoring stopped");
        }

        RaiseEvent(new DeviceMonitoringStateChangedLogEvent
        {
            Enabled = enabled,
            ChangedAt = DateTime.UtcNow
        });
        
        await ConfirmEvents();
        return true;
    }

    #region Event Handlers

    [EventHandler]
    public async Task HandleReadDevicePropertyAsync(ReadDevicePropertyEvent @event)
    {
        Logger.LogInformation("Reading device property: {PropertyName}", @event.PropertyName);
        
        if (DeviceConnection == null)
        {
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "DEVICE_NOT_CONNECTED",
                ErrorMessage = "Device not connected",
                Timestamp = DateTime.UtcNow
            });
            return;
        }

        try
        {
            var value = await DeviceConnection.ReadPropertyAsync(@event.PropertyName);
            
            // Publish property read result event (can be subscribed by other GAgents)
            await PublishAsync(new DevicePropertyReadResultEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                PropertyName = @event.PropertyName,
                Value = value,
                Timestamp = DateTime.UtcNow
            });
            
            Logger.LogInformation("Successfully read device property: {PropertyName} = {Value}", 
                @event.PropertyName, value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to read device property: {PropertyName}", @event.PropertyName);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "PROPERTY_READ_FAILED",
                ErrorMessage = $"Failed to read property: {ex.Message}",
                ErrorDetailsJson = JsonSerializer.Serialize(new { PropertyName = @event.PropertyName, Exception = ex.GetType().Name }),
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [EventHandler]
    public async Task HandleWriteDevicePropertyAsync(WriteDevicePropertyEvent @event)
    {
        Logger.LogInformation("Writing device property: {PropertyName} = {Value}", 
            @event.PropertyName, @event.ValueJson);
        
        if (DeviceConnection == null)
        {
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "DEVICE_NOT_CONNECTED",
                ErrorMessage = "Device not connected",
                Timestamp = DateTime.UtcNow
            });
            return;
        }

        try
        {
            // Deserialize value
            var value = JsonSerializer.Deserialize<object>(@event.ValueJson);
            
            var success = await DeviceConnection.WritePropertyAsync(@event.PropertyName, value!);
            
            if (success)
            {
                // Publish property write success event
                await PublishAsync(new DevicePropertyWriteResultEvent
                {
                    DeviceId = @event.DeviceId,
                    DeviceName = @event.DeviceName,
                    PropertyName = @event.PropertyName,
                    Value = value,
                    Success = true,
                    Timestamp = DateTime.UtcNow
                });
                
                Logger.LogInformation("Successfully wrote device property: {PropertyName}", @event.PropertyName);
            }
            else
            {
                await PublishAsync(new DeviceErrorEvent
                {
                    DeviceId = @event.DeviceId,
                    DeviceName = @event.DeviceName,
                    ErrorCode = "PROPERTY_WRITE_FAILED",
                    ErrorMessage = "Failed to write property",
                    ErrorDetailsJson = JsonSerializer.Serialize(new { PropertyName = @event.PropertyName }),
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to write device property: {PropertyName}", @event.PropertyName);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "PROPERTY_WRITE_EXCEPTION",
                ErrorMessage = $"Property write exception: {ex.Message}",
                ErrorDetailsJson = JsonSerializer.Serialize(new { PropertyName = @event.PropertyName, Exception = ex.GetType().Name }),
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [EventHandler]
    public async Task HandleExecuteDeviceActionAsync(ExecuteDeviceActionEvent @event)
    {
        Logger.LogInformation("Executing device action: {ActionName}", @event.ActionName);
        
        if (DeviceConnection == null)
        {
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "DEVICE_NOT_CONNECTED",
                ErrorMessage = "Device not connected",
                Timestamp = DateTime.UtcNow
            });
            return;
        }

        try
        {
            // Deserialize parameters
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(@event.ParametersJson) 
                ?? new Dictionary<string, object>();
            
            var result = await DeviceConnection.ExecuteActionAsync(@event.ActionName, parameters);
            
            // Publish action execution result event
            await PublishAsync(new DeviceActionExecutionResultEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ActionName = @event.ActionName,
                Success = result.IsSuccess,
                Result = result.Result,
                ErrorMessage = result.ErrorMessage,
                ExecutionTimeMs = result.ExecutionTimeMs,
                Timestamp = DateTime.UtcNow
            });
            
            Logger.LogInformation("Device action execution completed: {ActionName}, success: {Success}, duration: {ExecutionTime}ms", 
                @event.ActionName, result.IsSuccess, result.ExecutionTimeMs);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to execute device action: {ActionName}", @event.ActionName);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "ACTION_EXECUTION_EXCEPTION",
                ErrorMessage = $"Action execution exception: {ex.Message}",
                ErrorDetailsJson = JsonSerializer.Serialize(new { ActionName = @event.ActionName, Exception = ex.GetType().Name }),
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [EventHandler]
    public async Task HandleGetDeviceStatusAsync(GetDeviceStatusEvent @event)
    {
        Logger.LogInformation("Getting device status: {DeviceId}", @event.DeviceId);
        
        var statusSummary = await GetDeviceStatusSummaryAsync();
        
        // Publish device status response event
        await PublishAsync(new DeviceStatusResponseEvent
        {
            DeviceId = @event.DeviceId,
            DeviceName = @event.DeviceName,
            StatusSummary = statusSummary,
            Timestamp = DateTime.UtcNow
        });
    }

    [EventHandler]
    public async Task HandleConnectDeviceAsync(ConnectDeviceEvent @event)
    {
        Logger.LogInformation("Connecting device: {DeviceId}", @event.DeviceId);
        
        if (DeviceConnection == null)
        {
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "DEVICE_NOT_INITIALIZED",
                ErrorMessage = "Device not initialized",
                Timestamp = DateTime.UtcNow
            });
            return;
        }

        try
        {
            var connected = await DeviceConnection.ConnectAsync();
            
            await PublishAsync(new DeviceConnectionResultEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                Success = connected,
                Action = "Connect",
                Timestamp = DateTime.UtcNow
            });
            
            Logger.LogInformation("Device connection result: {DeviceId}, success: {Success}", 
                @event.DeviceId, connected);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to connect device: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "CONNECT_EXCEPTION",
                ErrorMessage = $"Connection exception: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    [EventHandler]
    public async Task HandleDisconnectDeviceAsync(DisconnectDeviceEvent @event)
    {
        Logger.LogInformation("Disconnecting device: {DeviceId}", @event.DeviceId);
        
        if (DeviceConnection == null)
        {
            await PublishAsync(new DeviceConnectionResultEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                Success = true, // Already disconnected, consider it successful
                Action = "Disconnect",
                Timestamp = DateTime.UtcNow
            });
            return;
        }

        try
        {
            await DeviceConnection.DisconnectAsync();
            
            await PublishAsync(new DeviceConnectionResultEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                Success = true,
                Action = "Disconnect",
                Timestamp = DateTime.UtcNow
            });
            
            Logger.LogInformation("Device disconnected successfully: {DeviceId}", @event.DeviceId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to disconnect device: {DeviceId}", @event.DeviceId);
            
            await PublishAsync(new DeviceErrorEvent
            {
                DeviceId = @event.DeviceId,
                DeviceName = @event.DeviceName,
                ErrorCode = "DISCONNECT_EXCEPTION",
                ErrorMessage = $"Disconnect exception: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    #endregion

    #region State Transitions

    protected override void AIGAgentTransitionState(DeviceGAgentState state, StateLogEventBase<DeviceGAgentStateLogEvent> @event)
    {
        switch (@event)
        {
            case DeviceConnectionInitializedLogEvent e:
                state.ConnectionConfig = e.Config;
                state.DeviceId = e.DeviceId;
                state.DeviceName = e.DeviceName;
                state.DeviceType = e.DeviceType;
                state.IsConnected = true;
                state.ConnectedAt = e.ConnectedAt;
                state.LastStatusUpdate = DateTime.UtcNow;
                break;
                
            case DeviceMonitoringStateChangedLogEvent e:
                state.MonitoringEnabled = e.Enabled;
                state.MonitoringStateChangedAt = e.ChangedAt;
                break;
                
            case DeviceConnectionStatusChangedLogEvent e:
                state.ConnectionStatus = e.NewStatus;
                state.IsConnected = e.NewStatus == DeviceConnectionStatus.Connected;
                state.LastStatusUpdate = e.ChangedAt;
                if (!string.IsNullOrEmpty(e.Reason))
                {
                    state.LastErrorMessage = e.Reason;
                }
                break;
                
            case DevicePropertyChangedLogEvent e:
                // Update property change history
                state.PropertyChangeHistory.Add(new DevicePropertyChangeRecord
                {
                    PropertyName = e.PropertyName,
                    OldValue = e.OldValue,
                    NewValue = e.NewValue,
                    ChangedAt = e.ChangedAt
                });
                
                // Keep history records under 1000
                if (state.PropertyChangeHistory.Count > 1000)
                {
                    state.PropertyChangeHistory.RemoveRange(0, state.PropertyChangeHistory.Count - 1000);
                }
                
                state.LastPropertyChangeAt = e.ChangedAt;
                break;
        }
    }

    #endregion

    #region Private Methods

    private async Task TryReconnectDeviceAsync()
    {
        if (State.ConnectionConfig == null) return;
        
        try
        {
            Logger.LogInformation("Attempting to reconnect device: {DeviceId}", State.DeviceId);
            await InitializeDeviceConnectionAsync(State.ConnectionConfig);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to reconnect device: {DeviceId}", State.DeviceId);
        }
    }

    private async Task PerformDeviceMonitoringAsync(CancellationToken cancellationToken)
    {
        if (DeviceConnection == null) return;

        try
        {
            // Check device health status
            var healthStatus = await DeviceConnection.GetHealthStatusAsync(cancellationToken);
            
            if (!healthStatus.IsHealthy)
            {
                Logger.LogWarning("Device health check failed: {DeviceId}, status: {Status}", 
                    DeviceConnection.DeviceId, healthStatus.StatusDescription);
                
                await PublishAsync(new DeviceWarningEvent
                {
                    DeviceId = DeviceConnection.DeviceId,
                    DeviceName = DeviceConnection.DeviceName,
                    WarningCode = "HEALTH_CHECK_FAILED",
                    WarningMessage = healthStatus.StatusDescription,
                    WarningDetailsJson = JsonSerializer.Serialize(healthStatus.Details),
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during device monitoring check: {DeviceId}", DeviceConnection.DeviceId);
        }
    }

    private void OnDevicePropertyChanged(object? sender, DevicePropertyChangedEventArgs e)
    {
        // Record property change
        RaiseEvent(new DevicePropertyChangedLogEvent
        {
            PropertyName = e.PropertyName,
            OldValue = e.OldValue,
            NewValue = e.NewValue,
            ChangedAt = e.ChangedAt
        });
        
        // Publish property change notification event
        _ = Task.Run(async () =>
        {
            try
            {
                await PublishAsync(new DevicePropertyChangedNotificationEvent
                {
                    DeviceId = DeviceConnection?.DeviceId ?? State.DeviceId,
                    DeviceName = DeviceConnection?.DeviceName ?? State.DeviceName,
                    PropertyName = e.PropertyName,
                    OldValueJson = JsonSerializer.Serialize(e.OldValue),
                    NewValueJson = JsonSerializer.Serialize(e.NewValue),
                    Timestamp = e.ChangedAt
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to publish device property change event");
            }
        });
    }

    private void OnDeviceConnectionStatusChanged(object? sender, DeviceConnectionStatusChangedEventArgs e)
    {
        // Record connection status change
        RaiseEvent(new DeviceConnectionStatusChangedLogEvent
        {
            OldStatus = e.OldStatus,
            NewStatus = e.NewStatus,
            ChangedAt = e.ChangedAt,
            Reason = e.Reason ?? string.Empty
        });
        
        // Publish connection status change notification event
        _ = Task.Run(async () =>
        {
            try
            {
                await PublishAsync(new DeviceConnectionStatusChangedNotificationEvent
                {
                    DeviceId = DeviceConnection?.DeviceId ?? State.DeviceId,
                    DeviceName = DeviceConnection?.DeviceName ?? State.DeviceName,
                    OldStatus = e.OldStatus,
                    NewStatus = e.NewStatus,
                    Reason = e.Reason ?? string.Empty,
                    Timestamp = e.ChangedAt
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to publish device connection status change event");
            }
        });
    }

    private async Task RegisterDeviceActionsAsKernelFunctionsAsync()
    {
        if (DeviceConnection == null) return;
        
        try
        {
            var kernel = GetKernelFromBrain();
            if (kernel == null)
            {
                Logger.LogWarning("Cannot get Semantic Kernel instance, skipping device action registration");
                return;
            }

            var deviceActions = DeviceConnection.SupportedActions;
            var functions = new List<KernelFunction>();
            
            foreach (var action in deviceActions)
            {
                var function = CreateKernelFunctionForDeviceAction(action.Key, action.Value);
                if (function != null)
                {
                    functions.Add(function);
                }
            }
            
            // Add device property read/write functions
            functions.AddRange(CreateKernelFunctionsForDeviceProperties());
            
            if (functions.Count > 0)
            {
                var pluginName = $"Device_{DeviceConnection.DeviceId.Replace("-", "_")}";
                kernel.Plugins.AddFromFunctions(pluginName, functions);
                
                Logger.LogInformation("Registered {Count} device actions as Semantic Kernel functions, plugin name: {PluginName}", 
                    functions.Count, pluginName);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred while registering device actions as Semantic Kernel functions");
        }
    }

    private KernelFunction? CreateKernelFunctionForDeviceAction(string actionName, DeviceAction action)
    {
        try
        {
            return KernelFunctionFactory.CreateFromMethod(
                async (Dictionary<string, object> parameters) =>
                {
                    if (DeviceConnection == null)
                        return "Device not connected";
                    
                    var result = await DeviceConnection.ExecuteActionAsync(actionName, parameters);
                    return result.IsSuccess ? 
                        JsonSerializer.Serialize(result.Result) : 
                        $"Operation failed: {result.ErrorMessage}";
                },
                functionName: $"Execute{actionName}",
                description: action.Description
            );
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to create Kernel function for device action {ActionName}", actionName);
            return null;
        }
    }

    private IEnumerable<KernelFunction> CreateKernelFunctionsForDeviceProperties()
    {
        var functions = new List<KernelFunction>();
        
        if (DeviceConnection == null) return functions;
        
        try
        {
            // Read property function
            var readPropertyFunction = KernelFunctionFactory.CreateFromMethod(
                async (string propertyName) =>
                {
                    if (DeviceConnection == null)
                        return "Device not connected";
                    
                    try
                    {
                        var value = await DeviceConnection.ReadPropertyAsync(propertyName);
                        return JsonSerializer.Serialize(value);
                    }
                    catch (Exception ex)
                    {
                        return $"Failed to read property: {ex.Message}";
                    }
                },
                functionName: "ReadProperty",
                description: "Read device property value"
            );
            functions.Add(readPropertyFunction);
            
            // Write property function
            var writePropertyFunction = KernelFunctionFactory.CreateFromMethod(
                async (string propertyName, string valueJson) =>
                {
                    if (DeviceConnection == null)
                        return "Device not connected";
                    
                    try
                    {
                        var value = JsonSerializer.Deserialize<object>(valueJson);
                        var success = await DeviceConnection.WritePropertyAsync(propertyName, value!);
                        return success ? "Write successful" : "Write failed";
                    }
                    catch (Exception ex)
                    {
                        return $"Failed to write property: {ex.Message}";
                    }
                },
                functionName: "WriteProperty",
                description: "Write device property value"
            );
            functions.Add(writePropertyFunction);
            
            // Get device status function
            var getStatusFunction = KernelFunctionFactory.CreateFromMethod(
                async () =>
                {
                    var status = await GetDeviceStatusSummaryAsync();
                    return JsonSerializer.Serialize(status);
                },
                functionName: "GetStatus",
                description: "Get current device status"
            );
            functions.Add(getStatusFunction);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to create device property related Kernel functions");
        }
        
        return functions;
    }

    #endregion
}
