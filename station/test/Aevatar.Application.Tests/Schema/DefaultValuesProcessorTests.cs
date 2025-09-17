using System.Collections.Generic;
using System.Linq;
using Aevatar.GAgents.AI.Common;
using Aevatar.Schema;
using Shouldly;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Aevatar.Application.Tests;
using NJsonSchema;

namespace Aevatar.Application.Tests.Schema;

/// <summary>
/// Integration tests for DefaultValuesProcessor
/// Verifies that x-descriptions are correctly added to JSON schema
/// </summary>
public class DefaultValuesProcessorTests : AevatarApplicationTestBase
{
    private readonly ISchemaProvider _schemaProvider;

    public DefaultValuesProcessorTests()
    {
        _schemaProvider = GetRequiredService<ISchemaProvider>();
    }

    /// <summary>
    /// Test DTO with DefaultValues containing descriptions
    /// </summary>
    public class TestConfigWithDescriptions
    {
        [DefaultValues(
            new object[] { "gpt-4", "claude-3", "gemini-pro" },
            new string[] { "OpenAI GPT-4 - Advanced reasoning", "Anthropic Claude 3 - Creative writing", "Google Gemini Pro - Multilingual support" }
        )]
        public string ModelName { get; set; } = "gpt-4";

        [DefaultValues(
            new object[] { "json", "xml" },
            new string[] { "JSON format - Structured data", "XML format - Hierarchical markup" }
        )]
        public string OutputFormat { get; set; } = "json";

        // Property without DefaultValues attribute
        public string RegularProperty { get; set; } = "default";
    }

    /// <summary>
    /// Test DTO with empty descriptions
    /// </summary>
    public class TestConfigWithEmptyDescriptions
    {
        [DefaultValues(
            new object[] { "option1", "option2" },
            new string[] { "", "" }
        )]
        public string EmptyDescriptions { get; set; } = "option1";
    }

    /// <summary>
    /// Test DTO with single value and description
    /// </summary>
    public class TestConfigWithSingleDescription
    {
        [DefaultValues(
            new object[] { "single-value" },
            new string[] { "This is the only option" }
        )]
        public string SingleValue { get; set; } = "single-value";
    }

