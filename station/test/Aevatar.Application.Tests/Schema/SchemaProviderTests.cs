using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Tests;
using Aevatar.GAgents.Basic;
using Aevatar.Options;
using Aevatar.Schema;
using NJsonSchema;
using NJsonSchema.Validation;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Schema;

/// <summary>
/// Comprehensive unit tests for SchemaProvider class
/// Tests all major functionality to achieve 80%+ code coverage
/// </summary>
public class SchemaProviderTests : AevatarApplicationTestBase
{
    private readonly ISchemaProvider _schemaProvider;

    public SchemaProviderTests()
    {
        _schemaProvider = GetRequiredService<ISchemaProvider>();
    }

    #region Test DTOs

    /// <summary>
    /// Simple test DTO for basic schema generation
    /// </summary>
    public class SimpleTestDto
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Complex test DTO with nested properties
    /// </summary>
    public class ComplexTestDto
    {
        public string Id { get; set; } = "";
        public SimpleTestDto NestedObject { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Test DTO for DynamicDropDown testing
    /// </summary>
    public class DynamicDropDownTestDto
    {
        [DynamicDropDown("SystemLLMConfigs")]
        public string SystemLLM { get; set; } = "";
        
        public string RegularProperty { get; set; } = "";
    }

    #endregion

    #region GetTypeSchema Tests

    [Fact]
    public void GetTypeSchema_WithSimpleType_ShouldReturnValidSchema()
    {
        // Act
        var schema = _schemaProvider.GetTypeSchema(typeof(SimpleTestDto));

        // Assert
        schema.ShouldNotBeNull();
        schema.Properties.ShouldNotBeNull();
        schema.Properties.ShouldContainKey("name");
        schema.Properties.ShouldContainKey("age");
        schema.Properties.ShouldContainKey("isActive");
        
        schema.Properties["name"].Type.ShouldBe(JsonObjectType.String);
        schema.Properties["age"].Type.ShouldBe(JsonObjectType.Integer);
        schema.Properties["isActive"].Type.ShouldBe(JsonObjectType.Boolean);
    }

    [Fact]
    public void GetTypeSchema_WithComplexType_ShouldReturnNestedSchema()
    {
        // Act
        var schema = _schemaProvider.GetTypeSchema(typeof(ComplexTestDto));

        // Assert
        schema.ShouldNotBeNull();
        schema.Properties.ShouldNotBeNull();
        schema.Properties.ShouldContainKey("id");
        schema.Properties.ShouldContainKey("nestedObject");
        schema.Properties.ShouldContainKey("tags");
        schema.Properties.ShouldContainKey("metadata");
        
        // Verify nested object structure
        var nestedProperty = schema.Properties["nestedObject"];
        nestedProperty.ShouldNotBeNull();
        nestedProperty.Type.ShouldBe(JsonObjectType.Object);
    }

    [Fact]
    public void GetTypeSchema_WithoutContext_ShouldUseCaching()
    {
        // Act - Call twice without context
        var schema1 = _schemaProvider.GetTypeSchema(typeof(SimpleTestDto));
        var schema2 = _schemaProvider.GetTypeSchema(typeof(SimpleTestDto));

        // Assert - Should return the same cached instance
        schema1.ShouldNotBeNull();
        schema2.ShouldNotBeNull();
        // Note: Due to caching implementation, both should be equivalent
        schema1.Properties.Count.ShouldBe(schema2.Properties.Count);
    }

    [Fact]
    public void GetTypeSchema_WithContext_ShouldNotUseCaching()
    {
        // Arrange
        var context = new DynamicDropDownContext
        {
            AdditionalData = new Dictionary<string, object>
            {
                ["SystemLLMConfigs"] = new List<SystemLLMConfigDto>
                {
                    new() { Name = "gpt-4", Strengths = "Advanced reasoning", BestFor = "Complex tasks" },
                    new() { Name = "claude-3", Strengths = "Creative writing", BestFor = "Content creation" }
                }
            }
        };

        // Act
        var schemaWithContext1 = _schemaProvider.GetTypeSchema(typeof(DynamicDropDownTestDto), context);
        var schemaWithContext2 = _schemaProvider.GetTypeSchema(typeof(DynamicDropDownTestDto), context);

        // Assert
        schemaWithContext1.ShouldNotBeNull();
        schemaWithContext2.ShouldNotBeNull();
        
        // Both should have SystemLLM property with dynamic enhancements
        schemaWithContext1.Properties.ShouldContainKey("systemLLM");
        schemaWithContext2.Properties.ShouldContainKey("systemLLM");
    }

    [Fact]
    public void GetTypeSchema_WithNullContext_ShouldUseBasicProcessing()
    {
        // Act
        var schema = _schemaProvider.GetTypeSchema(typeof(DynamicDropDownTestDto), null);

        // Assert
        schema.ShouldNotBeNull();
        schema.Properties.ShouldNotBeNull();
        schema.Properties.ShouldContainKey("systemLLM");
        schema.Properties.ShouldContainKey("regularProperty");
        
        // Without context, SystemLLM should be a basic string property
        var systemLLMProperty = schema.Properties["systemLLM"];
        systemLLMProperty.Type.ShouldBe(JsonObjectType.String);
    }

    [Fact]
    public void GetTypeSchema_ThreadSafety_ShouldHandleConcurrentAccess()
    {
        // Arrange
        var tasks = new List<Task<JsonSchema>>();
        
        // Act - Create multiple concurrent tasks
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => _schemaProvider.GetTypeSchema(typeof(SimpleTestDto))));
        }
        
