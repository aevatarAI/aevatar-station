# AIGAgentBase MCP Tool Parameter Type Conversion

## Overview

This document describes the intelligent type conversion system for MCP (Model Context Protocol) tool parameters in AIGAgentBase.

## Problem Statement

When LLMs call MCP tools, they often pass parameters as strings (e.g., `"gender": "1"`), but MCP tools may expect specific types (e.g., `gender` as a number). This mismatch causes errors like:

```
MCP error -32602: Invalid arguments for tool buildBaziFromSolarDatetime:
Expected number, received string
```

## Solution

The enhanced `CallMCPToolAsync` method now performs intelligent type conversion based on MCP tool parameter definitions.

### How It Works

1. **Tool Information Retrieval**: Before calling an MCP tool, the system retrieves the tool's parameter definitions
2. **Type-Aware Conversion**: Each parameter is converted to its expected type based on the tool's schema
3. **Fallback Handling**: If no type information is available, the system falls back to basic type inference

### Supported Type Conversions

| Source Type | Target Type | Examples |
|-------------|-------------|----------|
| String | Number/Double | `"3.14"` → `3.14` |
| String | Integer | `"42"` → `42` |
| String | Boolean | `"true"` → `true`, `"1"` → `true`, `"0"` → `false` |
| String | Array | `"[1,2,3]"` → `[1, 2, 3]` |
| String | Object | `'{"key":"value"}'` → `{key: "value"}` |
| JsonElement | Any | Automatic conversion based on ValueKind |

### Implementation Details

```csharp
// The conversion happens in CallMCPToolAsync
var tools = await mcpAgent.GetAvailableToolsAsync();
MCPToolInfo? toolInfo = tools.Values.FirstOrDefault(t => t.Name == toolName);

foreach (var (key, value) in kernelArgs)
{
    if (toolInfo?.Parameters.TryGetValue(key, out var paramInfo) == true)
    {
        // Convert based on expected type
        parameters[key] = ConvertToExpectedType(value, paramInfo.Type);
    }
    else
    {
        // Fallback to basic conversion
        parameters[key] = ConvertJsonElementToBasicType(value);
    }
}
```

### Error Handling

The conversion methods throw `InvalidOperationException` when a value cannot be converted to the expected type, providing clear error messages for debugging.

## Benefits

1. **Seamless Integration**: LLMs can call MCP tools without worrying about exact type formatting
2. **Reduced Errors**: Automatic type conversion prevents common parameter type mismatches
3. **Better Developer Experience**: No need to manually handle type conversions in tool implementations
4. **Backward Compatible**: Falls back to basic conversion when type information is unavailable

## Example

### Before (Error)
```json
// LLM sends
{
  "gender": "1",
  "year": "1990"
}

// MCP tool expects
{
  "gender": number,
  "year": number
}
// Result: Type mismatch error
```

### After (Success)
```json
// LLM sends
{
  "gender": "1",
  "year": "1990"
}

// System converts to
{
  "gender": 1,
  "year": 1990
}
// Result: Tool executes successfully
```

## Best Practices

1. **Define Clear Parameter Types**: Always specify parameter types in MCP tool definitions
2. **Use Standard Type Names**: Stick to standard types: `string`, `number`, `integer`, `boolean`, `array`, `object`
3. **Document Expected Formats**: In tool descriptions, mention expected formats for complex types
4. **Handle Edge Cases**: Consider how your tool handles null/undefined values

## Troubleshooting

### Tool still receiving wrong types?
1. Check that the MCP tool definition includes parameter type information
2. Verify the parameter name matches exactly (case-sensitive)
3. Check logs for conversion errors or warnings

### Conversion failing?
1. Ensure the input value can be reasonably converted to the target type
2. For complex objects, ensure they are valid JSON strings
3. Check for special characters or encoding issues 