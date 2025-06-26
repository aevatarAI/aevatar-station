using System;

// ABOUTME: Weather information DTO for plugin usage
// ABOUTME: Contains weather data structure without Orleans dependencies

namespace ProxyGeneratorDemo.Plugins;

/// <summary>
/// Weather information data transfer object
/// Pure data class with no Orleans dependencies
/// </summary>
public class WeatherInfo
{
    public string City { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public decimal Temperature { get; set; }
    public int Humidity { get; set; }
    public decimal WindSpeed { get; set; }
    public DateTime LastUpdated { get; set; }

    public override string ToString()
    {
        return $"{City}: {Condition}, {Temperature}°C, Humidity: {Humidity}%, Wind: {WindSpeed} m/s (Updated: {LastUpdated:HH:mm})";
    }
}