using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Service;
using Aevatar.Options;
using Shouldly;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Aevatar.Application.Tests.Service;

public class AgentServiceTests
{
    [Fact]
    public async Task GetConfigurationDefaultValuesAsync_WithValidType_ShouldReturnCorrectFormat()
    {
        // Arrange
        var agentServiceType = typeof(AgentService);
        var method = agentServiceType.GetMethod("GetConfigurationDefaultValuesAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        method.ShouldNotBeNull("GetConfigurationDefaultValuesAsync method should exist");
        
        var testConfigType = typeof(TestConfiguration);
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

    [Fact]
    public async Task GetConfigurationDefaultValuesAsync_WithComplexTypes_ShouldHandleCorrectly()
    {
        // Arrange
        var agentServiceType = typeof(AgentService);
        var method = agentServiceType.GetMethod("GetConfigurationDefaultValuesAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var testConfigType = typeof(ComplexTypeConfiguration);
        var agentService = CreateAgentServiceForTesting();
        
        // Act
        var result = await (Task<Dictionary<string, object?>>)method.Invoke(agentService, new object[] { testConfigType });
        
        // Assert
        result.ShouldNotBeNull();
        
        // Test DateTime default value
        result.ShouldContainKey("dateTimeProperty");
        var dateTimeValue = result["dateTimeProperty"] as List<object>;
        dateTimeValue.ShouldNotBeNull();
        dateTimeValue.Count.ShouldBe(1);
        dateTimeValue[0].ShouldBeOfType<DateTime>();
        
        // Test Enum default value
        result.ShouldContainKey("enumProperty");
        var enumValue = result["enumProperty"] as List<object>;
        enumValue.ShouldNotBeNull();
        enumValue.Count.ShouldBe(1);
        enumValue[0].ShouldBe(TestEnum.Value1);
        
        // Test Guid default value
        result.ShouldContainKey("guidProperty");
        var guidValue = result["guidProperty"] as List<object>;
        guidValue.ShouldNotBeNull();
        guidValue.Count.ShouldBe(1);
        guidValue[0].ShouldBeOfType<Guid>();
        
        // Test Collection default value (should be null initially, becomes empty list)
        result.ShouldContainKey("listProperty");
        var listValue = result["listProperty"] as List<object>;
        listValue.ShouldNotBeNull();
        listValue.Count.ShouldBe(0); // null collection becomes empty list
    }

    [Fact]
    public async Task GetConfigurationDefaultValuesAsync_WithPropertyAccessException_ShouldReturnEmptyList()
    {
        // Arrange
        var agentServiceType = typeof(AgentService);
        var method = agentServiceType.GetMethod("GetConfigurationDefaultValuesAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var testConfigType = typeof(ExceptionThrowingConfiguration);
        var agentService = CreateAgentServiceForTesting();
        
        // Act
        var result = await (Task<Dictionary<string, object?>>)method.Invoke(agentService, new object[] { testConfigType });
        
        // Assert
        result.ShouldNotBeNull();
        result.ShouldContainKey("normalProperty");
        result.ShouldContainKey("exceptionProperty");
        
        // Normal property should work fine
        var normalValue = result["normalProperty"] as List<object>;
        normalValue.ShouldNotBeNull();
        normalValue.Count.ShouldBe(1);
        normalValue[0].ShouldBe("Normal");
        
        // Exception property should return empty list
        var exceptionValue = result["exceptionProperty"] as List<object>;
        exceptionValue.ShouldNotBeNull();
        exceptionValue.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetConfigurationDefaultValuesAsync_WithAbstractClass_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var agentServiceType = typeof(AgentService);
        var method = agentServiceType.GetMethod("GetConfigurationDefaultValuesAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var testConfigType = typeof(AbstractConfiguration);
        var agentService = CreateAgentServiceForTesting();
        
        // Act
        var result = await (Task<Dictionary<string, object?>>)method.Invoke(agentService, new object[] { testConfigType });
        
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0); // Should be empty due to constructor exception
    }

    [Fact]
    public async Task GetConfigurationDefaultValuesAsync_WithNoPublicProperties_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var agentServiceType = typeof(AgentService);
        var method = agentServiceType.GetMethod("GetConfigurationDefaultValuesAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var testConfigType = typeof(EmptyConfiguration);
        var agentService = CreateAgentServiceForTesting();
        
        // Act
        var result = await (Task<Dictionary<string, object?>>)method.Invoke(agentService, new object[] { testConfigType });
        
        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0); // No public properties
    }

    [Fact]
    public async Task GetConfigurationDefaultValuesAsync_WithSingleCharacterProperty_ShouldHandleCorrectly()
    {
        // Arrange
        var agentServiceType = typeof(AgentService);
        var method = agentServiceType.GetMethod("GetConfigurationDefaultValuesAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var testConfigType = typeof(SingleCharPropertyConfiguration);
        var agentService = CreateAgentServiceForTesting();
        
        // Act
        var result = await (Task<Dictionary<string, object?>>)method.Invoke(agentService, new object[] { testConfigType });
        
        // Assert
        result.ShouldNotBeNull();
        result.ShouldContainKey("x"); // Single character property name should become lowercase
        
        var value = result["x"] as List<object>;
        value.ShouldNotBeNull();
        value.Count.ShouldBe(1);
        value[0].ShouldBe(100);
    }

    [Fact]
    public async Task GetConfigurationDefaultValuesAsync_WithInheritedProperties_ShouldOnlyIncludeDeclaredProperties()
    {
        // Arrange
        var agentServiceType = typeof(AgentService);
        var method = agentServiceType.GetMethod("GetConfigurationDefaultValuesAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var testConfigType = typeof(DerivedConfiguration);
        var agentService = CreateAgentServiceForTesting();
        
        // Act
        var result = await (Task<Dictionary<string, object?>>)method.Invoke(agentService, new object[] { testConfigType });
        
        // Assert
        result.ShouldNotBeNull();
        result.ShouldContainKey("derivedProperty"); // Should include derived class property
        result.ShouldNotContainKey("baseProperty"); // Should NOT include base class property due to DeclaredOnly flag
        
        var derivedValue = result["derivedProperty"] as List<object>;
        derivedValue.ShouldNotBeNull();
        derivedValue.Count.ShouldBe(1);
        derivedValue[0].ShouldBe("Derived");
    }

    [Fact]
    public async Task GetConfigurationDefaultValuesAsync_WithAIGAgentSystemLLM_ShouldReturnSystemLLMListFromOptions()
    {
        // Arrange
        var agentServiceType = typeof(AgentService);
        var method = agentServiceType.GetMethod("GetConfigurationDefaultValuesAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var testConfigType = typeof(AIGAgentConfiguration);
        var agentService = CreateAgentServiceForTestingWithSystemLLMOptions();
        
        // Act
        var result = await (Task<Dictionary<string, object?>>)method.Invoke(agentService, 
            new object[] { testConfigType, "Aevatar.Application.Grains.Agents.AI.TestAIGAgent", "TestAIGAgent" });
        
        // Assert
        result.ShouldNotBeNull();
        result.ShouldContainKey("systemLLM");
        
        var systemLLMValue = result["systemLLM"] as List<string>;
        systemLLMValue.ShouldNotBeNull();
        systemLLMValue.Count.ShouldBe(2);
        systemLLMValue.ShouldContain("gpt-4");
        systemLLMValue.ShouldContain("deepseek");
        
        // Verify other properties still work normally
        result.ShouldContainKey("instructions");
        var instructionsValue = result["instructions"] as List<object>;
        instructionsValue.ShouldNotBeNull();
        instructionsValue.Count.ShouldBe(1);
        instructionsValue[0].ShouldBe("Default AI instructions");
    }

    private AgentService CreateAgentServiceForTesting()
    {
        // For testing private methods, we can use FormatterServices.GetUninitializedObject
        // to create an instance without calling the constructor
        var agentService = (AgentService)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(AgentService));
        
        // Set up a mock logger to avoid null reference exceptions
        var mockLogger = new Mock<ILogger<AgentService>>();
        
        // Use reflection to set the private _logger field
        var loggerField = typeof(AgentService).GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(agentService, mockLogger.Object);
        
        return agentService;
    }

    private AgentService CreateAgentServiceForTestingWithSystemLLMOptions()
    {
        // For testing private methods, we can use FormatterServices.GetUninitializedObject
        // to create an instance without calling the constructor
        var agentService = (AgentService)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(AgentService));
        
        // Set up a mock logger to avoid null reference exceptions
        var mockLogger = new Mock<ILogger<AgentService>>();
        
        // Set up mock SystemLLMConfigOptions
        var mockSystemLLMOptions = new Mock<IOptionsMonitor<SystemLLMConfigOptions>>();
        var systemLLMConfig = new SystemLLMConfigOptions
        {
            SystemLLMConfigs = new List<string> { "gpt-4", "deepseek" }
        };
        mockSystemLLMOptions.Setup(x => x.CurrentValue).Returns(systemLLMConfig);
        
        // Use reflection to set the private fields
        var loggerField = typeof(AgentService).GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(agentService, mockLogger.Object);
        
        var systemLLMOptionsField = typeof(AgentService).GetField("_systemLLMConfigOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        systemLLMOptionsField?.SetValue(agentService, mockSystemLLMOptions.Object);
        
        return agentService;
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

    public class ComplexTypeConfiguration
    {
        public DateTime DateTimeProperty { get; set; } = new DateTime(2023, 1, 1);
        public TestEnum EnumProperty { get; set; } = TestEnum.Value1;
        public Guid GuidProperty { get; set; } = Guid.NewGuid();
        public List<string>? ListProperty { get; set; } = null; // Will be null initially
        public decimal DecimalProperty { get; set; } = 123.45m;
    }

    public class ExceptionThrowingConfiguration
    {
        public string NormalProperty { get; set; } = "Normal";
        
        public string ExceptionProperty 
        { 
            get => throw new InvalidOperationException("Property access exception"); 
            set { }
        }
    }

    public abstract class AbstractConfiguration
    {
        public string AbstractProperty { get; set; } = "Abstract";
    }

    public class EmptyConfiguration
    {
        // No public properties
        private string PrivateProperty { get; set; } = "Private";
        internal string InternalProperty { get; set; } = "Internal";
    }

    public class SingleCharPropertyConfiguration
    {
        public int X { get; set; } = 100;
    }

    public class BaseConfiguration
    {
        public string BaseProperty { get; set; } = "Base";
    }

    public class DerivedConfiguration : BaseConfiguration
    {
        public string DerivedProperty { get; set; } = "Derived";
    }

    public class AIGAgentConfiguration
    {
        public string Instructions { get; set; } = "Default AI instructions";
        public string SystemLLM { get; set; } = "default-llm";
        public int MaxTokens { get; set; } = 1000;
    }

    public enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }
} 