# Aevatar.GAgents.Device.Http

HTTP-based virtual device GAgents that connect to Virtual Device Hub API for real device control and monitoring.

## Overview

This module provides HTTP-based device GAgents that communicate with real devices through the Virtual Device Hub API. These GAgents are designed to work with AI assistants through event-driven control.

## Device Types

### 1. HttpSmartLightGAgent
- **Device ID**: `http-smart-light`
- **Purpose**: Controls smart light bulbs through API
- **Capabilities**: Power control, brightness adjustment, color setting, color temperature control

### 2. HttpTemperatureSensorGAgent  
- **Device ID**: `http-temperature-sensor`
- **Purpose**: Monitors environmental conditions through API
- **Capabilities**: Temperature reading, humidity monitoring, battery level checking, sensor calibration

### 3. HttpSmartSwitchGAgent
- **Device ID**: `http-smart-switch` 
- **Purpose**: Controls electrical switches and monitors power consumption through API
- **Capabilities**: Power control, electrical monitoring, usage tracking, safety monitoring

## AI Integration

These GAgents are designed to work with AI assistants through **event-driven control**. The AI does not call methods directly, but sends events that are handled by the GAgents.

### Available Events for AI Control

#### Device Property Operations
- `ReadDevicePropertyEvent` - Read device property values
- `WriteDevicePropertyEvent` - Write device property values  

#### Device Action Operations
- `ExecuteDeviceActionEvent` - Execute device actions/commands

#### Device Status Operations
- `GetDeviceStatusEvent` - Get complete device status
- `ConnectDeviceEvent` - Connect to device
- `DisconnectDeviceEvent` - Disconnect from device

### Event Parameters

All events include detailed `[Description]` attributes on their properties to help AI understand parameter requirements:

- **PropertyName**: Specific property names like 'Power', 'Brightness', 'Color', 'Temperature'
- **ValueJson**: JSON-formatted values (e.g., 'true', '75', '"#FF0000"')
- **ActionName**: Action names like 'TurnOn', 'TurnOff', 'SetBrightness', 'Calibrate'
- **ParametersJson**: JSON-formatted parameters (e.g., '{"brightness": 75}')

## GetDescriptionAsync Optimization

Each GAgent's `GetDescriptionAsync()` method has been optimized to provide AI-friendly descriptions that include:

1. **Device Identity**: Clear Device ID and Name display
2. **Event-Based Control**: Instructions on which events to send for control
3. **Parameter Examples**: Specific examples of event parameters
4. **Current Status**: Real-time device status information
5. **Safety Notes**: Important safety considerations for electrical devices

## Usage Example

The AI can control devices by sending events like:

```csharp
// Turn on a smart light
var turnOnEvent = new WriteDevicePropertyEvent
{
    DeviceId = "light001",
    DeviceName = "Living Room Light", 
    PropertyName = "Power",
    ValueJson = "true"
};

// Set brightness
var brightnessEvent = new ExecuteDeviceActionEvent
{
    DeviceId = "light001",
    DeviceName = "Living Room Light",
    ActionName = "SetBrightness", 
    ParametersJson = "{\"brightness\": 75}"
};
```

## API Integration

These GAgents connect to the Virtual Device Hub API using:
- **Base URL**: Configurable API endpoint
- **Authentication**: Optional API key support
- **Error Handling**: Robust retry and error handling
- **Real-time Status**: Live device status monitoring

The API follows OpenAPI 3.0.1 specification with endpoints for device management, status monitoring, and command execution.
