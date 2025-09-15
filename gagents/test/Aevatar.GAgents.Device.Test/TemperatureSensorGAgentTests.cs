using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Device.Abstractions;
using Aevatar.GAgents.Device.Examples;
using Aevatar.GAgents.TestBase;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Aevatar.GAgents.Device.Test;

[Collection(ClusterCollection.Name)]
public sealed class TemperatureSensorGAgentTests : AevatarDeviceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public TemperatureSensorGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task TemperatureSensorGAgent_InitializeConnection_ShouldConnectSuccessfully()
    {
        // Arrange
        var config = CreateTemperatureSensorConfig("test-temp-001");
        var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());

        // Act
        var connected = await tempSensor.InitializeDeviceConnectionAsync(config);

        // Assert
        connected.ShouldBeTrue();
        
        var deviceConnection = await tempSensor.GetDeviceConnectionInfoAsync();
        deviceConnection.ShouldNotBeNull();
        deviceConnection.DeviceId.ShouldBe("test-temp-001");
        deviceConnection.DeviceName.ShouldBe("Test Temperature Sensor");
        deviceConnection.DeviceType.ShouldBe("TemperatureSensor");

        _testOutputHelper.WriteLine($"Temperature sensor connected: {deviceConnection.DeviceId}");
    }

    [Fact]
    public async Task TemperatureSensorGAgent_GetTemperature_ShouldReturnValidValue()
    {
        // Arrange
        var config = CreateTemperatureSensorConfig("test-temp-002");
        var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
        await tempSensor.InitializeDeviceConnectionAsync(config);

        // Act
        var temperature = await tempSensor.GetTemperatureAsync();

        // Assert
        temperature.ShouldNotBe(double.NaN);
        temperature.ShouldBe(22.5); // Default value from config
        _testOutputHelper.WriteLine($"Temperature reading: {temperature:F1}°C");
    }

    [Fact]
    public async Task TemperatureSensorGAgent_GetHumidity_ShouldReturnValidValue()
    {
        // Arrange
        var config = CreateTemperatureSensorConfig("test-temp-003");
        var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
        await tempSensor.InitializeDeviceConnectionAsync(config);

        // Act
        var humidity = await tempSensor.GetHumidityAsync();

        // Assert
        humidity.ShouldNotBe(double.NaN);
        humidity.ShouldBe(45.0); // Default value from config
        _testOutputHelper.WriteLine($"Humidity reading: {humidity:F1}%");
    }

    [Fact]
    public async Task TemperatureSensorGAgent_GetReading_ShouldReturnCompleteData()
    {
        // Arrange
        var config = CreateTemperatureSensorConfig("test-temp-004");
        var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
        await tempSensor.InitializeDeviceConnectionAsync(config);

        // Act
        var reading = await tempSensor.GetReadingAsync();

        // Assert
        reading.ShouldNotBeNull();
        reading.Temperature.ShouldBe(22.5);
        reading.Humidity.ShouldBe(45.0);
        reading.Status.ShouldBe("Normal");
        reading.TemperatureLevel.ShouldBe("Comfortable");
        reading.HumidityLevel.ShouldBe("Moderate");
        reading.ComfortLevel.ShouldBe("Comfortable");
        reading.Timestamp.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));

        _testOutputHelper.WriteLine($"Complete reading: {reading.Temperature:F1}°C ({reading.TemperatureLevel}), " +
                                  $"{reading.Humidity:F1}% ({reading.HumidityLevel}), Comfort: {reading.ComfortLevel}");
    }

    [Fact]
    public async Task TemperatureSensorGAgent_TemperatureLevelClassification_ShouldBeCorrect()
    {
        // Test different temperature levels
        var testCases = new[]
        {
            (-5.0, "Freezing"),
            (5.0, "Cold"),
            (15.0, "Cool"),
            (22.5, "Comfortable"),
            (27.0, "Warm"),
            (32.0, "Hot"),
            (40.0, "Very Hot")
        };

        foreach (var (temp, expectedLevel) in testCases)
        {
            // Arrange
            var config = CreateTemperatureSensorConfig($"test-temp-level-{temp}");
            config.ExtendedProperties["Temperature"] = temp.ToString();
            var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
            await tempSensor.InitializeDeviceConnectionAsync(config);

            // Act
            var reading = await tempSensor.GetReadingAsync();

            // Assert
            reading.Temperature.ShouldBe(temp);
            reading.TemperatureLevel.ShouldBe(expectedLevel);
            
            _testOutputHelper.WriteLine($"Temperature {temp:F1}°C classified as: {expectedLevel}");
        }
    }

    [Fact]
    public async Task TemperatureSensorGAgent_HumidityLevelClassification_ShouldBeCorrect()
    {
        // Test different humidity levels
        var testCases = new[]
        {
            (25.0, "Dry"),
            (35.0, "Slightly Dry"),
            (50.0, "Moderate"),
            (65.0, "Slightly Humid"),
            (80.0, "Humid")
        };

        foreach (var (humidity, expectedLevel) in testCases)
        {
            // Arrange
            var config = CreateTemperatureSensorConfig($"test-temp-humidity-{humidity}");
            config.ExtendedProperties["Humidity"] = humidity.ToString();
            var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
            await tempSensor.InitializeDeviceConnectionAsync(config);

            // Act
            var reading = await tempSensor.GetReadingAsync();

            // Assert
            reading.Humidity.ShouldBe(humidity);
            reading.HumidityLevel.ShouldBe(expectedLevel);
            
            _testOutputHelper.WriteLine($"Humidity {humidity:F1}% classified as: {expectedLevel}");
        }
    }

    [Fact]
    public async Task TemperatureSensorGAgent_ComfortLevelClassification_ShouldBeCorrect()
    {
        // Test different comfort levels
        var testCases = new[]
        {
            (23.0, 50.0, "Comfortable"), // Ideal conditions
            (25.0, 65.0, "Fairly Comfortable"), // Acceptable conditions
            (18.0, 30.0, "Fairly Comfortable"), // Acceptable conditions
            (35.0, 80.0, "Uncomfortable"), // Too hot and humid
            (10.0, 20.0, "Uncomfortable") // Too cold and dry
        };

        foreach (var (temp, humidity, expectedComfort) in testCases)
        {
            // Arrange
            var config = CreateTemperatureSensorConfig($"test-comfort-{temp}-{humidity}");
            config.ExtendedProperties["Temperature"] = temp.ToString();
            config.ExtendedProperties["Humidity"] = humidity.ToString();
            var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
            await tempSensor.InitializeDeviceConnectionAsync(config);

            // Act
            var reading = await tempSensor.GetReadingAsync();

            // Assert
            reading.Temperature.ShouldBe(temp);
            reading.Humidity.ShouldBe(humidity);
            reading.ComfortLevel.ShouldBe(expectedComfort);
            
            _testOutputHelper.WriteLine($"Conditions {temp:F1}°C, {humidity:F1}% = {expectedComfort}");
        }
    }

    [Fact]
    public async Task TemperatureSensorGAgent_Calibrate_ShouldSucceed()
    {
        // Arrange
        var config = CreateTemperatureSensorConfig("test-temp-005");
        var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
        await tempSensor.InitializeDeviceConnectionAsync(config);

        // Act
        var calibrationResult = await tempSensor.CalibrateAsync();

        // Assert
        calibrationResult.ShouldBeTrue();
        _testOutputHelper.WriteLine("Sensor calibration completed successfully");
    }

    [Fact]
    public async Task TemperatureSensorGAgent_GetHistory_ShouldReturnEmptyInitially()
    {
        // Arrange
        var config = CreateTemperatureSensorConfig("test-temp-006");
        var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
        await tempSensor.InitializeDeviceConnectionAsync(config);

        // Act
        var history = await tempSensor.GetHistoryAsync(24);

        // Assert
        history.ShouldNotBeNull();
        history.Count.ShouldBe(0); // Initially empty
        _testOutputHelper.WriteLine($"Initial history count: {history.Count}");
    }

    [Fact]
    public async Task TemperatureSensorGAgent_GetDeviceStatusSummary_ShouldReturnCorrectInfo()
    {
        // Arrange
        var config = CreateTemperatureSensorConfig("test-temp-007");
        var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
        await tempSensor.InitializeDeviceConnectionAsync(config);

        // Act
        var statusSummary = await tempSensor.GetDeviceStatusSummaryAsync();
        
        // Assert
        statusSummary.ShouldNotBeNull();
        statusSummary.DeviceId.ShouldBe("test-temp-007");
        statusSummary.DeviceName.ShouldBe("Test Temperature Sensor");
        statusSummary.DeviceType.ShouldBe("TemperatureSensor");
        statusSummary.IsConnected.ShouldBeTrue();
        statusSummary.Status.ShouldBe(DeviceConnectionStatus.Connected);
        statusSummary.Properties.ShouldNotBeNull();
        statusSummary.Properties.Count.ShouldBeGreaterThan(0);
        
        _testOutputHelper.WriteLine($"Device Status: {statusSummary.DeviceId} - {statusSummary.Status}");
        _testOutputHelper.WriteLine($"Properties: {string.Join(", ", statusSummary.Properties.Select(p => $"{p.Key}={p.Value}"))}");
    }

    [Fact]
    public async Task TemperatureSensorGAgent_OperationsWithoutConnection_ShouldHandleGracefully()
    {
        // Arrange - Create GAgent but don't initialize connection
        var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());

        // Act & Assert - Operations should handle missing connection gracefully
        var temperature = await tempSensor.GetTemperatureAsync();
        temperature.ShouldBe(double.NaN);
        
        var humidity = await tempSensor.GetHumidityAsync();
        humidity.ShouldBe(double.NaN);
        
        var reading = await tempSensor.GetReadingAsync();
        reading.ShouldNotBeNull();
        reading.Status.ShouldBe("Error");
        
        var calibrationResult = await tempSensor.CalibrateAsync();
        calibrationResult.ShouldBeFalse();
        
        _testOutputHelper.WriteLine("Operations correctly handled missing device connection");
    }

    [Fact]
    public async Task TemperatureSensorGAgent_SetDeviceMonitoring_ShouldEnableDisable()
    {
        // Arrange
        var config = CreateTemperatureSensorConfig("test-temp-008");
        var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
        await tempSensor.InitializeDeviceConnectionAsync(config);

        // Act - Enable monitoring
        var enableResult = await tempSensor.SetDeviceMonitoringAsync(true);
        
        // Assert
        enableResult.ShouldBeTrue();
        _testOutputHelper.WriteLine("Device monitoring enabled");

        // Act - Disable monitoring
        var disableResult = await tempSensor.SetDeviceMonitoringAsync(false);
        
        // Assert
        disableResult.ShouldBeTrue();
        _testOutputHelper.WriteLine("Device monitoring disabled");
    }

    [Fact]
    public async Task TemperatureSensorGAgent_ConcurrentReadings_ShouldHandleCorrectly()
    {
        // Arrange
        var config = CreateTemperatureSensorConfig("test-temp-009");
        var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
        await tempSensor.InitializeDeviceConnectionAsync(config);

        // Act - Execute multiple readings concurrently
        var tasks = new List<Task>
        {
            Task.Run(async () => await tempSensor.GetTemperatureAsync()),
            Task.Run(async () => await tempSensor.GetHumidityAsync()),
            Task.Run(async () => await tempSensor.GetReadingAsync()),
            Task.Run(async () => await tempSensor.GetReadingAsync()),
            Task.Run(async () => await tempSensor.GetReadingAsync())
        };

        // Assert - All tasks should complete without exceptions
        await Task.WhenAll(tasks);
        
        // Verify final reading is still correct
        var finalReading = await tempSensor.GetReadingAsync();
        finalReading.Temperature.ShouldBe(22.5);
        finalReading.Humidity.ShouldBe(45.0);
        
        _testOutputHelper.WriteLine($"Concurrent readings completed - Final: {finalReading.Temperature:F1}°C, {finalReading.Humidity:F1}%");
    }

    [Fact]
    public async Task TemperatureSensorGAgent_MultipleInstances_ShouldWorkIndependently()
    {
        // Arrange - Create multiple sensor instances with different configs
        var config1 = CreateTemperatureSensorConfig("sensor-001");
        config1.ExtendedProperties["Temperature"] = "20.0";
        config1.ExtendedProperties["Humidity"] = "40.0";
        
        var config2 = CreateTemperatureSensorConfig("sensor-002");
        config2.ExtendedProperties["Temperature"] = "25.0";
        config2.ExtendedProperties["Humidity"] = "60.0";

        var sensor1 = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
        var sensor2 = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());

        // Act
        await sensor1.InitializeDeviceConnectionAsync(config1);
        await sensor2.InitializeDeviceConnectionAsync(config2);

        var reading1 = await sensor1.GetReadingAsync();
        var reading2 = await sensor2.GetReadingAsync();

        // Assert
        reading1.Temperature.ShouldBe(20.0);
        reading1.Humidity.ShouldBe(40.0);
        
        reading2.Temperature.ShouldBe(25.0);
        reading2.Humidity.ShouldBe(60.0);
        
        _testOutputHelper.WriteLine($"Sensor 1: {reading1.Temperature:F1}°C, {reading1.Humidity:F1}%");
        _testOutputHelper.WriteLine($"Sensor 2: {reading2.Temperature:F1}°C, {reading2.Humidity:F1}%");
    }

    [Fact]
    public async Task TemperatureSensorGAgent_GetDescription_ShouldIncludeCurrentReadings()
    {
        // Arrange
        var config = CreateTemperatureSensorConfig("test-temp-010");
        var tempSensor = await _gAgentFactory.GetGAgentAsync<ITemperatureSensorGAgent>(Guid.NewGuid());
        await tempSensor.InitializeDeviceConnectionAsync(config);

        // Act
        var description = await tempSensor.GetDescriptionAsync();

        // Assert
        description.ShouldNotBeNull();
        description.ShouldNotBeEmpty();
        description.ShouldContain("Temperature Sensor");
        description.ShouldContain("Connected");
        description.ShouldContain("22.5°C"); // Current temperature
        description.ShouldContain("45.0%");  // Current humidity
        description.ShouldContain("Comfortable"); // Temperature level
        description.ShouldContain("Moderate");    // Humidity level
        
        _testOutputHelper.WriteLine($"Description: {description}");
    }
}