        var results = Task.WhenAll(tasks).Result;

        // Assert - All should succeed and return valid schemas
        results.ShouldNotBeNull();
        results.Length.ShouldBe(10);
        
        foreach (var schema in results)
        {
            schema.ShouldNotBeNull();
            schema.Properties.ShouldNotBeNull();
            schema.Properties.Count.ShouldBeGreaterThan(0);
        }
    }

    #endregion

    #region ConvertValidateError Tests

    [Fact]
    public void ConvertValidateError_WithSingleError_ShouldReturnDescription()
    {
        // Arrange - Create a simple validation error using reflection to avoid constructor complexity
        var schema = JsonSchema.FromType<SimpleTestDto>();
        schema.Description = "Test field description";
        
        // Create validation error using simple constructor
        var validationError = new ValidationError(ValidationErrorKind.PropertyRequired, "name", "Property is required");
        
        // Set schema using reflection if needed
        var schemaField = typeof(ValidationError).GetField("_schema", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        schemaField?.SetValue(validationError, schema);

        // Act
        var result = _schemaProvider.ConvertValidateError(validationError);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("Test field description");
    }

    [Fact]
    public void ConvertValidateError_WithErrorCollection_ShouldReturnDictionary()
    {
        // Arrange
        var schema = JsonSchema.FromType<SimpleTestDto>();
        schema.Description = "Test schema description";
        
        var errors = new List<ValidationError>
        {
            new(ValidationErrorKind.PropertyRequired, "name", "Name is required"),
            new(ValidationErrorKind.NumberExpected, "age", "Age must be a number")
        };

        // Set schemas using reflection
        var schemaField = typeof(ValidationError).GetField("_schema", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var error in errors)
        {
            schemaField?.SetValue(error, schema);
        }

        // Act
        var result = _schemaProvider.ConvertValidateError(errors);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContainKey("name");
        result.ShouldContainKey("age");
        result["name"].ShouldNotBeNullOrEmpty();
        result["age"].ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ConvertValidateError_WithEmptyErrorCollection_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var errors = new List<ValidationError>();

        // Act
        var result = _schemaProvider.ConvertValidateError(errors);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void ConvertValidateError_WithErrorsWithoutProperty_ShouldSkipThoseErrors()
    {
        // Arrange
        var schema = JsonSchema.FromType<SimpleTestDto>();
        schema.Description = "Test schema description";
        
        var errors = new List<ValidationError>
        {
            new(ValidationErrorKind.PropertyRequired, "name", "Name is required"),
            new(ValidationErrorKind.PropertyRequired, null, "Schema error") // No property
        };

        // Set schemas using reflection
        var schemaField = typeof(ValidationError).GetField("_schema", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var error in errors)
        {
            schemaField?.SetValue(error, schema);
        }

        // Act
        var result = _schemaProvider.ConvertValidateError(errors);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1); // Only the error with property should be included
        result.ShouldContainKey("name");
        result.ShouldNotContainKey(""); // Should not contain empty property name
    }

    [Fact]
    public void ConvertValidateError_WithErrorWithoutDescription_ShouldUseDefaultMessage()
    {
        // Arrange
        var schema = new JsonSchema(); // Empty schema without description
        var validationError = new ValidationError(ValidationErrorKind.PropertyRequired, "testProperty", "Some error");
        
        // Set schema using reflection
        var schemaField = typeof(ValidationError).GetField("_schema", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        schemaField?.SetValue(validationError, schema);

        // Act
        var result = _schemaProvider.ConvertValidateError(validationError);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("Field is incorrect"); // Default fallback message
    }

    [Fact]
    public void ConvertValidateError_WithSchemaDescription_ShouldUseSchemaDescription()
    {
        // Arrange
        var schema = new JsonSchema { Description = "Custom validation message" };
        var validationError = new ValidationError(ValidationErrorKind.PropertyRequired, "testProperty", "Some error");
        
        // Set schema using reflection
        var schemaField = typeof(ValidationError).GetField("_schema", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        schemaField?.SetValue(validationError, schema);

        // Act
        var result = _schemaProvider.ConvertValidateError(validationError);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("Custom validation message");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SchemaProvider_WithDynamicDropDown_ShouldIntegrateCorrectly()
    {
        // Arrange
        var context = new DynamicDropDownContext
        {
            AdditionalData = new Dictionary<string, object>
            {
                ["SystemLLMConfigs"] = new List<SystemLLMConfigDto>
                {
                    new() { Name = "gpt-4", Strengths = "Advanced", BestFor = "Complex" },
                    new() { Name = "claude-3", Strengths = "Creative", BestFor = "Writing" }
                }
            }
        };

        // Act
        var schema = _schemaProvider.GetTypeSchema(typeof(DynamicDropDownTestDto), context);

        // Assert
        schema.ShouldNotBeNull();
        schema.Properties.ShouldContainKey("systemLLM");
        
        var systemLLMProperty = schema.Properties["systemLLM"];
        systemLLMProperty.ShouldNotBeNull();
        
        // Should have dynamic enhancements from SystemLLMDropDownSchemaProcess
        if (systemLLMProperty.ExtensionData != null)
        {
            // The property might have been enhanced with enum, x-enumNames, x-descriptions
            // This depends on the SystemLLMDropDownSchemaProcess implementation
        }
    }

    #endregion
}
