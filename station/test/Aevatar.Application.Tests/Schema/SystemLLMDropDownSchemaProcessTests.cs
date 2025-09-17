using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aevatar.Application.Tests;
using Aevatar.GAgents.Basic;
using Aevatar.Options;
using Aevatar.Schema;
using NJsonSchema;
using NJsonSchema.Generation;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Schema;

/// <summary>
/// Comprehensive unit tests for SystemLLMDropDownSchemaProcess class
/// Tests all major functionality to achieve 80%+ code coverage
/// </summary>
public class SystemLLMDropDownSchemaProcessTests : AevatarApplicationTestBase
{
    private readonly SystemLLMDropDownSchemaProcess _processor;

    public SystemLLMDropDownSchemaProcessTests()
    {
        _processor = GetRequiredService<SystemLLMDropDownSchemaProcess>();
    }

    #region Test DTOs

    /// <summary>
    /// Test DTO with SystemLLM property (exact case)
    /// </summary>
    public class TestDtoExactCase
    {
        [DynamicDropDown("SystemLLMConfigs")]
        public string SystemLLM { get; set; } = "";
    }

    /// <summary>
    /// Test DTO with systemLLM property (camelCase)
    /// </summary>
    public class TestDtoCamelCase
    {
        [DynamicDropDown("SystemLLMConfigs")]
        public string systemLLM { get; set; } = "";
    }

    /// <summary>
    /// Test DTO with systemllm property (lowercase)
    /// </summary>
    public class TestDtoLowerCase
    {
        [DynamicDropDown("SystemLLMConfigs")]
        public string systemllm { get; set; } = "";
    }

    /// <summary>
    /// Test DTO without SystemLLM property
    /// </summary>
    public class TestDtoNoSystemLLM
    {
        [DynamicDropDown("SystemLLMConfigs")]
        public string OtherProperty { get; set; } = "";
    }

    /// <summary>
    /// Test DTO with multiple properties
    /// </summary>
    public class TestDtoMultipleProperties
    {
        [DynamicDropDown("SystemLLMConfigs")]
        public string SystemLLM { get; set; } = "";
        
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    #endregion

    #region Helper Methods

    private SchemaProcessorContext CreateTestContext(JsonSchema schema)
    {
        // Create a test context with minimal required parameters
        var settings = new SystemTextJsonSchemaGeneratorSettings();
        var generator = new JsonSchemaGenerator(settings);
        var resolver = new JsonSchemaResolver(schema, settings);
        
        // Use reflection to create SchemaProcessorContext with proper parameters
        var contextualType = new JsonSchema().ToContextualType();
        
        return new SchemaProcessorContext(
            contextualType,
            schema,
            resolver,
            generator,
            settings
        );
    }

    private DynamicDropDownContext CreateTestDropDownContext(List<SystemLLMConfigDto>? configs = null)
    {
        configs ??= new List<SystemLLMConfigDto>
        {
            new() { Name = "gpt-4", Strengths = "Advanced reasoning capabilities", BestFor = "Complex problem solving" },
            new() { Name = "claude-3", Strengths = "Creative writing and analysis", BestFor = "Content creation" },
            new() { Name = "gemini-pro", Strengths = "Multimodal understanding", BestFor = "Visual and text tasks" }
        };

        return new DynamicDropDownContext
        {
            AdditionalData = new Dictionary<string, object>
            {
                ["SystemLLMConfigs"] = configs
            }
        };
    }

    #endregion

    #region OptionName Tests

    [Fact]
    public void OptionName_ShouldReturnCorrectValue()
    {
        // Act & Assert
        _processor.OptionName.ShouldBe("SystemLLMConfigs");
    }

    #endregion

    #region ProcessSchema Tests

    [Fact]
    public void ProcessSchema_WithExactCaseProperty_ShouldEnhanceSchema()
    {
        // Arrange
        var schema = JsonSchema.FromType<TestDtoExactCase>();
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoExactCase).GetProperty("SystemLLM")!;
        var dropDownContext = CreateTestDropDownContext();

        // Act
        _processor.ProcessSchema(context, property, dropDownContext);

        // Assert
        schema.Properties.ShouldContainKey("systemLLM"); // JSON schema uses camelCase
        var propertySchema = schema.Properties["systemLLM"];
        
