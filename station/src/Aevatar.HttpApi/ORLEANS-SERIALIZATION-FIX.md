# Orleans Serialization Fix for JObject

## Problem
When calling the MCP tool endpoint with complex JSON arguments:
```json
{
  "key": "user-preferences",
  "value": {
    "theme": "dark",
    "language": "en",
    "notifications": true
  }
}
```

Orleans throws an error:
```
"Could not find a copier for type Newtonsoft.Json.Linq.JObject."
```

## Root Cause
1. ASP.NET Core deserializes JSON request body using Newtonsoft.Json by default
2. When `Dictionary<string, object>` contains nested objects, they become `JObject` instances
3. Orleans cannot serialize `JObject` types when passing data between Grains
4. The original `ConvertValue` method only handled `System.Text.Json.JsonElement`

## Solution
Extended the `ConvertValue` method in `MCPDemoController` to handle Newtonsoft.Json.Linq types:

- `JObject` → `Dictionary<string, object>`
- `JArray` → `List<object>`
- `JValue` → Basic .NET type (string, int, bool, etc.)
- `JToken` → Appropriate basic type based on token type

## Code Changes
The `ConvertValue` method now handles both:
1. **Newtonsoft.Json.Linq types** (JObject, JArray, JValue, JToken)
2. **System.Text.Json types** (JsonElement)

This ensures all JSON data is converted to basic .NET types that Orleans can serialize.

## Alternative Solutions (Not Implemented)
1. Configure ASP.NET Core to use System.Text.Json globally
2. Change the API to accept string arguments and parse them in the Grain
3. Create custom Orleans serializers for JObject types

## Testing
After this fix, the test script should work correctly:
```bash
source ./set-env.sh
./test-tool-calling.sh
```

The store operation with complex objects will now succeed. 