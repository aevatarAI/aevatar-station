using Aevatar.Core.Abstractions.Plugin;

namespace PluginAgentExample;

/// <summary>
/// Example weather agent plugin that demonstrates version-independent agent development
/// This plugin has ZERO dependencies on Orleans or GAgentBase
/// </summary>
[AgentPlugin("WeatherAgent", "1.0.0", Description = "Weather monitoring and forecasting agent")]
[AgentConfiguration(EnableMetrics = true, EnableHealthChecks = true)]
public class WeatherAgentPlugin : AgentPluginBase
{
    private WeatherState _weatherState = new();
    private readonly List<WeatherAlert> _alerts = new();

    public override AgentPluginMetadata Metadata { get; protected set; } = 
        new("WeatherAgent", "1.0.0", "Weather monitoring and forecasting agent", 
            new Dictionary<string, object>
            {
                ["SupportedLocations"] = new[] { "New York", "London", "Tokyo", "Sydney" },
                ["UpdateInterval"] = "5 minutes",
                ["Features"] = new[] { "Current Weather", "Forecasts", "Alerts", "Historical Data" }
            });

    [AgentInject]
    public IAgentContext AgentContext { get; set; } = null!;

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger?.LogInformation("WeatherAgent initializing...");
        
        // Initialize weather state
        _weatherState = new WeatherState
        {
            Location = Context?.Configuration.GetValueOrDefault("Location", "Unknown")?.ToString() ?? "Unknown",
            LastUpdated = DateTime.UtcNow,
            Temperature = 20.0m,
            Humidity = 65,
            Conditions = "Partly Cloudy"
        };

