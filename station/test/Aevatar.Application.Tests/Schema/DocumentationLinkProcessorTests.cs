using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Aevatar.GAgents.Basic.Common;
using Aevatar.Schema;
using NJsonSchema;
using NJsonSchema.Generation;
using Shouldly;
using Xunit;

namespace Aevatar.Schema;

/// <summary>
/// Comprehensive unit tests for DocumentationLinkProcessor
/// </summary>
public class DocumentationLinkProcessorTests
{
    #region Test Data Classes
    
    /// <summary>
    /// Test DTO with DocumentationLinkAttribute properties
    /// </summary>
    public class TestConfigDto
    {
        [DocumentationLink("https://docs.example.com/api-key")]
        public string ApiKey { get; set; } = "";
        
        [DocumentationLink("https://docs.example.com/endpoint")]
        public string EndpointUrl { get; set; } = "";
        
        public string NormalProperty { get; set; } = "";
    }
    
    /// <summary>
    /// Test DTO with multiple properties for comprehensive testing
    /// </summary>
    public class TestConfigWithMultiplePropertiesDto
    {
        [DocumentationLink("https://valid.example.com/config")]
        public string ValidUrlProperty { get; set; } = "";
        
        [DocumentationLink("https://invalid.example.com/config")]
        public string InvalidUrlProperty { get; set; } = "";
        
        [DocumentationLink("https://docs.example.com/timeout")]
        public int TimeoutProperty { get; set; }
        
        public string PropertyWithoutAttribute { get; set; } = "";
    }
    
    /// <summary>
    /// Test enum for type filtering verification
    /// </summary>
    public enum TestEnum
    {
        Value1,
        Value2
    }
    
    #endregion

    #region Constructor and Interface Tests
    
    [Fact]
    public void Constructor_ShouldCreateProcessor()
    {
        // Arrange & Act
        var processor = new DocumentationLinkProcessor();
        
        // Assert
        processor.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithContext_ShouldCreateProcessor()
    {
        // Arrange
        var context = new SchemaProcessingContext();
        
        // Act
        var processor = new DocumentationLinkProcessor(context);
        
        // Assert
        processor.ShouldNotBeNull();
    }

    [Fact]
    public void ProcessorImplementsISchemaProcessor()
    {
        // Arrange & Act
        var processor = new DocumentationLinkProcessor();
        
        // Assert
        processor.ShouldBeAssignableTo<ISchemaProcessor>();
    }
    
    #endregion

    #region Process Method Tests
    
    [Fact]
    public void Process_WithNullContext_ShouldNotThrow()
    {
        // Arrange
        var processor = new DocumentationLinkProcessor();
        
        // Act & Assert
        Should.NotThrow(() => processor.Process(null));
    }

    [Fact]
    public void Process_WithEnumType_ShouldNotProcess_Integration()
    {
        // Arrange
        var processor = new DocumentationLinkProcessor();
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SchemaProcessors = { processor }
        };

        // Act
        var schema = JsonSchema.FromType(typeof(TestEnum), settings);

        // Assert
        // Enum types should not be processed by DocumentationLinkProcessor
        schema.ShouldNotBeNull();
        schema.Type.ShouldBe(JsonObjectType.Integer); // Enums are typically integers in JSON schema
    }

    [Fact]
    public void Process_WithClassType_ShouldProcessDocumentationLinks_Integration()
    {
        // Arrange
        var processor = new DocumentationLinkProcessor();
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SchemaProcessors = { processor }
        };
        settings.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var schema = JsonSchema.FromType(typeof(TestConfigDto), settings);

        // Assert
        schema.ShouldNotBeNull();
        schema.Properties.ShouldContainKey("apiKey");
        schema.Properties.ShouldContainKey("endpointUrl");
        schema.Properties.ShouldContainKey("normalProperty");
        
        // Check apiKey property has documentation URL
        schema.Properties["apiKey"].ExtensionData.ShouldContainKey("documentationUrl");
        schema.Properties["apiKey"].ExtensionData!["documentationUrl"].ShouldBe("https://docs.example.com/api-key");
        schema.Properties["apiKey"].ExtensionData!["documentationUrlValid"].ShouldBe(true);
        
        // Check endpointUrl property has documentation URL  
        schema.Properties["endpointUrl"].ExtensionData.ShouldContainKey("documentationUrl");
        schema.Properties["endpointUrl"].ExtensionData!["documentationUrl"].ShouldBe("https://docs.example.com/endpoint");
        schema.Properties["endpointUrl"].ExtensionData!["documentationUrlValid"].ShouldBe(true);
        
