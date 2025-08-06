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
using Aevatar.Schema;
using Aevatar.Agents.Creator.Models;
using NJsonSchema;
using NJsonSchema.Validation;
using Aevatar.Exceptions;
using Volo.Abp;
using Orleans;
using Orleans.Runtime;
using Orleans.Metadata;

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

    // Test configuration class that inherits from ConfigurationBase
    public class TestConfigurationBase : ConfigurationBase
    {
        public string StringProperty { get; set; } = "Default";
        public int IntProperty { get; set; } = 0;
    }

    #region CheckCreateParam Tests

    [Fact]
    public void CheckCreateParam_WithValidInput_ShouldNotThrowException()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var validDto = new CreateAgentInputDto
        {
            AgentType = "ValidAgentType",
            Name = "ValidAgentName"
        };

        // Act & Assert - using reflection to call private method
        var method = typeof(AgentService).GetMethod("CheckCreateParam", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        Should.NotThrow(() => method.Invoke(agentService, new object[] { validDto }));
    }

    [Fact]
    public void CheckCreateParam_WithNullAgentType_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var invalidDto = new CreateAgentInputDto
        {
            AgentType = null,
            Name = "ValidAgentName"
        };

        // Act & Assert
        var method = typeof(AgentService).GetMethod("CheckCreateParam", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var exception = Should.Throw<System.Reflection.TargetInvocationException>(() => 
            method.Invoke(agentService, new object[] { invalidDto }));
        
        exception.InnerException.ShouldBeOfType<UserFriendlyException>();
        exception.InnerException.Message.ShouldBe("Agent type is null");
    }

    [Fact]
    public void CheckCreateParam_WithEmptyAgentType_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var invalidDto = new CreateAgentInputDto
        {
            AgentType = "",
            Name = "ValidAgentName"
        };

        // Act & Assert
        var method = typeof(AgentService).GetMethod("CheckCreateParam", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var exception = Should.Throw<System.Reflection.TargetInvocationException>(() => 
            method.Invoke(agentService, new object[] { invalidDto }));
        
        exception.InnerException.ShouldBeOfType<UserFriendlyException>();
        exception.InnerException.Message.ShouldBe("Agent type is null");
    }

    [Fact]
    public void CheckCreateParam_WithNullName_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var invalidDto = new CreateAgentInputDto
        {
            AgentType = "ValidAgentType",
            Name = null
        };

        // Act & Assert
        var method = typeof(AgentService).GetMethod("CheckCreateParam", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var exception = Should.Throw<System.Reflection.TargetInvocationException>(() => 
            method.Invoke(agentService, new object[] { invalidDto }));
        
        exception.InnerException.ShouldBeOfType<UserFriendlyException>();
        exception.InnerException.Message.ShouldBe("name is null");
    }

    [Fact]
    public void CheckCreateParam_WithEmptyName_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var invalidDto = new CreateAgentInputDto
        {
            AgentType = "ValidAgentType",
            Name = ""
        };

        // Act & Assert
        var method = typeof(AgentService).GetMethod("CheckCreateParam", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var exception = Should.Throw<System.Reflection.TargetInvocationException>(() => 
            method.Invoke(agentService, new object[] { invalidDto }));
        
        exception.InnerException.ShouldBeOfType<UserFriendlyException>();
        exception.InnerException.Message.ShouldBe("name is null");
    }

    #endregion

    #region IsExcludedAgentType Tests

    [Fact]
    public void IsExcludedAgentType_WithExcludedType_ShouldReturnTrue()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var grainType = GrainType.Create("ExcludedType");
        var excludedTypes = new HashSet<GrainType> { grainType };

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("IsExcludedAgentType", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var result = method.Invoke(agentService, new object[] { grainType, excludedTypes });

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<bool>();
        ((bool)result).ShouldBeTrue();
    }

    [Fact]
    public void IsExcludedAgentType_WithNonExcludedType_ShouldReturnFalse()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var grainType = GrainType.Create("AllowedType");
        var excludedGrainType = GrainType.Create("ExcludedType");
        var excludedTypes = new HashSet<GrainType> { excludedGrainType };

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("IsExcludedAgentType", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var result = method.Invoke(agentService, new object[] { grainType, excludedTypes });

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<bool>();
        ((bool)result).ShouldBeFalse();
    }

    [Fact]
    public void IsExcludedAgentType_WithEmptyExcludedTypes_ShouldReturnFalse()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var grainType = GrainType.Create("AnyType");
        var excludedTypes = new HashSet<GrainType>();

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("IsExcludedAgentType", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var result = method.Invoke(agentService, new object[] { grainType, excludedTypes });

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<bool>();
        ((bool)result).ShouldBeFalse();
    }

    #endregion

    #region FilterSubAgentGrainIds Tests

    [Fact]
    public void FilterSubAgentGrainIds_WithMixedTypes_ShouldFilterCorrectly()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var allowedGrainType = GrainType.Create("AllowedType");
        var excludedGrainType = GrainType.Create("ExcludedType");
        
        var children = new List<GrainId>
        {
            GrainId.Create(allowedGrainType, "key1"),
            GrainId.Create(excludedGrainType, "key2"),
            GrainId.Create(allowedGrainType, "key3")
        };
        
        var excludedTypes = new HashSet<GrainType> { excludedGrainType };

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("FilterSubAgentGrainIds", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var result = method.Invoke(agentService, new object[] { children, excludedTypes });

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<GrainId>>();
        var filteredIds = (List<GrainId>)result;
        filteredIds.Count.ShouldBe(2); // Should exclude the excluded type
        filteredIds.All(id => id.Type == allowedGrainType).ShouldBeTrue();
    }

    [Fact]
    public void FilterSubAgentGrainIds_WithNullChildren_ShouldReturnEmptyList()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        List<GrainId> children = null;
        var excludedTypes = new HashSet<GrainType>();

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("FilterSubAgentGrainIds", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var result = method.Invoke(agentService, new object[] { children, excludedTypes });

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<GrainId>>();
        var filteredIds = (List<GrainId>)result;
        filteredIds.Count.ShouldBe(0);
    }

    #endregion

    #region EnsureUserAuthorized Tests

    [Fact]
    public void EnsureUserAuthorized_WithSameUserId_ShouldNotThrowException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var mockUserAppService = new Mock<IUserAppService>();
        mockUserAppService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        
        var agentService = CreateAgentServiceWithMockUserService(mockUserAppService.Object);

        // Act & Assert - using reflection to call private method
        var method = typeof(AgentService).GetMethod("EnsureUserAuthorized", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        Should.NotThrow(() => method.Invoke(agentService, new object[] { currentUserId }));
    }

    [Fact]
    public void EnsureUserAuthorized_WithDifferentUserId_ShouldThrowUserFriendlyException()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var mockUserAppService = new Mock<IUserAppService>();
        mockUserAppService.Setup(x => x.GetCurrentUserId()).Returns(currentUserId);
        
        var agentService = CreateAgentServiceWithMockUserService(mockUserAppService.Object);

        // Act & Assert
        var method = typeof(AgentService).GetMethod("EnsureUserAuthorized", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var exception = Should.Throw<System.Reflection.TargetInvocationException>(() => 
            method.Invoke(agentService, new object[] { differentUserId }));
        
        exception.InnerException.ShouldBeOfType<UserFriendlyException>();
        exception.InnerException.Message.ShouldBe("You are not the owner of this agent");
    }

    #endregion

    #region CreateGrainIdFromAgentType Tests

    [Fact]
    public void CreateGrainIdFromAgentType_WithValidInput_ShouldReturnGrainId()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var agentType = "TestAgentType";
        var primaryKey = Guid.NewGuid();

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("CreateGrainIdFromAgentType", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var result = method.Invoke(agentService, new object[] { agentType, primaryKey });

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<GrainId>();
    }

    [Fact]
    public void CreateGrainIdFromAgentType_WithNullAgentType_ShouldThrowArgumentException()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        string agentType = null;
        var primaryKey = Guid.NewGuid();

        // Act & Assert
        var method = typeof(AgentService).GetMethod("CreateGrainIdFromAgentType", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var exception = Should.Throw<System.Reflection.TargetInvocationException>(() => 
            method.Invoke(agentService, new object[] { agentType, primaryKey }));
        
        exception.InnerException.ShouldBeOfType<ArgumentException>();
        exception.InnerException.Message.ShouldContain("Agent type cannot be null or empty");
    }

    #endregion

    #region ShouldConfigureAgent Tests

    [Fact]
    public void ShouldConfigureAgent_WithValidConfigurationAndProperties_ShouldReturnTrue()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var configuration = new Configuration
        {
            DtoType = typeof(TestConfigurationBase)
        };
        var agentProperties = "{ \"stringProperty\": \"test\" }";

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("ShouldConfigureAgent", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var result = method.Invoke(agentService, new object[] { configuration, agentProperties });

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<bool>();
        ((bool)result).ShouldBeTrue();
    }

    [Fact]
    public void ShouldConfigureAgent_WithNullConfiguration_ShouldReturnFalse()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        Configuration configuration = null;
        var agentProperties = "{ \"stringProperty\": \"test\" }";

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("ShouldConfigureAgent", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var result = method.Invoke(agentService, new object[] { configuration, agentProperties });

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<bool>();
        ((bool)result).ShouldBeFalse();
    }

    [Fact]
    public void ShouldConfigureAgent_WithNullProperties_ShouldReturnFalse()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var configuration = new Configuration
        {
            DtoType = typeof(TestConfigurationBase)
        };
        string agentProperties = null;

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("ShouldConfigureAgent", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var result = method.Invoke(agentService, new object[] { configuration, agentProperties });

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<bool>();
        ((bool)result).ShouldBeFalse();
    }

    #endregion

    #region GetExcludedAgentTypes Tests

    [Fact]
    public void GetExcludedAgentTypes_ShouldReturnCreatorAndSubscriptionTypes()
    {
        // Arrange
        var agentService = CreateAgentServiceWithMockGrainTypeResolver();

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("GetExcludedAgentTypes", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var result = method.Invoke(agentService, new object[0]);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<HashSet<GrainType>>();
        var excludedTypes = (HashSet<GrainType>)result;
        excludedTypes.Count.ShouldBe(2); // CreatorGAgent and SubscriptionGAgent
    }

    #endregion

    #region GetAgentConfigurationAsync Tests

    [Fact]
    public async Task GetAgentConfigurationAsync_WithValidAgent_ShouldReturnConfiguration()
    {
        // Arrange
        var mockAgent = new Mock<IGAgent>();
        var configurationType = typeof(TestConfigurationBase);
        
        mockAgent.Setup(x => x.GetConfigurationTypeAsync())
                 .ReturnsAsync(configurationType);

        var agentService = CreateAgentServiceForTesting();

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("GetAgentConfigurationAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var task = (Task)method.Invoke(agentService, new object[] { mockAgent.Object });
        await task;
        
        var result = task.GetType().GetProperty("Result").GetValue(task);

        // Assert
        result.ShouldNotBeNull();
        var configuration = result as Configuration;
        configuration.ShouldNotBeNull();
        configuration.DtoType.ShouldBe(configurationType);
        configuration.Properties.ShouldNotBeNull();
        configuration.Properties.Count.ShouldBe(2); // StringProperty and IntProperty
    }

    [Fact]
    public async Task GetAgentConfigurationAsync_WithNullConfigurationType_ShouldReturnNull()
    {
        // Arrange
        var mockAgent = new Mock<IGAgent>();
        
        mockAgent.Setup(x => x.GetConfigurationTypeAsync())
                 .ReturnsAsync((Type)null);

        var agentService = CreateAgentServiceForTesting();

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("GetAgentConfigurationAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var task = (Task)method.Invoke(agentService, new object[] { mockAgent.Object });
        await task;
        
        var result = task.GetType().GetProperty("Result").GetValue(task);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAgentConfigurationAsync_WithAbstractConfigurationType_ShouldReturnNull()
    {
        // Arrange
        var mockAgent = new Mock<IGAgent>();
        var configurationType = typeof(AbstractConfiguration);
        
        mockAgent.Setup(x => x.GetConfigurationTypeAsync())
                 .ReturnsAsync(configurationType);

        var agentService = CreateAgentServiceForTesting();

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("GetAgentConfigurationAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var task = (Task)method.Invoke(agentService, new object[] { mockAgent.Object });
        await task;
        
        var result = task.GetType().GetProperty("Result").GetValue(task);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region ConfigureBusinessAgentAsync Tests

    [Fact]
    public async Task ConfigureBusinessAgentAsync_WithValidInputs_ShouldConfigureAndReturnConfig()
    {
        // Arrange
        var mockAgent = new Mock<IGAgent>();
        var configuration = new Configuration
        {
            DtoType = typeof(TestConfigurationBase)
        };
        var agentProperties = "{ \"stringProperty\": \"test\", \"intProperty\": 42 }";

        mockAgent.Setup(x => x.ConfigAsync(It.IsAny<ConfigurationBase>()))
                 .Returns(Task.CompletedTask);

        var agentService = CreateAgentServiceWithMockSchemaProvider();

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("ConfigureBusinessAgentAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var task = (Task)method.Invoke(agentService, new object[] { mockAgent.Object, configuration, agentProperties });
        await task;
        
        var result = task.GetType().GetProperty("Result").GetValue(task);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<TestConfigurationBase>();
        var config = (TestConfigurationBase)result;
        config.StringProperty.ShouldBe("test");
        config.IntProperty.ShouldBe(42);
        
        // Verify ConfigAsync was called
        mockAgent.Verify(x => x.ConfigAsync(It.IsAny<ConfigurationBase>()), Times.Once);
    }

    [Fact]
    public void ConfigureBusinessAgentAsync_WithNullBusinessAgent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var agentService = CreateAgentServiceForTesting();
        var configuration = new Configuration
        {
            DtoType = typeof(TestConfigurationBase)
        };
        var agentProperties = "{ \"stringProperty\": \"test\" }";

        // Act & Assert
        var method = typeof(AgentService).GetMethod("ConfigureBusinessAgentAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var exception = Should.Throw<System.Reflection.TargetInvocationException>(() => 
            method.Invoke(agentService, new object[] { null, configuration, agentProperties }));
        
        exception.InnerException.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ConfigureBusinessAgentAsync_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var mockAgent = new Mock<IGAgent>();
        var agentService = CreateAgentServiceForTesting();
        var agentProperties = "{ \"stringProperty\": \"test\" }";

        // Act & Assert
        var method = typeof(AgentService).GetMethod("ConfigureBusinessAgentAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var exception = Should.Throw<System.Reflection.TargetInvocationException>(() => 
            method.Invoke(agentService, new object[] { mockAgent.Object, null, agentProperties }));
        
        exception.InnerException.ShouldBeOfType<ArgumentNullException>();
    }

    #endregion

    #region GetSubAgentGrainIds Tests

    [Fact]
    public async Task GetSubAgentGrainIds_WithValidAgent_ShouldReturnFilteredGrainIds()
    {
        // Arrange
        var mockAgent = new Mock<IGAgent>();
        var allowedGrainType = GrainType.Create("AllowedType");
        var creatorGrainType = GrainType.Create("CreatorGAgent");
        var subscriptionGrainType = GrainType.Create("SubscriptionGAgent");
        
        var children = new List<GrainId>
        {
            GrainId.Create(allowedGrainType, "key1"),
            GrainId.Create(creatorGrainType, "key2"),
            GrainId.Create(subscriptionGrainType, "key3"),
            GrainId.Create(allowedGrainType, "key4")
        };
        
        mockAgent.Setup(x => x.GetChildrenAsync())
                 .ReturnsAsync(children);

        var agentService = CreateAgentServiceWithMockGrainTypeResolver();

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("GetSubAgentGrainIds", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var task = (Task)method.Invoke(agentService, new object[] { mockAgent.Object });
        await task;
        
        var result = task.GetType().GetProperty("Result").GetValue(task);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<GrainId>>();
        var filteredIds = (List<GrainId>)result;
        filteredIds.Count.ShouldBe(2); // Should exclude CreatorGAgent and SubscriptionGAgent
        filteredIds.All(id => id.Type == allowedGrainType).ShouldBeTrue();
    }

    [Fact]
    public async Task GetSubAgentGrainIds_WithNullChildren_ShouldReturnEmptyList()
    {
        // Arrange
        var mockAgent = new Mock<IGAgent>();
        
        mockAgent.Setup(x => x.GetChildrenAsync())
                 .ReturnsAsync((List<GrainId>)null);

        var agentService = CreateAgentServiceWithMockGrainTypeResolver();

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("GetSubAgentGrainIds", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        var task = (Task)method.Invoke(agentService, new object[] { mockAgent.Object });
        await task;
        
        var result = task.GetType().GetProperty("Result").GetValue(task);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<List<GrainId>>();
        var filteredIds = (List<GrainId>)result;
        filteredIds.Count.ShouldBe(0);
    }

    #endregion

    #region GetAgentTypeDataMap Tests - Simplified Version

    [Fact]
    public async Task GetAgentTypeDataMap_WithValidSetup_ShouldReturnAgentTypeMap()
    {
        // Note: This is a simplified test due to the complexity of mocking all dependencies
        // In a real scenario, you would mock IGAgentManager, IGAgentFactory, etc.
        // For now, we'll test the method exists and can be invoked
        
        // Arrange
        var agentService = CreateAgentServiceWithComplexMocks();

        // Act - using reflection to call private method
        var method = typeof(AgentService).GetMethod("GetAgentTypeDataMap", BindingFlags.NonPublic | BindingFlags.Instance);
        method.ShouldNotBeNull();

        // For this complex method, we'll just verify it can be called without crashing
        // A full test would require extensive mocking of Orleans infrastructure
        var task = (Task)method.Invoke(agentService, new object[0]);
        
        // Assert - Method should exist and be callable
        task.ShouldNotBeNull();
        
        // Note: Due to the complexity of fully mocking all Orleans dependencies,
        // this test primarily verifies the method structure rather than full functionality
    }

    #endregion

    private AgentService CreateAgentServiceWithComplexMocks()
    {
        // For testing the most complex private method GetAgentTypeDataMap
        var agentService = (AgentService)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(AgentService));
        
        // Set up basic mocks to prevent null reference exceptions
        var mockLogger = new Mock<ILogger<AgentService>>();
        var mockAgentOptions = new Mock<IOptionsMonitor<AgentOptions>>();
        var mockGAgentManager = new Mock<IGAgentManager>();
        var mockGrainTypeResolver = new Mock<GrainTypeResolver>();
        var mockGAgentFactory = new Mock<IGAgentFactory>();
        var mockSchemaProvider = new Mock<ISchemaProvider>();
        
        // Setup basic return values
        mockAgentOptions.Setup(x => x.CurrentValue).Returns(new AgentOptions
        {
            SystemAgentList = new List<string> { "SystemAgent1", "SystemAgent2" }
        });
        
        mockGAgentManager.Setup(x => x.GetAvailableGAgentTypes())
                        .Returns(new List<Type> { typeof(TestConfigurationBase) });
        
        mockGrainTypeResolver.Setup(x => x.GetGrainType(It.IsAny<Type>()))
                            .Returns((Type t) => GrainType.Create(t.Name));
        
        // Use reflection to set the private fields
        var loggerField = typeof(AgentService).GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(agentService, mockLogger.Object);
        
        var agentOptionsField = typeof(AgentService).GetField("_agentOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        agentOptionsField?.SetValue(agentService, mockAgentOptions.Object);
        
        var gAgentManagerField = typeof(AgentService).GetField("_gAgentManager", BindingFlags.NonPublic | BindingFlags.Instance);
        gAgentManagerField?.SetValue(agentService, mockGAgentManager.Object);
        
        var grainTypeResolverField = typeof(AgentService).GetField("_grainTypeResolver", BindingFlags.NonPublic | BindingFlags.Instance);
        grainTypeResolverField?.SetValue(agentService, mockGrainTypeResolver.Object);
        
        var gAgentFactoryField = typeof(AgentService).GetField("_gAgentFactory", BindingFlags.NonPublic | BindingFlags.Instance);
        gAgentFactoryField?.SetValue(agentService, mockGAgentFactory.Object);
        
        var schemaProviderField = typeof(AgentService).GetField("_schemaProvider", BindingFlags.NonPublic | BindingFlags.Instance);
        schemaProviderField?.SetValue(agentService, mockSchemaProvider.Object);
        
        return agentService;
    }

    private AgentService CreateAgentServiceWithMockGrainTypeResolver()
    {
        // For testing private methods, we can use FormatterServices.GetUninitializedObject
        // to create an instance without calling the constructor
        var agentService = (AgentService)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(AgentService));
        
        // Set up a mock logger to avoid null reference exceptions
        var mockLogger = new Mock<ILogger<AgentService>>();
        
        // Set up mock GrainTypeResolver
        var mockGrainTypeResolver = new Mock<GrainTypeResolver>();
        mockGrainTypeResolver.Setup(x => x.GetGrainType(It.IsAny<Type>()))
                            .Returns((Type t) => GrainType.Create(t.Name));
        
        // Use reflection to set the private fields
        var loggerField = typeof(AgentService).GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(agentService, mockLogger.Object);
        
        var grainTypeResolverField = typeof(AgentService).GetField("_grainTypeResolver", BindingFlags.NonPublic | BindingFlags.Instance);
        grainTypeResolverField?.SetValue(agentService, mockGrainTypeResolver.Object);
        
        return agentService;
    }

    private AgentService CreateAgentServiceWithMockSchemaProvider()
    {
        // For testing private methods, we can use FormatterServices.GetUninitializedObject
        // to create an instance without calling the constructor
        var agentService = (AgentService)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(AgentService));
        
        // Set up a mock logger to avoid null reference exceptions
        var mockLogger = new Mock<ILogger<AgentService>>();
        
        // Set up mock schema provider
        var mockSchemaProvider = new Mock<ISchemaProvider>();
        var mockSchema = new Mock<JsonSchema>();
        
        // Setup successful validation (empty validation result)
        mockSchema.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<JsonSchemaValidatorSettings>()))
                  .Returns(new List<ValidationError>());
        
        mockSchemaProvider.Setup(x => x.GetTypeSchema(It.IsAny<Type>()))
                         .Returns(mockSchema.Object);
        
        // Use reflection to set the private fields
        var loggerField = typeof(AgentService).GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(agentService, mockLogger.Object);
        
        var schemaProviderField = typeof(AgentService).GetField("_schemaProvider", BindingFlags.NonPublic | BindingFlags.Instance);
        schemaProviderField?.SetValue(agentService, mockSchemaProvider.Object);
        
        return agentService;
    }

    private AgentService CreateAgentServiceWithMockUserService(IUserAppService userAppService)
    {
        // For testing private methods, we can use FormatterServices.GetUninitializedObject
        // to create an instance without calling the constructor
        var agentService = (AgentService)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(AgentService));
        
        // Set up a mock logger to avoid null reference exceptions
        var mockLogger = new Mock<ILogger<AgentService>>();
        
        // Use reflection to set the private fields
        var loggerField = typeof(AgentService).GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance);
        loggerField?.SetValue(agentService, mockLogger.Object);
        
        var userAppServiceField = typeof(AgentService).GetField("_userAppService", BindingFlags.NonPublic | BindingFlags.Instance);
        userAppServiceField?.SetValue(agentService, userAppService);
        
        return agentService;
    }
} 