    [Fact]
    public void SchemaProvider_WithDefaultValuesAndDescriptions_ShouldAddAllSchemaExtensions()
    {
        // Arrange & Act
        var schema = _schemaProvider.GetTypeSchema(typeof(TestConfigWithDescriptions));

        // Assert
        schema.ShouldNotBeNull();
        schema.Properties.ShouldNotBeNull();

        // Check ModelName property
        schema.Properties.ShouldContainKey("modelName");
        var modelNameProperty = schema.Properties["modelName"];
        modelNameProperty.ExtensionData.ShouldNotBeNull();
        
        // Should have enum values
        modelNameProperty.ExtensionData.ShouldContainKey("enum");
        var enumValues = modelNameProperty.ExtensionData["enum"] as string[];
        enumValues.ShouldNotBeNull();
        enumValues.Length.ShouldBe(3);
        enumValues[0].ShouldBe("gpt-4");
        enumValues[1].ShouldBe("claude-3");
        enumValues[2].ShouldBe("gemini-pro");
        
        // Should have x-enumNames
        modelNameProperty.ExtensionData.ShouldContainKey("x-enumNames");
        var enumNames = modelNameProperty.ExtensionData["x-enumNames"] as string[];
        enumNames.ShouldNotBeNull();
        enumNames.Length.ShouldBe(3);
        enumNames.ShouldBe(enumValues); // Should match enum values
        
        // Should have default value (first enum value)
        modelNameProperty.ExtensionData.ShouldContainKey("default");
        var defaultValue = modelNameProperty.ExtensionData["default"] as string;
        defaultValue.ShouldNotBeNull();
        defaultValue.ShouldBe("gpt-4");
        
        // Should have x-descriptions
        modelNameProperty.ExtensionData.ShouldContainKey("x-descriptions");
        var descriptions = modelNameProperty.ExtensionData["x-descriptions"] as string[];
        descriptions.ShouldNotBeNull();
        descriptions.Length.ShouldBe(3);
        descriptions[0].ShouldBe("OpenAI GPT-4 - Advanced reasoning");
        descriptions[1].ShouldBe("Anthropic Claude 3 - Creative writing");
        descriptions[2].ShouldBe("Google Gemini Pro - Multilingual support");

        // Check OutputFormat property
        schema.Properties.ShouldContainKey("outputFormat");
        var outputFormatProperty = schema.Properties["outputFormat"];
        outputFormatProperty.ExtensionData.ShouldNotBeNull();
        
        // Should have enum values
        outputFormatProperty.ExtensionData.ShouldContainKey("enum");
        var outputEnumValues = outputFormatProperty.ExtensionData["enum"] as string[];
        outputEnumValues.ShouldNotBeNull();
        outputEnumValues.Length.ShouldBe(2);
        outputEnumValues[0].ShouldBe("json");
        outputEnumValues[1].ShouldBe("xml");
        
        // Should have x-enumNames
        outputFormatProperty.ExtensionData.ShouldContainKey("x-enumNames");
        
        // Should have default value
        outputFormatProperty.ExtensionData.ShouldContainKey("default");
        var outputDefaultValue = outputFormatProperty.ExtensionData["default"] as string;
        outputDefaultValue.ShouldNotBeNull();
        outputDefaultValue.ShouldBe("json");
        
        // Should have x-descriptions
        outputFormatProperty.ExtensionData.ShouldContainKey("x-descriptions");
        var outputDescriptions = outputFormatProperty.ExtensionData["x-descriptions"] as string[];
        outputDescriptions.ShouldNotBeNull();
        outputDescriptions.Length.ShouldBe(2);
        outputDescriptions[0].ShouldBe("JSON format - Structured data");
        outputDescriptions[1].ShouldBe("XML format - Hierarchical markup");

        // Check RegularProperty (should not have any extension data)
        schema.Properties.ShouldContainKey("regularProperty");
        var regularProperty = schema.Properties["regularProperty"];
        if (regularProperty.ExtensionData != null)
        {
            regularProperty.ExtensionData.ShouldNotContainKey("x-descriptions");
            regularProperty.ExtensionData.ShouldNotContainKey("enum");
            regularProperty.ExtensionData.ShouldNotContainKey("x-enumNames");
            regularProperty.ExtensionData.ShouldNotContainKey("default");
        }
    }

    [Fact]
    public void SchemaProvider_WithEmptyDescriptions_ShouldAddEnumButNotDescriptions()
    {
        // Arrange & Act
        var schema = _schemaProvider.GetTypeSchema(typeof(TestConfigWithEmptyDescriptions));

        // Assert
        schema.ShouldNotBeNull();
        schema.Properties.ShouldNotBeNull();
        schema.Properties.ShouldContainKey("emptyDescriptions");
        
        var property = schema.Properties["emptyDescriptions"];
        property.ExtensionData.ShouldNotBeNull();
        
        // Should have enum and x-enumNames even with empty descriptions
        property.ExtensionData.ShouldContainKey("enum");
        property.ExtensionData.ShouldContainKey("x-enumNames");
        
        var enumValues = property.ExtensionData["enum"] as string[];
        enumValues.ShouldNotBeNull();
        enumValues.Length.ShouldBe(2);
        enumValues[0].ShouldBe("option1");
        enumValues[1].ShouldBe("option2");
        
        // Should have default value
        property.ExtensionData.ShouldContainKey("default");
        var defaultValue = property.ExtensionData["default"] as string;
        defaultValue.ShouldNotBeNull();
        defaultValue.ShouldBe("option1");
        
        // Should not have x-descriptions because all descriptions are empty
        property.ExtensionData.ShouldNotContainKey("x-descriptions");
    }

    [Fact]
    public void SchemaProvider_WithSingleValueAndDescription_ShouldAddAllExtensions()
    {
        // Arrange & Act
        var schema = _schemaProvider.GetTypeSchema(typeof(TestConfigWithSingleDescription));

        // Assert
        schema.ShouldNotBeNull();
        schema.Properties.ShouldNotBeNull();
        schema.Properties.ShouldContainKey("singleValue");
        
        var property = schema.Properties["singleValue"];
        property.ExtensionData.ShouldNotBeNull();
        
        // Should have enum and x-enumNames
        property.ExtensionData.ShouldContainKey("enum");
        property.ExtensionData.ShouldContainKey("x-enumNames");
        
        var enumValues = property.ExtensionData["enum"] as string[];
        enumValues.ShouldNotBeNull();
        enumValues.Length.ShouldBe(1);
        enumValues[0].ShouldBe("single-value");
        
        // Should have default value
        property.ExtensionData.ShouldContainKey("default");
        var defaultValue = property.ExtensionData["default"] as string;
        defaultValue.ShouldNotBeNull();
        defaultValue.ShouldBe("single-value");
        
        // Should have x-descriptions
        property.ExtensionData.ShouldContainKey("x-descriptions");
        var descriptions = property.ExtensionData["x-descriptions"] as string[];
        descriptions.ShouldNotBeNull();
        descriptions.Length.ShouldBe(1);
        descriptions[0].ShouldBe("This is the only option");
    }

