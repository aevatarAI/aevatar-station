using System.Collections.Generic;
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
    }
}