        propertySchema.ExtensionData.ShouldNotBeNull();
        propertySchema.ExtensionData.ShouldContainKey("x-descriptions");
        propertySchema.ExtensionData.ShouldContainKey("x-enumNames");
        propertySchema.ExtensionData.ShouldContainKey("enum");
    }

    [Fact]
    public void ProcessSchema_WithCamelCaseProperty_ShouldEnhanceSchema()
    {
        // Arrange
        var schema = JsonSchema.FromType<TestDtoCamelCase>();
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoCamelCase).GetProperty("systemLLM")!;
        var dropDownContext = CreateTestDropDownContext();

        // Act
        _processor.ProcessSchema(context, property, dropDownContext);

        // Assert
        schema.Properties.ShouldContainKey("systemLLM");
        var propertySchema = schema.Properties["systemLLM"];
        
        propertySchema.ExtensionData.ShouldNotBeNull();
        propertySchema.ExtensionData.ShouldContainKey("x-descriptions");
        propertySchema.ExtensionData.ShouldContainKey("x-enumNames");
        propertySchema.ExtensionData.ShouldContainKey("enum");
    }

    [Fact]
    public void ProcessSchema_WithLowerCaseProperty_ShouldEnhanceSchema()
    {
        // Arrange
        var schema = JsonSchema.FromType<TestDtoLowerCase>();
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoLowerCase).GetProperty("systemllm")!;
        var dropDownContext = CreateTestDropDownContext();

        // Act
        _processor.ProcessSchema(context, property, dropDownContext);

        // Assert
        schema.Properties.ShouldContainKey("systemllm");
        var propertySchema = schema.Properties["systemllm"];
        
        propertySchema.ExtensionData.ShouldNotBeNull();
        propertySchema.ExtensionData.ShouldContainKey("x-descriptions");
        propertySchema.ExtensionData.ShouldContainKey("x-enumNames");
        propertySchema.ExtensionData.ShouldContainKey("enum");
    }

    [Fact]
    public void ProcessSchema_WithNoMatchingProperty_ShouldNotEnhanceSchema()
    {
        // Arrange
        var schema = JsonSchema.FromType<TestDtoNoSystemLLM>();
        var originalExtensionDataCount = schema.Properties.Values
            .Where(p => p.ExtensionData != null)
            .Sum(p => p.ExtensionData!.Count);
        
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoNoSystemLLM).GetProperty("OtherProperty")!;
        var dropDownContext = CreateTestDropDownContext();

        // Act
        _processor.ProcessSchema(context, property, dropDownContext);

        // Assert
        // Should not have enhanced any properties since there's no SystemLLM property
        var newExtensionDataCount = schema.Properties.Values
            .Where(p => p.ExtensionData != null)
            .Sum(p => p.ExtensionData!.Count);
        
        newExtensionDataCount.ShouldBe(originalExtensionDataCount);
    }

    [Fact]
    public void ProcessSchema_WithEmptySchema_ShouldNotThrow()
    {
        // Arrange
        var schema = new JsonSchema();
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoExactCase).GetProperty("SystemLLM")!;
        var dropDownContext = CreateTestDropDownContext();

        // Act & Assert - Should not throw
        Should.NotThrow(() => _processor.ProcessSchema(context, property, dropDownContext));
    }

    [Fact]
    public void ProcessSchema_WithNullProperties_ShouldNotThrow()
    {
        // Arrange
        var schema = new JsonSchema { Properties = null };
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoExactCase).GetProperty("SystemLLM")!;
        var dropDownContext = CreateTestDropDownContext();

        // Act & Assert - Should not throw
        Should.NotThrow(() => _processor.ProcessSchema(context, property, dropDownContext));
    }

    [Fact]
    public void ProcessSchema_WithNullDropDownContext_ShouldNotEnhanceSchema()
    {
        // Arrange
        var schema = JsonSchema.FromType<TestDtoExactCase>();
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoExactCase).GetProperty("SystemLLM")!;

        // Act
        _processor.ProcessSchema(context, property, null);

        // Assert
        var propertySchema = schema.Properties["systemLLM"];
        if (propertySchema.ExtensionData != null)
        {
            propertySchema.ExtensionData.ShouldNotContainKey("x-descriptions");
            propertySchema.ExtensionData.ShouldNotContainKey("x-enumNames");
            propertySchema.ExtensionData.ShouldNotContainKey("enum");
        }
    }

    #endregion

    #region InjectSystemLLMConfigurations Tests

    [Fact]
    public void ProcessSchema_WithValidConfigurations_ShouldInjectCorrectData()
    {
        // Arrange
        var testConfigs = new List<SystemLLMConfigDto>
        {
            new() { Name = "test-model-1", Strengths = "Fast processing", BestFor = "Quick tasks" },
            new() { Name = "test-model-2", Strengths = "High accuracy", BestFor = "Precise work" }
        };
        
        var schema = JsonSchema.FromType<TestDtoExactCase>();
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoExactCase).GetProperty("SystemLLM")!;
        var dropDownContext = CreateTestDropDownContext(testConfigs);

        // Act
        _processor.ProcessSchema(context, property, dropDownContext);

        // Assert
        var propertySchema = schema.Properties["systemLLM"];
        propertySchema.ExtensionData.ShouldNotBeNull();
        
        // Check x-descriptions
        propertySchema.ExtensionData.ShouldContainKey("x-descriptions");
        var descriptions = propertySchema.ExtensionData["x-descriptions"] as List<object>;
        descriptions.ShouldNotBeNull();
        descriptions.Count.ShouldBe(2);
        
        // Check x-enumNames
        propertySchema.ExtensionData.ShouldContainKey("x-enumNames");
        var enumNames = propertySchema.ExtensionData["x-enumNames"] as string[];
        enumNames.ShouldNotBeNull();
        enumNames.Length.ShouldBe(2);
        enumNames[0].ShouldBe("test-model-1");
        enumNames[1].ShouldBe("test-model-2");
        
        // Check enum
        propertySchema.ExtensionData.ShouldContainKey("enum");
        var enumValues = propertySchema.ExtensionData["enum"] as string[];
        enumValues.ShouldNotBeNull();
        enumValues.Length.ShouldBe(2);
        enumValues[0].ShouldBe("test-model-1");
        enumValues[1].ShouldBe("test-model-2");
    }

    [Fact]
    public void ProcessSchema_WithEmptyConfigurations_ShouldNotInjectData()
    {
        // Arrange
        var schema = JsonSchema.FromType<TestDtoExactCase>();
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoExactCase).GetProperty("SystemLLM")!;
        var dropDownContext = CreateTestDropDownContext(new List<SystemLLMConfigDto>());

        // Act
        _processor.ProcessSchema(context, property, dropDownContext);

        // Assert
        var propertySchema = schema.Properties["systemLLM"];
        if (propertySchema.ExtensionData != null)
        {
            propertySchema.ExtensionData.ShouldNotContainKey("x-descriptions");
            propertySchema.ExtensionData.ShouldNotContainKey("x-enumNames");
            propertySchema.ExtensionData.ShouldNotContainKey("enum");
        }
    }

    [Fact]
    public void ProcessSchema_WithMissingConfigurationKey_ShouldNotInjectData()
    {
        // Arrange
        var schema = JsonSchema.FromType<TestDtoExactCase>();
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoExactCase).GetProperty("SystemLLM")!;
        var dropDownContext = new DynamicDropDownContext
        {
            AdditionalData = new Dictionary<string, object>
            {
                ["OtherKey"] = new List<SystemLLMConfigDto>() // Wrong key
            }
        };

        // Act
        _processor.ProcessSchema(context, property, dropDownContext);

        // Assert
        var propertySchema = schema.Properties["systemLLM"];
        if (propertySchema.ExtensionData != null)
        {
            propertySchema.ExtensionData.ShouldNotContainKey("x-descriptions");
            propertySchema.ExtensionData.ShouldNotContainKey("x-enumNames");
            propertySchema.ExtensionData.ShouldNotContainKey("enum");
        }
    }

    [Fact]
    public void ProcessSchema_WithWrongConfigurationType_ShouldNotInjectData()
    {
        // Arrange
        var schema = JsonSchema.FromType<TestDtoExactCase>();
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoExactCase).GetProperty("SystemLLM")!;
        var dropDownContext = new DynamicDropDownContext
        {
            AdditionalData = new Dictionary<string, object>
            {
                ["SystemLLMConfigs"] = "wrong-type" // Wrong type
            }
        };

        // Act
        _processor.ProcessSchema(context, property, dropDownContext);

        // Assert
        var propertySchema = schema.Properties["systemLLM"];
        if (propertySchema.ExtensionData != null)
        {
            propertySchema.ExtensionData.ShouldNotContainKey("x-descriptions");
            propertySchema.ExtensionData.ShouldNotContainKey("x-enumNames");
            propertySchema.ExtensionData.ShouldNotContainKey("enum");
        }
    }

    [Fact]
    public void ProcessSchema_WithExistingExtensionData_ShouldPreserveExisting()
    {
        // Arrange
        var schema = JsonSchema.FromType<TestDtoExactCase>();
        var propertySchema = schema.Properties["systemLLM"];
        propertySchema.ExtensionData = new Dictionary<string, object?>
        {
            ["existing-key"] = "existing-value"
        };
        
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoExactCase).GetProperty("SystemLLM")!;
        var dropDownContext = CreateTestDropDownContext();

        // Act
        _processor.ProcessSchema(context, property, dropDownContext);

        // Assert
        propertySchema.ExtensionData.ShouldNotBeNull();
        propertySchema.ExtensionData.ShouldContainKey("existing-key");
        propertySchema.ExtensionData["existing-key"].ShouldBe("existing-value");
        
        // Should also have new keys
        propertySchema.ExtensionData.ShouldContainKey("x-descriptions");
        propertySchema.ExtensionData.ShouldContainKey("x-enumNames");
        propertySchema.ExtensionData.ShouldContainKey("enum");
    }

    #endregion

    #region Property Name Matching Tests

    [Fact]
    public void ProcessSchema_ShouldTestAllNamingConventions()
    {
        // This test verifies that the processor tries different naming conventions
        
        // Test data with various naming patterns
        var testCases = new[]
        {
            (typeof(TestDtoExactCase), "SystemLLM", "systemLLM"),
            (typeof(TestDtoCamelCase), "systemLLM", "systemLLM"),
            (typeof(TestDtoLowerCase), "systemllm", "systemllm")
        };

        foreach (var (dtoType, propertyName, expectedKey) in testCases)
        {
            // Arrange
            var schema = JsonSchema.FromType(dtoType);
            var context = CreateTestContext(schema);
            var property = dtoType.GetProperty(propertyName)!;
            var dropDownContext = CreateTestDropDownContext();

            // Act
            _processor.ProcessSchema(context, property, dropDownContext);

            // Assert
            schema.Properties.ShouldContainKey(expectedKey, 
                $"Should find property with key '{expectedKey}' for type {dtoType.Name}");
                
            var propertySchema = schema.Properties[expectedKey];
            propertySchema.ExtensionData.ShouldNotBeNull(
                $"ExtensionData should be added for property '{expectedKey}' in type {dtoType.Name}");
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ProcessSchema_CompleteIntegration_ShouldWorkEndToEnd()
    {
        // Arrange
        var schema = JsonSchema.FromType<TestDtoMultipleProperties>();
        var context = CreateTestContext(schema);
        var property = typeof(TestDtoMultipleProperties).GetProperty("SystemLLM")!;
        var dropDownContext = CreateTestDropDownContext();

        // Act
        _processor.ProcessSchema(context, property, dropDownContext);

        // Assert
        schema.Properties.ShouldContainKey("systemLLM");
        schema.Properties.ShouldContainKey("name");
        schema.Properties.ShouldContainKey("age");
        
        // Only SystemLLM should be enhanced
        var systemLLMProperty = schema.Properties["systemLLM"];
        systemLLMProperty.ExtensionData.ShouldNotBeNull();
        systemLLMProperty.ExtensionData.Count.ShouldBeGreaterThan(0);
        
        // Other properties should not be enhanced
        var nameProperty = schema.Properties["name"];
        var ageProperty = schema.Properties["age"];
        
        (nameProperty.ExtensionData?.Count ?? 0).ShouldBe(0);
        (ageProperty.ExtensionData?.Count ?? 0).ShouldBe(0);
    }

    #endregion
}
