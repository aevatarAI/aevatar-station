using System;
using System.Collections.Generic;
using Aevatar.Service;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace Aevatar.Application.Tests.Service;

/// <summary>
/// Unit tests for AiWorkflowNodeDto JsonProperties functionality
/// </summary>
public class AiWorkflowNodeDtoTests
{
    #region Positive Test Cases

    [Fact]
    public void JsonProperties_WithEmptyProperties_ShouldReturnEmptyJsonObject()
    {
        // Arrange
        var nodeDto = new AiWorkflowNodeDto
        {
            NodeId = "test-node-1",
            AgentType = "TestAgent",
            Name = "Test Node",
            Properties = new Dictionary<string, object>()
        };

        // Act
        var jsonProperties = nodeDto.JsonProperties;

        // Assert
        jsonProperties.ShouldNotBeNull();
        jsonProperties.ShouldBe("{}");
    }

    [Fact]
    public void JsonProperties_WithValidProperties_ShouldReturnCorrectJsonString()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            { "inputText", "Hello World" },
            { "maxTokens", 100 },
            { "temperature", 0.7 },
            { "enabled", true }
        };

        var nodeDto = new AiWorkflowNodeDto
        {
            NodeId = "test-node-2",
            AgentType = "TextCompletionAgent",
            Name = "Text Completion",
            Properties = properties
        };

        // Act
        var jsonProperties = nodeDto.JsonProperties;

        // Assert
        jsonProperties.ShouldNotBeNull();
        
        // Parse back to verify JSON structure
        var parsedProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonProperties);
        parsedProperties.ShouldNotBeNull();
        parsedProperties.Count.ShouldBe(4);
        parsedProperties["inputText"].ShouldBe("Hello World");
        parsedProperties["maxTokens"].ToString().ShouldBe("100");
        parsedProperties["temperature"].ToString().ShouldBe("0.7");
        parsedProperties["enabled"].ToString().ShouldBe("True");
    }

    [Fact]
    public void JsonProperties_WithComplexNestedProperties_ShouldReturnValidJson()
    {
        // Arrange
        var nestedObject = new Dictionary<string, object>
        {
            { "subProperty1", "value1" },
            { "subProperty2", 42 }
        };

        var arrayProperty = new List<object> { "item1", "item2", 123 };

        var properties = new Dictionary<string, object>
        {
            { "simpleString", "test value" },
            { "nestedObject", nestedObject },
            { "arrayProperty", arrayProperty },
            { "nullValue", null }
        };

        var nodeDto = new AiWorkflowNodeDto
        {
            NodeId = "complex-node",
            AgentType = "ComplexAgent",
            Name = "Complex Node",
            Properties = properties
        };

        // Act
        var jsonProperties = nodeDto.JsonProperties;

        // Assert
        jsonProperties.ShouldNotBeNull();
        jsonProperties.ShouldNotBe("{}");
        
        // Verify it's valid JSON by parsing
        var parsedProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonProperties);
        parsedProperties.ShouldNotBeNull();
        parsedProperties.Count.ShouldBe(4);
        parsedProperties.ContainsKey("simpleString").ShouldBeTrue();
        parsedProperties.ContainsKey("nestedObject").ShouldBeTrue();
        parsedProperties.ContainsKey("arrayProperty").ShouldBeTrue();
        parsedProperties.ContainsKey("nullValue").ShouldBeTrue();
    }

    [Fact]
    public void JsonProperties_WithSpecialCharacters_ShouldEscapeCorrectly()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            { "quotedText", "This is a \"quoted\" string" },
            { "newlineText", "Line 1\nLine 2" },
            { "tabText", "Column1\tColumn2" },
            { "backslashText", "Path\\to\\file" }
        };

        var nodeDto = new AiWorkflowNodeDto
        {
            NodeId = "special-chars-node",
            AgentType = "SpecialCharsAgent",
            Name = "Special Characters Node",
            Properties = properties
        };

        // Act
        var jsonProperties = nodeDto.JsonProperties;

        // Assert
        jsonProperties.ShouldNotBeNull();
        
        // Parse back to verify escaping worked correctly
        var parsedProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonProperties);
        parsedProperties.ShouldNotBeNull();
        parsedProperties["quotedText"].ShouldBe("This is a \"quoted\" string");
        parsedProperties["newlineText"].ShouldBe("Line 1\nLine 2");
        parsedProperties["tabText"].ShouldBe("Column1\tColumn2");
        parsedProperties["backslashText"].ShouldBe("Path\\to\\file");
    }

    #endregion

    #region Negative Test Cases

    [Fact]
    public void JsonProperties_WithNullProperties_ShouldReturnEmptyJsonObject()
    {
        // Arrange
        var nodeDto = new AiWorkflowNodeDto
        {
            NodeId = "null-props-node",
            AgentType = "TestAgent",
            Name = "Null Properties Node",
            Properties = null
        };

        // Act
        var jsonProperties = nodeDto.JsonProperties;

        // Assert
        jsonProperties.ShouldNotBeNull();
        jsonProperties.ShouldBe("{}");
    }

    [Fact]
    public void JsonProperties_WithPropertiesContainingCircularReference_ShouldHandleGracefully()
    {
        // Arrange
        var nodeDto = new AiWorkflowNodeDto
        {
            NodeId = "circular-ref-node",
            AgentType = "TestAgent",
            Name = "Circular Reference Node",
            Properties = new Dictionary<string, object>()
        };

        // Create circular reference
        nodeDto.Properties["self"] = nodeDto.Properties;

        // Act
        var jsonProperties = nodeDto.JsonProperties;

        // Assert
        // Should return "{}" due to exception handling
        jsonProperties.ShouldBe("{}");
    }

    #endregion

    #region Boundary Test Cases

    [Fact]
    public void JsonProperties_WithSingleProperty_ShouldReturnValidJson()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            { "singleKey", "singleValue" }
        };

        var nodeDto = new AiWorkflowNodeDto
        {
            NodeId = "single-prop-node",
            AgentType = "SinglePropAgent",
            Name = "Single Property Node",
            Properties = properties
        };

        // Act
        var jsonProperties = nodeDto.JsonProperties;

        // Assert
        jsonProperties.ShouldNotBeNull();
        jsonProperties.ShouldContain("singleKey");
        jsonProperties.ShouldContain("singleValue");
        
        var parsedProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonProperties);
        parsedProperties.Count.ShouldBe(1);
    }

    [Fact]
    public void JsonProperties_WithLargeNumberOfProperties_ShouldHandleCorrectly()
    {
        // Arrange
        var properties = new Dictionary<string, object>();
        for (int i = 0; i < 100; i++)
        {
            properties[$"property{i}"] = $"value{i}";
        }

        var nodeDto = new AiWorkflowNodeDto
        {
            NodeId = "large-props-node",
            AgentType = "LargePropsAgent",
            Name = "Large Properties Node",
            Properties = properties
        };

        // Act
        var jsonProperties = nodeDto.JsonProperties;

        // Assert
        jsonProperties.ShouldNotBeNull();
        jsonProperties.ShouldNotBe("{}");
        
        var parsedProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonProperties);
        parsedProperties.Count.ShouldBe(100);
    }

    [Fact]
    public void JsonProperties_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            { "chineseText", "你好世界" },
            { "emojiText", "🌟🚀💡" },
            { "arabicText", "مرحبا بالعالم" },
            { "unicodeSymbols", "∑∆∏∫" }
        };

        var nodeDto = new AiWorkflowNodeDto
        {
            NodeId = "unicode-node",
            AgentType = "UnicodeAgent",
            Name = "Unicode Node",
            Properties = properties
        };

        // Act
        var jsonProperties = nodeDto.JsonProperties;

        // Assert
        jsonProperties.ShouldNotBeNull();
        
        var parsedProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonProperties);
        parsedProperties.ShouldNotBeNull();
        parsedProperties["chineseText"].ShouldBe("你好世界");
        parsedProperties["emojiText"].ShouldBe("🌟🚀💡");
        parsedProperties["arabicText"].ShouldBe("مرحبا بالعالم");
        parsedProperties["unicodeSymbols"].ShouldBe("∑∆∏∫");
    }

    #endregion

    #region Exception Test Cases

    [Fact]
    public void JsonProperties_MultipleAccess_ShouldReturnConsistentResults()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            { "consistent", "value" },
            { "number", 42 }
        };

        var nodeDto = new AiWorkflowNodeDto
        {
            NodeId = "consistent-node",
            AgentType = "ConsistentAgent",
            Name = "Consistent Node",
            Properties = properties
        };

        // Act
        var jsonProperties1 = nodeDto.JsonProperties;
        var jsonProperties2 = nodeDto.JsonProperties;
        var jsonProperties3 = nodeDto.JsonProperties;

        // Assert
        jsonProperties1.ShouldBe(jsonProperties2);
        jsonProperties2.ShouldBe(jsonProperties3);
        jsonProperties1.ShouldNotBeNull();
        jsonProperties1.ShouldNotBe("{}");
    }

    [Fact]
    public void JsonProperties_AfterPropertiesModification_ShouldReflectChanges()
    {
        // Arrange
        var nodeDto = new AiWorkflowNodeDto
        {
            NodeId = "modifiable-node",
            AgentType = "ModifiableAgent",
            Name = "Modifiable Node",
            Properties = new Dictionary<string, object> { { "initial", "value" } }
        };

        // Act - Get initial JSON
        var initialJson = nodeDto.JsonProperties;
        
        // Modify properties
        nodeDto.Properties["newKey"] = "newValue";
        nodeDto.Properties.Remove("initial");
        
        // Get updated JSON
        var updatedJson = nodeDto.JsonProperties;

        // Assert
        initialJson.ShouldContain("initial");
        initialJson.ShouldNotContain("newKey");
        
        updatedJson.ShouldNotContain("initial");
        updatedJson.ShouldContain("newKey");
        updatedJson.ShouldContain("newValue");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteNodeDto_WithJsonProperties_ShouldWorkTogether()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            { "description", "Test description" },
            { "timeout", 30000 },
            { "retryCount", 3 }
        };

        var nodeDto = new AiWorkflowNodeDto
        {
            NodeId = "integration-test-node",
            AgentType = "IntegrationTestAgent", 
            Name = "Integration Test Node",
            ExtendedData = new AiWorkflowNodeExtendedDataDto
            {
                XPosition = "100",
                YPosition = "200"
            },
            Properties = properties
        };

        // Act
        var jsonProperties = nodeDto.JsonProperties;

        // Assert - Verify all DTO properties work correctly
        nodeDto.NodeId.ShouldBe("integration-test-node");
        nodeDto.AgentType.ShouldBe("IntegrationTestAgent");
        nodeDto.Name.ShouldBe("Integration Test Node");
        nodeDto.ExtendedData.XPosition.ShouldBe("100");
        nodeDto.ExtendedData.YPosition.ShouldBe("200");
        
        // Verify JsonProperties
        jsonProperties.ShouldNotBeNull();
        jsonProperties.ShouldNotBe("{}");
        
        var parsedProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonProperties);
        parsedProperties.Count.ShouldBe(3);
        parsedProperties["description"].ShouldBe("Test description");
        parsedProperties["timeout"].ToString().ShouldBe("30000");
        parsedProperties["retryCount"].ToString().ShouldBe("3");
    }

    #endregion
}