using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Service;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Service;

public class AgentServiceTests
{
    [Fact]
    public void AgentService_TypeExists_ShouldPass()
    {
        // Arrange & Act
        var serviceType = typeof(AgentService);
        
        // Assert - Simply verify the type exists and has expected methods
        serviceType.ShouldNotBeNull();
        serviceType.GetMethod("GetAllAgents").ShouldNotBeNull();
        serviceType.GetMethod("CreateAgentAsync").ShouldNotBeNull();
        serviceType.GetMethod("GetAgentAsync").ShouldNotBeNull();
        serviceType.GetMethod("UpdateAgentAsync").ShouldNotBeNull();
        serviceType.GetMethod("DeleteAgentAsync").ShouldNotBeNull();
        serviceType.GetMethod("RemoveAllSubAgentAsync").ShouldNotBeNull();
    }

    [Fact]
    public void AgentService_Constructor_RequiresDependencies()
    {
        // Arrange & Act
        var constructors = typeof(AgentService).GetConstructors();
        
        // Assert - Verify constructor exists and has expected parameters
        constructors.Length.ShouldBe(1);
        var constructor = constructors[0];
        var parameters = constructor.GetParameters();
        
        // Should have 10 parameters based on AgentService constructor
        parameters.Length.ShouldBe(10);
        
        // Verify some key parameter types exist
        parameters.ShouldContain(p => p.ParameterType.Name.Contains("IClusterClient"));
        parameters.ShouldContain(p => p.ParameterType.Name.Contains("ICQRSProvider"));
        parameters.ShouldContain(p => p.ParameterType.Name.Contains("ILogger"));
    }

    [Fact]
    public async Task GetConfigurationDefaultValuesAsync_WithValidType_ShouldReturnCorrectFormat()
    {
        // Arrange
        var agentServiceType = typeof(AgentService);
        var method = agentServiceType.GetMethod("GetConfigurationDefaultValuesAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        method.ShouldNotBeNull("GetConfigurationDefaultValuesAsync method should exist");
        
        // Create a simple test configuration type
        var testConfigType = typeof(TestConfiguration);
        
        // Create AgentService instance with null parameters for this test
        var agentService = CreateAgentServiceForTesting();
        
        // Act
        var result = await (Task<Dictionary<string, object?>>)method.Invoke(agentService, new object[] { testConfigType });
        
        // Assert
        result.ShouldNotBeNull();
        result.ShouldContainKey("stringProperty");
        result.ShouldContainKey("intProperty");
        result.ShouldContainKey("boolProperty");
        
        // Verify list format: non-null values should be single-item lists
        var stringValue = result["stringProperty"] as List<object>;
        stringValue.ShouldNotBeNull();
        stringValue.Count.ShouldBe(1);
        stringValue[0].ShouldBe("DefaultValue");
        
        var intValue = result["intProperty"] as List<object>;
        intValue.ShouldNotBeNull();
        intValue.Count.ShouldBe(1);
        intValue[0].ShouldBe(42);
        
        var boolValue = result["boolProperty"] as List<object>;
        boolValue.ShouldNotBeNull();
        boolValue.Count.ShouldBe(1);
        boolValue[0].ShouldBe(true);
    }

    [Fact]
    public async Task GetConfigurationDefaultValuesAsync_WithNullProperty_ShouldReturnEmptyList()
    {
        // Arrange
        var agentServiceType = typeof(AgentService);
        var method = agentServiceType.GetMethod("GetConfigurationDefaultValuesAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var testConfigType = typeof(TestConfigurationWithNulls);
        var agentService = CreateAgentServiceForTesting();
        
        // Act
        var result = await (Task<Dictionary<string, object?>>)method.Invoke(agentService, new object[] { testConfigType });
        
        // Assert
        result.ShouldNotBeNull();
        result.ShouldContainKey("nullStringProperty");
        
        // Verify null values become empty lists
        var nullValue = result["nullStringProperty"] as List<object>;
        nullValue.ShouldNotBeNull();
        nullValue.Count.ShouldBe(0);
    }

    private AgentService CreateAgentServiceForTesting()
    {
        // For testing private methods, we can use FormatterServices.GetUninitializedObject
        // to create an instance without calling the constructor
        return (AgentService)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(AgentService));
    }

    // Test configuration classes
    public class TestConfiguration
    {
        public string StringProperty { get; set; } = "DefaultValue";
        public int IntProperty { get; set; } = 42;
        public bool BoolProperty { get; set; } = true;
    }

    public class TestConfigurationWithNulls
    {
        public string? NullStringProperty { get; set; } = null;
        public int? NullIntProperty { get; set; } = null;
    }
} 