# JsonConversionHelper Usage Guide

## Purpose
`JsonConversionHelper` is a utility class that converts JSON objects to basic .NET types that can be serialized by Orleans and other systems. It handles both Newtonsoft.Json.Linq types (JObject, JArray, etc.) and System.Text.Json types (JsonElement).

## When to Use
Use this helper when:
- You receive JSON data from APIs that needs to be passed to Orleans Grains
- You have mixed JSON types (JObject/JsonElement) that need conversion
- You need to ensure all data is serializable by Orleans
- You want to convert complex JSON structures to basic .NET types

## Usage Examples

### Basic Dictionary Conversion
```csharp
using Aevatar.Domain.Shared.Util;

// Convert a dictionary that may contain JObject/JsonElement
var input = new Dictionary<string, object>
{
    ["name"] = "John",
    ["data"] = someJObject,  // JObject from Newtonsoft.Json
    ["config"] = someJsonElement  // JsonElement from System.Text.Json
};

var converted = JsonConversionHelper.ConvertToBasicTypes(input);
// Now all values are basic .NET types
```

### Single Value Conversion
```csharp
// Convert a single JObject
JObject jObj = JObject.Parse(@"{""name"": ""test"", ""value"": 123}");
var converted = JsonConversionHelper.ConvertValue(jObj);
// Result: Dictionary<string, object> { ["name"] = "test", ["value"] = 123L }

// Convert a JsonElement
JsonElement element = JsonDocument.Parse(@"[1, 2, 3]").RootElement;
var converted = JsonConversionHelper.ConvertValue(element);
// Result: List<object> { 1, 2, 3 }
```

### List Conversion
```csharp
var inputList = new List<object> { jObject1, jsonElement2, "string", 123 };
var convertedList = JsonConversionHelper.ConvertToBasicTypes(inputList);
// All JSON objects are converted to basic types
```

### Check if Conversion is Needed
```csharp
var needsConversion = JsonConversionHelper.NeedsConversion(value.GetType());
if (needsConversion)
{
    value = JsonConversionHelper.ConvertValue(value);
}
```

## In Controllers

### Example: MCP Controller
```csharp
[HttpPost("tool-call")]
public async Task<IActionResult> CallTool([FromBody] ToolCallRequest request)
{
    // Convert arguments before passing to Orleans
    var toolCallEvent = new ToolCallEvent
    {
        ToolName = request.ToolName,
        Arguments = JsonConversionHelper.ConvertToBasicTypes(
            request.Arguments ?? new Dictionary<string, object>()
        )
    };
    
    // Now safe to pass to Orleans Grain
    await grain.ProcessToolCall(toolCallEvent);
}
```

### Example: Generic API Handler
```csharp
public async Task<IActionResult> ProcessData([FromBody] Dictionary<string, object> data)
{
    // Ensure all nested JSON objects are converted
    var processableData = JsonConversionHelper.ConvertToBasicTypes(data);
    
    // Now safe for Orleans serialization
    await grain.ProcessData(processableData);
}
```

## Supported Conversions

### Newtonsoft.Json.Linq Types
- `JObject` → `Dictionary<string, object>`
- `JArray` → `List<object>`
- `JValue` → Appropriate .NET type (string, long, double, bool, etc.)
- `JToken` → Based on token type

### System.Text.Json Types
- `JsonElement` (Object) → `Dictionary<string, object>`
- `JsonElement` (Array) → `List<object>`
- `JsonElement` (String/Number/Bool) → Appropriate .NET type

### Additional Features
- Handles nested structures recursively
- Preserves null values
- Converts special types (DateTime, Guid, Uri, TimeSpan)
- Falls back to string representation for unknown types

## Performance Considerations
- The conversion is recursive, so deeply nested structures may impact performance
- Consider caching converted results if the same data is used multiple times
- For large datasets, consider streaming or pagination approaches 