        // Check normalProperty should not have extension data
        (schema.Properties["normalProperty"].ExtensionData?.ContainsKey("documentationUrl") ?? false).ShouldBeFalse();
    }

    [Fact]
    public void Process_WithValidUrls_ShouldMarkAllUrlsValid_Integration()
    {
        // Arrange
        var processingContext = new SchemaProcessingContext(); // No invalid URLs
        var processor = new DocumentationLinkProcessor(processingContext);
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SchemaProcessors = { processor }
        };
        settings.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var schema = JsonSchema.FromType(typeof(TestConfigWithMultiplePropertiesDto), settings);

        // Assert
        schema.Properties["validUrlProperty"].ExtensionData!["documentationUrlValid"].ShouldBe(true);
        schema.Properties["invalidUrlProperty"].ExtensionData!["documentationUrlValid"].ShouldBe(true);
        schema.Properties["timeoutProperty"].ExtensionData!["documentationUrlValid"].ShouldBe(true);
    }

    [Fact]
    public void Process_WithInvalidUrls_ShouldMarkSpecificUrlsInvalid_Integration()
    {
        // Arrange
        var processingContext = new SchemaProcessingContext();
        processingContext.InvalidUrls.Add("https://invalid.example.com/config");
        var processor = new DocumentationLinkProcessor(processingContext);
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SchemaProcessors = { processor }
        };
        settings.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var schema = JsonSchema.FromType(typeof(TestConfigWithMultiplePropertiesDto), settings);

        // Assert
        schema.Properties["validUrlProperty"].ExtensionData!["documentationUrlValid"].ShouldBe(true);
        schema.Properties["invalidUrlProperty"].ExtensionData!["documentationUrlValid"].ShouldBe(false);
        schema.Properties["timeoutProperty"].ExtensionData!["documentationUrlValid"].ShouldBe(true);
    }

    [Fact]
    public void Process_WithNullProcessingContext_ShouldMarkAllUrlsValid_Integration()
    {
        // Arrange
        var processor = new DocumentationLinkProcessor(null);
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SchemaProcessors = { processor }
        };
        settings.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var schema = JsonSchema.FromType(typeof(TestConfigDto), settings);

        // Assert
        schema.Properties["apiKey"].ExtensionData!["documentationUrlValid"].ShouldBe(true);
        schema.Properties["endpointUrl"].ExtensionData!["documentationUrlValid"].ShouldBe(true);
    }

    [Fact]
    public void Process_Integration_WithRealSchemaGeneration()
    {
        // Arrange
        var processingContext = new SchemaProcessingContext();
        processingContext.InvalidUrls.Add("https://invalid.example.com/config");
        
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            FlattenInheritanceHierarchy = true,
            GenerateEnumMappingDescription = true,
            SchemaProcessors = { new DocumentationLinkProcessor(processingContext) }
        };
        settings.SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var schema = JsonSchema.FromType(typeof(TestConfigWithMultiplePropertiesDto), settings);

        // Assert
        schema.ShouldNotBeNull();
        schema.Properties.ShouldContainKey("validUrlProperty");
        schema.Properties.ShouldContainKey("invalidUrlProperty");
        schema.Properties.ShouldContainKey("timeoutProperty");
        schema.Properties.ShouldContainKey("propertyWithoutAttribute");
        
        // Check documentation URLs are added correctly
        schema.Properties["validUrlProperty"].ExtensionData.ShouldContainKey("documentationUrl");
        schema.Properties["validUrlProperty"].ExtensionData!["documentationUrl"].ShouldBe("https://valid.example.com/config");
        schema.Properties["validUrlProperty"].ExtensionData!["documentationUrlValid"].ShouldBe(true);
        
        schema.Properties["invalidUrlProperty"].ExtensionData.ShouldContainKey("documentationUrl");
        schema.Properties["invalidUrlProperty"].ExtensionData!["documentationUrl"].ShouldBe("https://invalid.example.com/config");
        schema.Properties["invalidUrlProperty"].ExtensionData!["documentationUrlValid"].ShouldBe(false);
        
        // Property without attribute should not have documentation extension data
        (schema.Properties["propertyWithoutAttribute"].ExtensionData?.ContainsKey("documentationUrl") ?? false).ShouldBeFalse();
    }

    #endregion

    #region Property Name Conversion Tests

    [Fact]
    public void GetPropertyName_WithNormalPropertyName_ShouldConvertToCamelCase()
    {
        // Arrange
        var processor = new DocumentationLinkProcessor();
        var method = typeof(DocumentationLinkProcessor).GetMethod("GetPropertyName", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = method?.Invoke(processor, new object[] { "ApiKey" }) as string;
        
        // Assert
        result.ShouldBe("apiKey");
    }

    [Fact]  
    public void GetPropertyName_WithSingleCharacter_ShouldConvertToCamelCase()
    {
        // Arrange
        var processor = new DocumentationLinkProcessor();
        var method = typeof(DocumentationLinkProcessor).GetMethod("GetPropertyName", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = method?.Invoke(processor, new object[] { "A" }) as string;
        
        // Assert
        result.ShouldBe("a");
    }

    [Fact]
    public void GetPropertyName_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        var processor = new DocumentationLinkProcessor();
        var method = typeof(DocumentationLinkProcessor).GetMethod("GetPropertyName", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = method?.Invoke(processor, new object[] { "" }) as string;
        
        // Assert
        result.ShouldBe("");
    }

    [Fact]
    public void GetPropertyName_WithNullString_ShouldReturnNull()
    {
        // Arrange
        var processor = new DocumentationLinkProcessor();
        var method = typeof(DocumentationLinkProcessor).GetMethod("GetPropertyName", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = method?.Invoke(processor, new object[] { null! }) as string;
        
        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetPropertyName_WithAlreadyCamelCase_ShouldReturnSame()
    {
        // Arrange
        var processor = new DocumentationLinkProcessor();
        var method = typeof(DocumentationLinkProcessor).GetMethod("GetPropertyName", BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = method?.Invoke(processor, new object[] { "apiKey" }) as string;
        
        // Assert
        result.ShouldBe("apiKey");
    }

    #endregion
}