// ABOUTME: Plugin implementation for weather service without Orleans dependencies
// ABOUTME: Demonstrates how plugins can implement complex business logic independently

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions.Plugin;

namespace ProxyGeneratorDemo.Plugins;

/// <summary>
/// Weather service plugin - NO Orleans dependencies!
/// Implements weather-related business logic as a pure plugin
/// </summary>
[AgentPlugin("WeatherService", "1.0.0")]
public class WeatherServicePlugin : AgentPluginBase
{
    private readonly ConcurrentDictionary<string, WeatherInfo> _weatherCache = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _monitors = new();
    private readonly Random _random = new();

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger?.LogInformation("Weather service plugin initialized");
        
        // Initialize with some default weather data
        _weatherCache["London"] = new WeatherInfo
        {
            City = "London",
            Condition = "Cloudy",
            Temperature = 15.5m,
            Humidity = 70,
            WindSpeed = 12.5m,
            LastUpdated = DateTime.UtcNow
        };
        
        _weatherCache["New York"] = new WeatherInfo
        {
            City = "New York",
            Condition = "Sunny",
            Temperature = 22.0m,
            Humidity = 45,
            WindSpeed = 8.0m,
            LastUpdated = DateTime.UtcNow
        };
        
        await Task.CompletedTask;
    }

    [AgentMethod("GetCurrentWeatherAsync", IsReadOnly = true)]
    public async Task<WeatherInfo> GetCurrentWeatherAsync(string city)
    {
        Logger?.LogDebug("Getting weather for {City}", city);
        
        if (_weatherCache.TryGetValue(city, out var weather))
        {
            return weather;
        }
        
        // Simulate fetching weather from external API
        await Task.Delay(100);
        
        var newWeather = GenerateRandomWeather(city);
        _weatherCache[city] = newWeather;
        
        return newWeather;
    }

    [AgentMethod("GetTemperatureAsync", IsReadOnly = true)]
    public async Task<decimal> GetTemperatureAsync(string city)
    {
        var weather = await GetCurrentWeatherAsync(city);
        return weather.Temperature;
    }

    [AgentMethod("UpdateWeatherAsync")]
    public async Task UpdateWeatherAsync(string city, WeatherInfo weather)
    {
        Logger?.LogInformation("Updating weather for {City}", city);
        
        weather.LastUpdated = DateTime.UtcNow;
        _weatherCache[city] = weather;
        
        // Publish weather update event
        await PublishEventAsync("WeatherUpdated", new { City = city, Weather = weather });
    }

    [AgentMethod("StartMonitoringAsync", AlwaysInterleave = true)]
    public async Task StartMonitoringAsync(string city, int intervalMinutes)
    {
        Logger?.LogInformation("Starting weather monitoring for {City} with interval {Interval} minutes", 
            city, intervalMinutes);
        
        // Cancel existing monitor if any
        if (_monitors.TryGetValue(city, out var existingCts))
        {
            existingCts.Cancel();
        }
        
        var cts = new CancellationTokenSource();
        _monitors[city] = cts;
        
        // Start monitoring task
        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), cts.Token);
                    
                    // Simulate weather change
                    var weather = await GetCurrentWeatherAsync(city);
                    weather.Temperature += (decimal)(_random.NextDouble() * 4 - 2); // -2 to +2 change
                    weather.Humidity = Math.Clamp(weather.Humidity + _random.Next(-5, 6), 0, 100);
                    weather.WindSpeed = Math.Max(0, weather.WindSpeed + (decimal)(_random.NextDouble() * 2 - 1));
                    
                    await UpdateWeatherAsync(city, weather);
                    
                    Logger?.LogDebug("Weather monitoring update for {City}: {Temperature}°C", 
                        city, weather.Temperature);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, cts.Token);
        
        await Task.CompletedTask;
    }

    [AgentMethod("StopMonitoringAsync")]
    public async Task StopMonitoringAsync(string city)
    {
        if (_monitors.TryRemove(city, out var cts))
        {
            Logger?.LogInformation("Stopping weather monitoring for {City}", city);
            cts.Cancel();
        }
        
        await Task.CompletedTask;
    }

    [AgentMethod("LogWeatherEventAsync", OneWay = true)]
    public async Task LogWeatherEventAsync(string eventMessage)
    {
        // Fire and forget logging
        Logger?.LogInformation("Weather Event: {EventMessage}", eventMessage);
        
        // Could also write to file, send to monitoring system, etc.
        await Task.CompletedTask;
    }

    [AgentMethod("GetMonitoredCitiesAsync", IsReadOnly = true)]
    public async Task<List<string>> GetMonitoredCitiesAsync()
    {
        var cities = _monitors.Keys.ToList();
        await Task.CompletedTask;
        return cities;
    }

    protected override async Task OnDisposeAsync()
    {
        // Cancel all monitors
        foreach (var cts in _monitors.Values)
        {
            cts.Cancel();
        }
        
        _monitors.Clear();
        await base.OnDisposeAsync();
    }

    private WeatherInfo GenerateRandomWeather(string city)
    {
        var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Partly Cloudy", "Foggy", "Windy" };
        
        return new WeatherInfo
        {
            City = city,
            Condition = conditions[_random.Next(conditions.Length)],
            Temperature = (decimal)(_random.NextDouble() * 30 + 5), // 5-35°C
            Humidity = _random.Next(30, 90),
            WindSpeed = (decimal)(_random.NextDouble() * 25), // 0-25 m/s
            LastUpdated = DateTime.UtcNow
        };
    }
}