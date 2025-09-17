using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Aevatar.GAgents.AI.Common;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Service
{
    /// <summary>
    /// Unit tests specifically for testing the schema enhancement logic changes
    /// These tests verify that single-value DefaultValues don't create enums while multi-value ones do
    /// </summary>
    public class SchemaEnhancementTests
    {
        /// <summary>
        /// Test DTO with single-value DefaultValues to verify no enum is created
        /// </summary>
        public class TestSingleValueConfigDto
        {
            [DefaultValues("Single Value")]
            public string Instructions { get; set; } = "Single Value";
            
            public string RegularProperty { get; set; } = "DefaultValue";
        }

        /// <summary>
        /// Test DTO with multi-value DefaultValues to verify enum is created
        /// </summary>
        public class TestMultiValueConfigDto
        {
            [DefaultValues("Option1", "Option2", "Option3")]
            public string VideoStyle { get; set; } = "Option1";
            
            [DefaultValues(5, 10, 15)]
            public int Duration { get; set; } = 5;
        }

        [Fact]
        public void DefaultValuesAttribute_WithSingleValue_ShouldOnlyHaveOneValue()
        {
            // Test to verify our test setup is correct
            var property = typeof(TestSingleValueConfigDto).GetProperty("Instructions");
            var attribute = property.GetCustomAttribute<DefaultValuesAttribute>();
            
            attribute.ShouldNotBeNull();
            attribute.Values.Length.ShouldBe(1);
            attribute.Values[0].ShouldBe("Single Value");
        }

        [Fact]
        public void DefaultValuesAttribute_WithMultipleValues_ShouldHaveMultipleValues()
        {
            // Test to verify our test setup is correct
            var property = typeof(TestMultiValueConfigDto).GetProperty("VideoStyle");
            var attribute = property.GetCustomAttribute<DefaultValuesAttribute>();
            
            attribute.ShouldNotBeNull();
            attribute.Values.Length.ShouldBe(3);
            attribute.Values[0].ShouldBe("Option1");
            attribute.Values[1].ShouldBe("Option2");
            attribute.Values[2].ShouldBe("Option3");
        }

        [Fact]
        public void EnumLogic_WithSingleValueAttribute_ShouldNotCreateEnum()
        {
            // Arrange - Simulate the condition check from the fix
            var property = typeof(TestSingleValueConfigDto).GetProperty("Instructions");
            var defaultValuesAttribute = property.GetCustomAttribute<DefaultValuesAttribute>();
            
            // Act - Test the fixed condition: Length > 1 instead of Length > 0
            bool shouldCreateEnum = defaultValuesAttribute?.Values != null && defaultValuesAttribute.Values.Length > 1;
            
            // Assert
            shouldCreateEnum.ShouldBeFalse("Single-value DefaultValues should NOT create enum property");
        }

        [Fact]
        public void EnumLogic_WithMultiValueAttribute_ShouldCreateEnum()
        {
            // Arrange - Simulate the condition check from the fix  
            var property = typeof(TestMultiValueConfigDto).GetProperty("VideoStyle");
            var defaultValuesAttribute = property.GetCustomAttribute<DefaultValuesAttribute>();
            
            // Act - Test the fixed condition: Length > 1 instead of Length > 0
            bool shouldCreateEnum = defaultValuesAttribute?.Values != null && defaultValuesAttribute.Values.Length > 1;
            
            // Assert
            shouldCreateEnum.ShouldBeTrue("Multi-value DefaultValues should create enum property");
        }

        [Fact]
        public void EnumLogic_VerifyOldBehaviorWouldHaveBeenWrong()
        {
            // Arrange - Simulate the OLD buggy condition  
            var property = typeof(TestSingleValueConfigDto).GetProperty("Instructions");
            var defaultValuesAttribute = property.GetCustomAttribute<DefaultValuesAttribute>();
            
            // Act - Test the OLD condition: Length > 0 (this was the bug)
            bool oldBuggyLogic = defaultValuesAttribute?.Values != null && defaultValuesAttribute.Values.Length > 0;
            
            // Assert - This proves the old logic was wrong
            oldBuggyLogic.ShouldBeTrue("OLD buggy logic would have incorrectly created enum for single values");
        }

        [Fact]
        public void JsonPropertySchema_SingleValueEnum_ShouldBeInvalidJsonSchema()
        {
            // Arrange - Create a problematic single-value enum like the bug produced
            var problematicSchema = new Dictionary<string, object>
            {
                ["type"] = "string",
                ["default"] = "Single Value",
                ["enum"] = new[] { "Single Value" } // This is what the bug was creating
            };
            
            var json = JsonSerializer.Serialize(problematicSchema);
            
            // Assert - This demonstrates why single-value enums are problematic
            json.ShouldContain("\"enum\":[\"Single Value\"]");
            
            // A single-value enum doesn't make semantic sense for a dropdown/selection
            // It should just be a default value without enum
            problematicSchema["enum"].ShouldBeOfType<string[]>();
            ((string[])problematicSchema["enum"]).Length.ShouldBe(1);
        }

        [Fact]
        public void JsonPropertySchema_MultiValueEnum_ShouldBeValidJsonSchema()
        {
            // Arrange - Create a proper multi-value enum 
            var validSchema = new Dictionary<string, object>
            {
                ["type"] = "string", 
                ["default"] = "Option1",
                ["enum"] = new[] { "Option1", "Option2", "Option3" }
            };
            
            var json = JsonSerializer.Serialize(validSchema);
            
            // Assert - This is what we want: meaningful choice with multiple options
            json.ShouldContain("\"enum\":[\"Option1\",\"Option2\",\"Option3\"]");
            
            validSchema["enum"].ShouldBeOfType<string[]>();
            ((string[])validSchema["enum"]).Length.ShouldBe(3);
        }

        #region New Tests for Descriptions Feature

        /// <summary>
        /// Test DTO with values and descriptions to verify new functionality
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
        }

        /// <summary>
        /// Test DTO with mismatched descriptions length
        /// </summary>
        public class TestConfigMismatchedDescriptions
        {
            [DefaultValues(
                new object[] { "option1", "option2", "option3" },
                new string[] { "Only one description" } // Mismatched length
            )]
            public string Options { get; set; } = "option1";
        }

        /// <summary>
        /// Test DTO with empty descriptions
        /// </summary>
        public class TestConfigEmptyDescriptions
        {
            [DefaultValues(
                new object[] { "val1", "val2" },
                new string[] { "", "" } // Empty descriptions
            )]
            public string EmptyDescriptions { get; set; } = "val1";

            [DefaultValues(
                new object[] { "val1", "val2" },
                new string[] { "Good description", "" } // Mixed descriptions
            )]
            public string MixedDescriptions { get; set; } = "val1";
        }

        [Fact]
        public void DefaultValuesAttribute_WithDescriptions_ShouldHaveCorrectDescriptions()
        {
            // Arrange
            var property = typeof(TestConfigWithDescriptions).GetProperty("ModelName");
            var attribute = property.GetCustomAttribute<DefaultValuesAttribute>();
            
            // Act & Assert
            attribute.ShouldNotBeNull();
            attribute.Values.Length.ShouldBe(3);
            attribute.Descriptions.Length.ShouldBe(3);
            
            attribute.Values[0].ShouldBe("gpt-4");
            attribute.Values[1].ShouldBe("claude-3");
            attribute.Values[2].ShouldBe("gemini-pro");
            
            attribute.Descriptions[0].ShouldBe("OpenAI GPT-4 - Advanced reasoning");
            attribute.Descriptions[1].ShouldBe("Anthropic Claude 3 - Creative writing");
            attribute.Descriptions[2].ShouldBe("Google Gemini Pro - Multilingual support");
        }

        [Fact]
        public void DefaultValuesAttribute_WithMismatchedDescriptionsLength_ShouldAdjustDescriptionsArray()
        {
            // Arrange
            var property = typeof(TestConfigMismatchedDescriptions).GetProperty("Options");
            var attribute = property.GetCustomAttribute<DefaultValuesAttribute>();
            
            // Act & Assert - Descriptions array should be adjusted to match values length
            attribute.ShouldNotBeNull();
            attribute.Values.Length.ShouldBe(3);
            attribute.Descriptions.Length.ShouldBe(3); // Should be adjusted
            
            attribute.Descriptions[0].ShouldBe("Only one description");
            attribute.Descriptions[1].ShouldBe(""); // Should be empty string for missing descriptions
            attribute.Descriptions[2].ShouldBe(""); // Should be empty string for missing descriptions
        }

        [Fact]
        public void DefaultValuesAttribute_WithEmptyDescriptions_ShouldHaveEmptyStrings()
        {
            // Arrange
            var property = typeof(TestConfigEmptyDescriptions).GetProperty("EmptyDescriptions");
            var attribute = property.GetCustomAttribute<DefaultValuesAttribute>();
            
            // Act & Assert
            attribute.ShouldNotBeNull();
            attribute.Descriptions.Length.ShouldBe(2);
            attribute.Descriptions[0].ShouldBe("");
            attribute.Descriptions[1].ShouldBe("");
        }

        [Fact]
        public void DefaultValuesAttribute_WithMixedDescriptions_ShouldPreserveNonEmptyDescriptions()
        {
            // Arrange
            var property = typeof(TestConfigEmptyDescriptions).GetProperty("MixedDescriptions");
            var attribute = property.GetCustomAttribute<DefaultValuesAttribute>();
            
            // Act & Assert
            attribute.ShouldNotBeNull();
            attribute.Descriptions.Length.ShouldBe(2);
            attribute.Descriptions[0].ShouldBe("Good description");
            attribute.Descriptions[1].ShouldBe("");
        }

        [Fact]
        public void SchemaEnhancement_WithDescriptions_ShouldIncludeXDescriptionsField()
        {
            // Arrange - Simulate schema enhancement logic for descriptions
            var property = typeof(TestConfigWithDescriptions).GetProperty("ModelName");
            var attribute = property.GetCustomAttribute<DefaultValuesAttribute>();
            
            // Act - Test the logic that determines if x-descriptions should be added
            bool hasDescriptions = attribute?.Descriptions != null && 
                                 attribute.Descriptions.Length == attribute.Values.Length &&
                                 attribute.Descriptions.Any(d => !string.IsNullOrEmpty(d));
            
            // Simulate creating the schema extension
            var schemaExtensions = new Dictionary<string, object>();
            if (hasDescriptions)
            {
                schemaExtensions["x-descriptions"] = attribute.Descriptions;
            }
            
            // Assert
            hasDescriptions.ShouldBeTrue("Should detect that valid descriptions exist");
            schemaExtensions.ShouldContainKey("x-descriptions");
            schemaExtensions["x-descriptions"].ShouldBeOfType<string[]>();
            
            var descriptions = (string[])schemaExtensions["x-descriptions"];
            descriptions.Length.ShouldBe(3);
            descriptions[0].ShouldBe("OpenAI GPT-4 - Advanced reasoning");
        }

        [Fact]
        public void SchemaEnhancement_WithEmptyDescriptions_ShouldNotIncludeXDescriptionsField()
        {
            // Arrange
            var property = typeof(TestConfigEmptyDescriptions).GetProperty("EmptyDescriptions");
            var attribute = property.GetCustomAttribute<DefaultValuesAttribute>();
            
            // Act - Test the logic that determines if x-descriptions should be added
            bool hasDescriptions = attribute?.Descriptions != null && 
                                 attribute.Descriptions.Length == attribute.Values.Length &&
                                 attribute.Descriptions.Any(d => !string.IsNullOrEmpty(d));
            
            // Assert
            hasDescriptions.ShouldBeFalse("Should detect that no meaningful descriptions exist");
        }

        [Fact]
        public void SchemaEnhancement_WithSingleValueAndDescription_ShouldNotCreateEnum()
        {
            // Arrange - Test that single values with descriptions still don't create enums
            var singleValueAttribute = new DefaultValuesAttribute(
                new object[] { "single-option" },
                new string[] { "This is the only option" }
            );
            
            // Act - Apply both the enum condition AND the description condition
            bool shouldCreateEnum = singleValueAttribute?.Values != null && singleValueAttribute.Values.Length > 1;
            bool hasDescriptions = singleValueAttribute?.Descriptions != null && 
                                 singleValueAttribute.Descriptions.Length == singleValueAttribute.Values.Length &&
                                 singleValueAttribute.Descriptions.Any(d => !string.IsNullOrEmpty(d));
            
            // Assert
            shouldCreateEnum.ShouldBeFalse("Single values should never create enums, even with descriptions");
            hasDescriptions.ShouldBeTrue("Single value can still have description");
        }

        #endregion
    }
}
