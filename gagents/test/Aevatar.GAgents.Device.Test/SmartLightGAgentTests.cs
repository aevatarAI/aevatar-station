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
public sealed class SmartLightGAgentTests : AevatarDeviceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public SmartLightGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task SmartLightGAgent_InitializeConnection_ShouldConnectSuccessfully()
    {
        // Arrange
        var config = CreateSmartLightConfig("test-light-001");
        var smartLight = await _gAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());

        // Act
        var connected = await smartLight.InitializeDeviceConnectionAsync(config);

        // Assert
        connected.ShouldBeTrue();
        
        var deviceConnection = await smartLight.GetDeviceConnectionInfoAsync();
        deviceConnection.ShouldNotBeNull();
        deviceConnection.DeviceId.ShouldBe("test-light-001");
        deviceConnection.DeviceName.ShouldBe("Test Smart Light");
        deviceConnection.DeviceType.ShouldBe("SmartLight");

        _testOutputHelper.WriteLine($"Smart light connected: {deviceConnection.DeviceId}");
    }

    [Fact]
    public async Task SmartLightGAgent_TurnOnOff_ShouldUpdateState()
    {
        // Arrange
        var config = CreateSmartLightConfig("test-light-002");
        var smartLight = await _gAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());
        await smartLight.InitializeDeviceConnectionAsync(config);

        // Act - Turn on
        var turnOnResult = await smartLight.TurnOnAsync();
        
        // Assert - Turn on
        turnOnResult.ShouldBeTrue();
        
        var statusAfterOn = await smartLight.GetLightStatusAsync();
        statusAfterOn.IsOn.ShouldBeTrue();
        _testOutputHelper.WriteLine($"Light turned on - Status: {statusAfterOn.IsOn}");

        // Act - Turn off
        var turnOffResult = await smartLight.TurnOffAsync();
        
        // Assert - Turn off
        turnOffResult.ShouldBeTrue();
        
        var statusAfterOff = await smartLight.GetLightStatusAsync();
        statusAfterOff.IsOn.ShouldBeFalse();
        _testOutputHelper.WriteLine($"Light turned off - Status: {statusAfterOff.IsOn}");
    }

    [Fact]
    public async Task SmartLightGAgent_SetBrightness_ShouldUpdateBrightnessCorrectly()
    {
        // Arrange
        var config = CreateSmartLightConfig("test-light-003");
        var smartLight = await _gAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());
        await smartLight.InitializeDeviceConnectionAsync(config);

        // Act - Set brightness to 75%
        var setBrightnessResult = await smartLight.SetBrightnessAsync(75);
        
        // Assert
        setBrightnessResult.ShouldBeTrue();
        
        var status = await smartLight.GetLightStatusAsync();
        status.Brightness.ShouldBe(75);
        _testOutputHelper.WriteLine($"Brightness set to: {status.Brightness}%");

        // Act - Set brightness to 25%
        var setBrightnessResult2 = await smartLight.SetBrightnessAsync(25);
        
        // Assert
        setBrightnessResult2.ShouldBeTrue();
        
        var status2 = await smartLight.GetLightStatusAsync();
        status2.Brightness.ShouldBe(25);
        _testOutputHelper.WriteLine($"Brightness changed to: {status2.Brightness}%");
    }

    [Fact]
    public async Task SmartLightGAgent_SetBrightness_InvalidRange_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateSmartLightConfig("test-light-004");
        var smartLight = await _gAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());
        await smartLight.InitializeDeviceConnectionAsync(config);

        // Act & Assert - Test invalid brightness values
        var result1 = await smartLight.SetBrightnessAsync(-10); // Below range
        result1.ShouldBeFalse();
        
        var result2 = await smartLight.SetBrightnessAsync(150); // Above range
        result2.ShouldBeFalse();
        
        _testOutputHelper.WriteLine("Invalid brightness values correctly rejected");
    }

    [Fact]
    public async Task SmartLightGAgent_SetColor_ShouldUpdateColorCorrectly()
    {
        // Arrange
        var config = CreateSmartLightConfig("test-light-005");
        var smartLight = await _gAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());
        await smartLight.InitializeDeviceConnectionAsync(config);

        // Act - Set color to red
        var setColorResult = await smartLight.SetColorAsync("#FF0000");
        
        // Assert
        setColorResult.ShouldBeTrue();
        
        var status = await smartLight.GetLightStatusAsync();
        status.Color.ShouldBe("#FF0000");
        _testOutputHelper.WriteLine($"Color set to: {status.Color}");

        // Act - Set color to blue
        var setColorResult2 = await smartLight.SetColorAsync("#0000FF");
        
        // Assert
        setColorResult2.ShouldBeTrue();
        
        var status2 = await smartLight.GetLightStatusAsync();
        status2.Color.ShouldBe("#0000FF");
        _testOutputHelper.WriteLine($"Color changed to: {status2.Color}");
    }

    [Fact]
    public async Task SmartLightGAgent_SetColor_InvalidFormat_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateSmartLightConfig("test-light-006");
        var smartLight = await _gAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());
        await smartLight.InitializeDeviceConnectionAsync(config);

        // Act & Assert - Test invalid color formats
        var result1 = await smartLight.SetColorAsync("FF0000"); // Missing #
        result1.ShouldBeFalse();
        
        var result2 = await smartLight.SetColorAsync("#FF00"); // Too short
        result2.ShouldBeFalse();
        
        var result3 = await smartLight.SetColorAsync(""); // Empty
        result3.ShouldBeFalse();
        
        _testOutputHelper.WriteLine("Invalid color formats correctly rejected");
    }

    [Fact]
    public async Task SmartLightGAgent_Toggle_ShouldSwitchOnOffState()
    {
        // Arrange
        var config = CreateSmartLightConfig("test-light-007");
        var smartLight = await _gAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());
        await smartLight.InitializeDeviceConnectionAsync(config);

        // Get initial state (should be off by default)
        var initialStatus = await smartLight.GetLightStatusAsync();
        var initialState = initialStatus.IsOn;
        _testOutputHelper.WriteLine($"Initial state: {initialState}");

        // Act - Toggle
        var toggleResult = await smartLight.ToggleAsync();
        
        // Assert - State should be opposite of initial
        toggleResult.ShouldBeTrue();
        
        var statusAfterToggle = await smartLight.GetLightStatusAsync();
        statusAfterToggle.IsOn.ShouldBe(!initialState);
        _testOutputHelper.WriteLine($"After toggle: {statusAfterToggle.IsOn}");

        // Act - Toggle again
        var toggleResult2 = await smartLight.ToggleAsync();
        
        // Assert - State should be back to initial
        toggleResult2.ShouldBeTrue();
        
        var statusAfterSecondToggle = await smartLight.GetLightStatusAsync();
        statusAfterSecondToggle.IsOn.ShouldBe(initialState);
        _testOutputHelper.WriteLine($"After second toggle: {statusAfterSecondToggle.IsOn}");
    }

    [Fact]
    public async Task SmartLightGAgent_GetDeviceStatusSummary_ShouldReturnCorrectInfo()
    {
        // Arrange
        var config = CreateSmartLightConfig("test-light-008");
        var smartLight = await _gAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());
        await smartLight.InitializeDeviceConnectionAsync(config);

        // Set up some state
        await smartLight.TurnOnAsync();
        await smartLight.SetBrightnessAsync(80);
        await smartLight.SetColorAsync("#00FF00");

        // Act
        var statusSummary = await smartLight.GetDeviceStatusSummaryAsync();
        
        // Assert
        statusSummary.ShouldNotBeNull();
        statusSummary.DeviceId.ShouldBe("test-light-008");
        statusSummary.DeviceName.ShouldBe("Test Smart Light");
        statusSummary.DeviceType.ShouldBe("SmartLight");
        statusSummary.IsConnected.ShouldBeTrue();
        statusSummary.Status.ShouldBe(DeviceConnectionStatus.Connected);
        statusSummary.Properties.ShouldNotBeNull();
        statusSummary.Properties.Count.ShouldBeGreaterThan(0);
        
        _testOutputHelper.WriteLine($"Device Status: {statusSummary.DeviceId} - {statusSummary.Status}");
        _testOutputHelper.WriteLine($"Properties: {string.Join(", ", statusSummary.Properties.Select(p => $"{p.Key}={p.Value}"))}");
    }

    [Fact]
    public async Task SmartLightGAgent_PowerConsumptionCalculation_ShouldBeCorrect()
    {
        // Arrange
        var config = CreateSmartLightConfig("test-light-009");
        var smartLight = await _gAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());
        await smartLight.InitializeDeviceConnectionAsync(config);

        // Act & Assert - Light off, power consumption should be 0
        await smartLight.TurnOffAsync();
        var statusOff = await smartLight.GetLightStatusAsync();
        statusOff.PowerConsumption.ShouldBe(0.0);
        _testOutputHelper.WriteLine($"Power consumption when off: {statusOff.PowerConsumption}W");

        // Act & Assert - Light on at 50% brightness
        await smartLight.TurnOnAsync();
        await smartLight.SetBrightnessAsync(50);
        var status50 = await smartLight.GetLightStatusAsync();
        status50.PowerConsumption.ShouldBe(5.0); // 50% of 10W max
        _testOutputHelper.WriteLine($"Power consumption at 50%: {status50.PowerConsumption}W");

        // Act & Assert - Light on at 100% brightness
        await smartLight.SetBrightnessAsync(100);
        var status100 = await smartLight.GetLightStatusAsync();
        status100.PowerConsumption.ShouldBe(10.0); // 100% of 10W max
        _testOutputHelper.WriteLine($"Power consumption at 100%: {status100.PowerConsumption}W");
    }

    [Fact]
    public async Task SmartLightGAgent_OperationsWithoutConnection_ShouldReturnFalse()
    {
        // Arrange - Create GAgent but don't initialize connection
        var smartLight = await _gAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());

        // Act & Assert - All operations should return false without connection
        var turnOnResult = await smartLight.TurnOnAsync();
        turnOnResult.ShouldBeFalse();
        
        var turnOffResult = await smartLight.TurnOffAsync();
        turnOffResult.ShouldBeFalse();
        
        var setBrightnessResult = await smartLight.SetBrightnessAsync(50);
        setBrightnessResult.ShouldBeFalse();
        
        var setColorResult = await smartLight.SetColorAsync("#FF0000");
        setColorResult.ShouldBeFalse();
        
        var toggleResult = await smartLight.ToggleAsync();
        toggleResult.ShouldBeFalse();
        
        _testOutputHelper.WriteLine("All operations correctly failed without device connection");
    }

    [Fact]
    public async Task SmartLightGAgent_ConcurrentOperations_ShouldHandleCorrectly()
    {
        // Arrange
        var config = CreateSmartLightConfig("test-light-010");
        var smartLight = await _gAgentFactory.GetGAgentAsync<ISmartLightGAgent>(Guid.NewGuid());
        await smartLight.InitializeDeviceConnectionAsync(config);

        // Act - Execute multiple operations concurrently
        var tasks = new List<Task<bool>>
        {
            smartLight.TurnOnAsync(),
            smartLight.SetBrightnessAsync(75),
            smartLight.SetColorAsync("#FF00FF")
        };

        var results = await Task.WhenAll(tasks);
        
        // Assert - All operations should succeed
        results.All(r => r).ShouldBeTrue();
        
        // Give time for all operations to complete
        await Task.Delay(500);
        
        var finalStatus = await smartLight.GetLightStatusAsync();
        finalStatus.IsOn.ShouldBeTrue();
        
        _testOutputHelper.WriteLine($"Concurrent operations completed - Final state: On={finalStatus.IsOn}, Brightness={finalStatus.Brightness}%, Color={finalStatus.Color}");
    }
}
