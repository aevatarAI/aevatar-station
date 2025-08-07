using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Aevatar.Agent;
using Aevatar.Service;
using Aevatar.Options;
using Aevatar.Application.Grains.Agents.AI;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.Core.Abstractions;
using Shouldly;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq;
using System.Diagnostics;

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
    public async Task GetConfigurationDefaultValuesAsync_WithRegularConfiguration_ShouldReturnNormalValues()
    {
        // Arrange
        var agentServiceType = typeof(AgentService);
        var method = agentServiceType.GetMethod("GetConfigurationDefaultValuesAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        var testConfigType = typeof(AIGAgentConfiguration);
        var agentService = CreateAgentServiceForTestingWithSystemLLMOptions();
        
        // Act
        var result = await (Task<Dictionary<string, object?>>)method.Invoke(agentService, 
            new object[] { testConfigType });
        
        // Assert
        result.ShouldNotBeNull();
        result.ShouldContainKey("systemLLM");
        
        // Since AIGAgentConfiguration is not a real AIGAgent type (no longer detected by string matching),
        // it should be treated as a regular configuration and return normal list format
        var systemLLMValue = result["systemLLM"] as List<object>;
        systemLLMValue.ShouldNotBeNull();
        systemLLMValue.Count.ShouldBe(1);
        systemLLMValue[0].ShouldBe("default-llm");
        
        // Verify other properties still work normally
        result.ShouldContainKey("instructions");
        var instructionsValue = result["instructions"] as List<object>;
        instructionsValue.ShouldNotBeNull();
        instructionsValue.Count.ShouldBe(1);
        instructionsValue[0].ShouldBe("Default AI instructions");
    }

    /// <summary>
    /// 全面测试新的内联AI Agent检测逻辑是否兼容各种AI Agent类型
    /// </summary>
    [Fact]
    public async Task NewInlinedAIGAgentDetection_ShouldCorrectlyIdentifyAllAIAgentTypes()
    {
        // Arrange - 模拟新的内联逻辑：configurationType != null && typeof(IAIGAgent).IsAssignableFrom(configurationType)
        
        // 1. 测试具体的AI Agent接口类型
        var textCompletionInterfaceType = typeof(ITextCompletionGAgent);
        var workflowComposerInterfaceType = typeof(IWorkflowComposerGAgent);
        
        // 2. 测试具体的AI Agent实现类型  
        var textCompletionClassType = typeof(TextCompletionGAgent);
        var workflowComposerClassType = typeof(WorkflowComposerGAgent);
        
        // 3. 测试基础接口类型（应该被检测为非AI Agent）
        var baseGAgentType = typeof(IGAgent);
        var nullType = (Type?)null;
        
        // 4. 测试普通非AI类型（应该被检测为非AI Agent）
        var stringType = typeof(string);
        var intType = typeof(int);
        
        // Act - 使用新的内联判断逻辑
        var isTextCompletionInterface = textCompletionInterfaceType != null && typeof(IAIGAgent).IsAssignableFrom(textCompletionInterfaceType);
        var isWorkflowComposerInterface = workflowComposerInterfaceType != null && typeof(IAIGAgent).IsAssignableFrom(workflowComposerInterfaceType);
        
        var isTextCompletionClass = textCompletionClassType != null && typeof(IAIGAgent).IsAssignableFrom(textCompletionClassType);
        var isWorkflowComposerClass = workflowComposerClassType != null && typeof(IAIGAgent).IsAssignableFrom(workflowComposerClassType);
        
        var isBaseGAgent = baseGAgentType != null && typeof(IAIGAgent).IsAssignableFrom(baseGAgentType);
        var isNullType = nullType != null && typeof(IAIGAgent).IsAssignableFrom(nullType);
        
        var isStringType = stringType != null && typeof(IAIGAgent).IsAssignableFrom(stringType);
        var isIntType = intType != null && typeof(IAIGAgent).IsAssignableFrom(intType);
        
        // Assert - 验证结果
        // ✅ 应该被识别为AI Agent的类型
        isTextCompletionInterface.ShouldBeTrue("ITextCompletionGAgent接口应该被识别为AI Agent");
        isWorkflowComposerInterface.ShouldBeTrue("IWorkflowComposerGAgent接口应该被识别为AI Agent");
        isTextCompletionClass.ShouldBeTrue("TextCompletionGAgent实现类应该被识别为AI Agent");
        isWorkflowComposerClass.ShouldBeTrue("WorkflowComposerGAgent实现类应该被识别为AI Agent");
        
        // ❌ 不应该被识别为AI Agent的类型
        isBaseGAgent.ShouldBeFalse("基础IGAgent接口不应该被识别为AI Agent");
        isNullType.ShouldBeFalse("null类型不应该被识别为AI Agent");
        isStringType.ShouldBeFalse("string类型不应该被识别为AI Agent");
        isIntType.ShouldBeFalse("int类型不应该被识别为AI Agent");
        
        // 额外验证：模拟ChatAIGAgent的多层接口继承情况
        // 假设 IChatAIGAgent : IAIGAgent, 然后 ChatAIGAgent : IChatAIGAgent
        // 这种情况下我们的逻辑也应该正确工作
        
        // 从现有的接口验证多层继承检测
        // ITextCompletionGAgent : IAIGAgent, IStateGAgent<TextCompletionState>
        // 这已经是多层继承的例子
        var multiLevelInheritance = typeof(IAIGAgent).IsAssignableFrom(typeof(ITextCompletionGAgent));
        multiLevelInheritance.ShouldBeTrue("多层接口继承应该被正确检测");
        
        // 打印详细的继承信息用于调试
        LogTypeInheritanceInfo(typeof(ITextCompletionGAgent), "ITextCompletionGAgent");
        LogTypeInheritanceInfo(typeof(TextCompletionGAgent), "TextCompletionGAgent");
        LogTypeInheritanceInfo(typeof(IWorkflowComposerGAgent), "IWorkflowComposerGAgent");
        LogTypeInheritanceInfo(typeof(WorkflowComposerGAgent), "WorkflowComposerGAgent");
    }
    
    /// <summary>
    /// 记录类型继承信息用于调试
    /// </summary>
    private void LogTypeInheritanceInfo(Type type, string typeName)
    {
        var interfaces = type.GetInterfaces();
        var baseType = type.BaseType;
        
        var inheritanceInfo = $"{typeName} 继承信息:\n";
        inheritanceInfo += $"  - 基类: {baseType?.Name ?? "无"}\n";
        inheritanceInfo += $"  - 实现接口: {string.Join(", ", interfaces.Select(i => i.Name))}\n";
        inheritanceInfo += $"  - 是否为IAIGAgent: {typeof(IAIGAgent).IsAssignableFrom(type)}\n";
        
        // 在测试输出中显示这些信息
        System.Diagnostics.Debug.WriteLine(inheritanceInfo);
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
        
        // Set up mock AgentDefaultValuesOptions (not AgentDefaultValuesOptions)
        var mockAgentDefaultValuesOptions = new Mock<IOptionsMonitor<AgentDefaultValuesOptions>>();
        var agentDefaultValuesConfig = new AgentDefaultValuesOptions
        {
            SystemLLMConfigs = new List<string> { "gpt-4", "deepseek" }
        };
        mockAgentDefaultValuesOptions.Setup(x => x.CurrentValue).Returns(agentDefaultValuesConfig);
        
        // Use reflection to set the private fields
        var loggerField = typeof(AgentService).GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(agentService, mockLogger.Object);
        
        var agentDefaultValuesOptionsField = typeof(AgentService).GetField("_agentDefaultValuesOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        agentDefaultValuesOptionsField?.SetValue(agentService, mockAgentDefaultValuesOptions.Object);
        
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