    [Fact]
    public void DefaultValuesProcessor_ShouldNotProcessNonClassTypes()
    {
        // Arrange & Act
        var schema = _schemaProvider.GetTypeSchema(typeof(string));

        // Assert - String type should not have any extension data
        schema.ShouldNotBeNull();
        if (schema.ExtensionData != null)
        {
            schema.ExtensionData.ShouldNotContainKey("x-descriptions");
        }
    }

    [Fact]
    public void DefaultValuesProcessor_PropertyNameConversion_ShouldUseCamelCase()
    {
        // This test verifies that the processor correctly converts property names to camelCase
        // Arrange & Act
        var schema = _schemaProvider.GetTypeSchema(typeof(TestConfigWithDescriptions));

        // Assert
        schema.ShouldNotBeNull();
        schema.Properties.ShouldNotBeNull();
        
        // Properties should be in camelCase
        schema.Properties.ShouldContainKey("modelName"); // ModelName -> modelName
        schema.Properties.ShouldContainKey("outputFormat"); // OutputFormat -> outputFormat
        schema.Properties.ShouldContainKey("regularProperty"); // RegularProperty -> regularProperty
        
        // Should not contain PascalCase keys
        schema.Properties.ShouldNotContainKey("ModelName");
        schema.Properties.ShouldNotContainKey("OutputFormat");
        schema.Properties.ShouldNotContainKey("RegularProperty");
    }

    /// <summary>
    /// Test DTO with mixed descriptions (some empty, some with content)
    /// </summary>
    public class TestConfigWithMixedDescriptions
    {
        [DefaultValues(
            new object[] { "option1", "option2", "option3" },
            new string[] { "Good description", "", "Another good description" }
        )]
        public string MixedDescriptions { get; set; } = "option1";
    }

    [Fact]
    public void SchemaProvider_WithMixedDescriptions_ShouldAddAllExtensions()
    {
        // Even if some descriptions are empty, as long as at least one is not empty,
        // x-descriptions should be added, and enum/x-enumNames should always be added
        
        // Arrange & Act
        var schema = _schemaProvider.GetTypeSchema(typeof(TestConfigWithMixedDescriptions));

        // Assert
        schema.ShouldNotBeNull();
        schema.Properties.ShouldNotBeNull();
        schema.Properties.ShouldContainKey("mixedDescriptions");
        
        var property = schema.Properties["mixedDescriptions"];
        property.ExtensionData.ShouldNotBeNull();
        
        // Should have enum and x-enumNames
        property.ExtensionData.ShouldContainKey("enum");
        property.ExtensionData.ShouldContainKey("x-enumNames");
        
        var enumValues = property.ExtensionData["enum"] as string[];
        enumValues.ShouldNotBeNull();
        enumValues.Length.ShouldBe(3);
        enumValues[0].ShouldBe("option1");
        enumValues[1].ShouldBe("option2");
        enumValues[2].ShouldBe("option3");
        
        // Should have default value
        property.ExtensionData.ShouldContainKey("default");
        var defaultValue = property.ExtensionData["default"] as string;
        defaultValue.ShouldNotBeNull();
        defaultValue.ShouldBe("option1");
        
        // Should have x-descriptions because at least one description is not empty
        property.ExtensionData.ShouldContainKey("x-descriptions");
        var descriptions = property.ExtensionData["x-descriptions"] as string[];
        descriptions.ShouldNotBeNull();
        descriptions.Length.ShouldBe(3);
        descriptions[0].ShouldBe("Good description");
        descriptions[1].ShouldBe("");
        descriptions[2].ShouldBe("Another good description");
    }
}