        Logger?.LogInformation($"WeatherAgent initialized for location: {_weatherState.Location}");
        await Task.CompletedTask;
    }

    #region Public Agent Methods (Exposed to Orleans)

    [AgentMethod("GetCurrentWeather", IsReadOnly = true)]
    public async Task<WeatherInfo> GetCurrentWeatherAsync()
    {
        Logger?.LogDebug($"Getting current weather for {_weatherState.Location}");
        
        await SimulateWeatherDataFetch();
        
        return new WeatherInfo
        {
            Location = _weatherState.Location,
            Temperature = _weatherState.Temperature,
            Humidity = _weatherState.Humidity,
            Conditions = _weatherState.Conditions,
            LastUpdated = _weatherState.LastUpdated,
            Alerts = _alerts.Where(a => a.IsActive).ToList()
        };
    }

    [AgentMethod("GetForecast", IsReadOnly = true)]
    public async Task<List<WeatherForecast>> GetForecastAsync(int days = 5)
    {
        Logger?.LogDebug($"Getting {days} day forecast for {_weatherState.Location}");
        
        await SimulateWeatherDataFetch();
        
        var forecasts = new List<WeatherForecast>();
        var baseDate = DateTime.UtcNow.Date.AddDays(1);
        
        for (int i = 0; i < Math.Min(days, 10); i++)
        {
            forecasts.Add(new WeatherForecast
            {
                Date = baseDate.AddDays(i),
                HighTemperature = _weatherState.Temperature + Random.Shared.Next(-5, 8),
                LowTemperature = _weatherState.Temperature + Random.Shared.Next(-10, 3),
                Conditions = GetRandomConditions(),
                ChanceOfRain = Random.Shared.Next(0, 101)
            });
        }
        
        return forecasts;
    }

    [AgentMethod("UpdateLocation")]
    public async Task<bool> UpdateLocationAsync(string newLocation)
    {
        Logger?.LogInformation($"Updating location from {_weatherState.Location} to {newLocation}");
        
        if (string.IsNullOrWhiteSpace(newLocation))
        {
            Logger?.LogWarning($"Invalid location provided: {newLocation}");
            return false;
        }

        var oldLocation = _weatherState.Location;
        _weatherState.Location = newLocation;
        _weatherState.LastUpdated = DateTime.UtcNow;
        
        // Publish location change event
        await PublishEventAsync("LocationChanged", new LocationChangedData
        {
            OldLocation = oldLocation,
            NewLocation = newLocation,
            UpdatedAt = DateTime.UtcNow
        });
        
        return true;
    }

    [AgentMethod("CreateAlert")]
    public async Task<string> CreateAlertAsync(string alertType, string message, int durationMinutes = 60)
    {
        var alertId = Guid.NewGuid().ToString();
        var alert = new WeatherAlert
        {
            Id = alertId,
            Type = alertType,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(durationMinutes),
            IsActive = true
        };
        
        _alerts.Add(alert);
        Logger?.LogInformation($"Created weather alert: {alertType} - {message}");
        
        // Publish alert event
        await PublishEventAsync("AlertCreated", alert);
        
        return alertId;
    }

    [AgentMethod("GetAlerts", IsReadOnly = true)]
    public async Task<List<WeatherAlert>> GetAlertsAsync(bool activeOnly = true)
    {
        await Task.Delay(1); // Simulate async operation
        
        return activeOnly 
            ? _alerts.Where(a => a.IsActive && a.ExpiresAt > DateTime.UtcNow).ToList()
            : _alerts.ToList();
    }

    [AgentMethod("DismissAlert")]
    public async Task<bool> DismissAlertAsync(string alertId)
    {
        var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
        if (alert == null)
        {
            Logger?.LogWarning($"Alert not found: {alertId}");
            return false;
        }
        
        alert.IsActive = false;
        Logger?.LogInformation($"Dismissed alert: {alertId}");
        
        await PublishEventAsync("AlertDismissed", new { AlertId = alertId, DismissedAt = DateTime.UtcNow });
        
        return true;
    }

    [AgentMethod("StartWeatherMonitoring", AlwaysInterleave = true)]
    public async Task StartWeatherMonitoringAsync(int intervalMinutes = 5)
    {
        Logger?.LogInformation($"Starting weather monitoring with {intervalMinutes} minute intervals");
        
        await PublishEventAsync("MonitoringStarted", new
        {
            Location = _weatherState.Location,
            IntervalMinutes = intervalMinutes,
            StartedAt = DateTime.UtcNow
        });
    }

    [AgentMethod("RequestWeatherFromAgent")]
    public async Task<WeatherInfo?> RequestWeatherFromAgentAsync(string targetAgentId)
    {
        if (Context == null)
        {
            Logger?.LogWarning("Context not available for inter-agent communication");
            return null;
        }

        try
        {
            Logger?.LogInformation($"Requesting weather data from agent: {targetAgentId}");
            
            // Get reference to target agent
            var targetAgent = await Context.GetAgentAsync(targetAgentId);
            
            // Call method on target agent
            var weather = await targetAgent.CallMethodAsync<WeatherInfo>("GetCurrentWeather");
            
            Logger?.LogInformation($"Received weather data from {targetAgentId}: {weather?.Location} - {weather?.Temperature}°C");
            
            return weather;
        }
        catch (Exception ex)
        {
            Logger?.LogError($"Failed to get weather from agent {targetAgentId}: {ex.Message}", ex);
            return null;
        }
    }

    [AgentMethod("SendAlertToAgent")]
    public async Task<bool> SendAlertToAgentAsync(string targetAgentId, string alertType, string message)
    {
        if (Context == null)
        {
            Logger?.LogWarning("Context not available for inter-agent communication");
            return false;
        }

        try
        {
            Logger?.LogInformation($"Sending alert to agent: {targetAgentId}");
            
            // Get reference to target agent
            var targetAgent = await Context.GetAgentAsync(targetAgentId);
            
            // Send event to target agent
            var alertEvent = new AgentEvent
            {
                EventType = "ExternalWeatherAlert",
                Data = new ExternalAlertData
                {
                    Type = alertType,
                    Message = message,
                    DurationMinutes = 30
                },
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString(),
                SourceAgentId = Context.AgentId
            };
            
            await targetAgent.SendEventAsync(alertEvent);
            
            Logger?.LogInformation($"Alert sent successfully to {targetAgentId}");
            return true;
        }
        catch (Exception ex)
        {
            Logger?.LogError($"Failed to send alert to agent {targetAgentId}: {ex.Message}", ex);
            return false;
        }
    }

    [AgentMethod("SyncDataWithAgents")]
    public async Task<List<WeatherSyncResult>> SyncDataWithAgentsAsync(string[] agentIds)
    {
        var results = new List<WeatherSyncResult>();
        
        if (Context == null)
        {
            Logger?.LogWarning("Context not available for multi-agent sync");
            return results;
        }

        Logger?.LogInformation($"Starting data sync with {agentIds.Length} agents");

        foreach (var agentId in agentIds)
        {
            var syncResult = new WeatherSyncResult { AgentId = agentId };
            
            try
            {
                var targetAgent = await Context.GetAgentAsync(agentId);
                
                // Get their weather data
                var theirWeather = await targetAgent.CallMethodAsync<WeatherInfo>("GetCurrentWeather");
                
                // Send our data to them
                await targetAgent.SendEventAsync(new AgentEvent
                {
                    EventType = "WeatherDataSync",
                    Data = new
                    {
                        SourceLocation = _weatherState.Location,
                        Temperature = _weatherState.Temperature,
                        Conditions = _weatherState.Conditions,
                        SyncedAt = DateTime.UtcNow
                    },
                    Timestamp = DateTime.UtcNow,
                    SourceAgentId = Context.AgentId
                });
                
                syncResult.Success = true;
                syncResult.RemoteWeather = theirWeather;
                
                Logger?.LogInformation($"Synced successfully with {agentId}");
            }
            catch (Exception ex)
            {
                syncResult.Success = false;
                syncResult.ErrorMessage = ex.Message;
                Logger?.LogWarning($"Failed to sync with {agentId}: {ex.Message}");
            }
            
            results.Add(syncResult);
        }

        return results;
    }

    [AgentMethod("GetHealthStatus", IsReadOnly = true)]
    public async Task<AgentHealthInfo> GetHealthStatusAsync()
    {
        await Task.Delay(1);
        
        var isHealthy = _weatherState.LastUpdated > DateTime.UtcNow.AddMinutes(-10);
        
        return new AgentHealthInfo
        {
            IsHealthy = isHealthy,
            Status = isHealthy ? "Healthy" : "Stale Data",
            LastDataUpdate = _weatherState.LastUpdated,
            ActiveAlerts = _alerts.Count(a => a.IsActive),
            Location = _weatherState.Location
        };
    }

    #endregion

    #region Event Handlers

    [AgentEventHandler("WeatherUpdateRequest")]
    public async Task HandleWeatherUpdateRequestAsync(IAgentEvent agentEvent)
    {
        Logger?.LogDebug("Handling weather update request");
        
        await SimulateWeatherDataFetch();
        
        // Publish updated weather data
        await PublishEventAsync("WeatherUpdated", new
        {
            Location = _weatherState.Location,
            Temperature = _weatherState.Temperature,
            Conditions = _weatherState.Conditions,
            UpdatedAt = _weatherState.LastUpdated
        });
    }

    [AgentEventHandler("ExternalWeatherAlert")]
    public async Task HandleExternalWeatherAlertAsync(IAgentEvent agentEvent)
    {
        if (agentEvent.Data is ExternalAlertData alertData)
        {
            Logger?.LogInformation($"Received external weather alert: {alertData.Type}");
            
            await CreateAlertAsync(alertData.Type, alertData.Message, alertData.DurationMinutes);
        }
    }

    [AgentEventHandler("WeatherDataSync")]
    public async Task HandleWeatherDataSyncAsync(IAgentEvent agentEvent)
    {
        Logger?.LogInformation($"Received weather data sync from agent: {agentEvent.SourceAgentId}");
        
        // Process the incoming sync data
        // In a real implementation, you might update local forecasts, 
        // compare data for accuracy, or trigger other actions
        
        // Log the received data for demonstration
        if (agentEvent.Data != null)
        {
            try
            {
                var syncData = System.Text.Json.JsonSerializer.Serialize(agentEvent.Data);
                Logger?.LogDebug($"Sync data received: {syncData}");
                
                // You could update local state, trigger alerts, etc.
                await CreateAlertAsync("Data Sync", 
                    $"Received weather sync from {agentEvent.SourceAgentId}", 15);
            }
            catch (Exception ex)
            {
                Logger?.LogWarning($"Failed to process sync data: {ex.Message}");
            }
        }
    }

    [AgentEventHandler] // Generic event handler
    public async Task HandleGenericEventAsync(IAgentEvent agentEvent)
    {
        Logger?.LogDebug($"Received generic event: {agentEvent.EventType}");
        
        // Log all unhandled events for debugging
        await Task.Delay(1);
    }

    #endregion

    #region State Management

    public override async Task<object?> GetStateAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        return new
        {
            WeatherState = _weatherState,
            Alerts = _alerts,
            LastUpdated = DateTime.UtcNow
        };
    }

    public override async Task SetStateAsync(object? state, CancellationToken cancellationToken = default)
    {
        if (state is WeatherState weatherState)
        {
            _weatherState = weatherState;
            Logger?.LogDebug("Weather state updated");
        }
        
        await Task.Delay(1, cancellationToken);
    }

    #endregion

    #region Private Helper Methods

    private async Task SimulateWeatherDataFetch()
    {
        // Simulate API call delay
        await Task.Delay(Random.Shared.Next(50, 200));
        
        // Simulate weather data update
        _weatherState.Temperature += (decimal)(Random.Shared.NextDouble() - 0.5) * 2;
        _weatherState.Humidity = Math.Max(0, Math.Min(100, _weatherState.Humidity + Random.Shared.Next(-5, 6)));
        _weatherState.LastUpdated = DateTime.UtcNow;
        
        // Randomly update conditions
        if (Random.Shared.Next(1, 11) == 1) // 10% chance
        {
            _weatherState.Conditions = GetRandomConditions();
        }
        
        // Check for alert conditions
        await CheckForAlertConditions();
    }

    private async Task CheckForAlertConditions()
    {
        // Check for extreme temperature
        if (_weatherState.Temperature > 35)
        {
            await CreateAlertAsync("Heat Warning", 
                $"High temperature alert: {_weatherState.Temperature:F1}°C in {_weatherState.Location}", 30);
        }
        else if (_weatherState.Temperature < -10)
        {
            await CreateAlertAsync("Cold Warning", 
                $"Low temperature alert: {_weatherState.Temperature:F1}°C in {_weatherState.Location}", 30);
        }
        
        // Check for high humidity
        if (_weatherState.Humidity > 90)
        {
            await CreateAlertAsync("Humidity Alert", 
                $"High humidity: {_weatherState.Humidity}% in {_weatherState.Location}", 15);
        }
    }

    private string GetRandomConditions()
    {
        var conditions = new[] 
        { 
            "Sunny", "Partly Cloudy", "Cloudy", "Overcast", 
            "Light Rain", "Rain", "Heavy Rain", "Thunderstorms",
            "Snow", "Fog", "Windy", "Clear"
        };
        
        return conditions[Random.Shared.Next(conditions.Length)];
    }

    #endregion

    protected override async Task OnDisposeAsync()
    {
        Logger?.LogInformation("WeatherAgent disposing...");
        
        // Clean up any resources
        _alerts.Clear();
        
        await Task.CompletedTask;
    }
}

#region Data Models

public class WeatherState
{
    public string Location { get; set; } = string.Empty;
    public decimal Temperature { get; set; }
    public int Humidity { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public class WeatherInfo
{
    public string Location { get; set; } = string.Empty;
    public decimal Temperature { get; set; }
    public int Humidity { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public List<WeatherAlert> Alerts { get; set; } = new();
}

public class WeatherForecast
{
    public DateTime Date { get; set; }
    public decimal HighTemperature { get; set; }
    public decimal LowTemperature { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public int ChanceOfRain { get; set; }
}

public class WeatherAlert
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}

public class LocationChangedData
{
    public string OldLocation { get; set; } = string.Empty;
    public string NewLocation { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}

public class ExternalAlertData
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 60;
}

public class AgentHealthInfo
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastDataUpdate { get; set; }
    public int ActiveAlerts { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class WeatherSyncResult
{
    public string AgentId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public WeatherInfo? RemoteWeather { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion