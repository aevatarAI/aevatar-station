using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.MCP.Core;

/// <summary>
/// Provides utility methods for converting between JSON elements and other data types
/// </summary>
public static class JsonConversionHelper
{
    /// <summary>
    /// Converts a JsonElement to a specified property type
    /// </summary>
    private static object? ConvertJsonElementToType(JsonElement jsonElement, Type targetType)
    {
        if (targetType == typeof(string))
            return jsonElement.GetString();

        if (targetType == typeof(int) || targetType == typeof(int?))
            return jsonElement.TryGetInt32(out var intValue) ? intValue : null;

        if (targetType == typeof(long) || targetType == typeof(long?))
            return jsonElement.TryGetInt64(out var longValue) ? longValue : null;

        if (targetType == typeof(double) || targetType == typeof(double?))
            return jsonElement.TryGetDouble(out var doubleValue) ? doubleValue : null;

        if (targetType == typeof(bool) || targetType == typeof(bool?))
            return jsonElement.ValueKind == JsonValueKind.True || jsonElement.ValueKind == JsonValueKind.False
                ? jsonElement.GetBoolean()
                : null;

        if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            return jsonElement.TryGetDateTime(out var dateValue) ? dateValue : null;

        if (targetType == typeof(Guid) || targetType == typeof(Guid?))
            return jsonElement.TryGetGuid(out var guidValue) ? guidValue : null;

        // For complex types, try to deserialize JSON
        try
        {
            var json = jsonElement.GetRawText();
            return JsonSerializer.Deserialize(json, targetType);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts JsonElement to basic types suitable for Orleans serialization
    /// Enhanced version with better error handling and decimal support
    /// </summary>
    public static object ConvertJsonElementToBasicType(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;
            case JsonValueKind.Number:
                if (element.TryGetInt32(out var intValue))
                    return intValue;
                if (element.TryGetInt64(out var longValue))
                    return longValue;
                if (element.TryGetDouble(out var doubleValue))
                    return doubleValue;
                return element.GetDecimal();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null!;
            case JsonValueKind.Array:
                var list = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertJsonElementToBasicType(item));
                }
                return list;
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    dict[property.Name] = ConvertJsonElementToBasicType(property.Value);
                }
                return dict;
            case JsonValueKind.Undefined:
            default:
                return element.ToString();
        }
    }

    /// <summary>
    /// Converts a value to the expected type based on string type definition (from MCP parameter type)
    /// </summary>
    public static object ConvertToExpectedType(object value, string expectedType, ILogger? logger = null)
    {
        try
        {
            // Handle JsonElement conversion first
            if (value is JsonElement element)
            {
                return ConvertJsonElementToExpectedType(element, expectedType, logger);
            }

            // Handle string to other types conversion
            if (value is string strValue)
            {
                switch (expectedType.ToLower())
                {
                    case "number":
                    case "float":
                    case "double":
                        if (double.TryParse(strValue, out var doubleValue))
                            return doubleValue;
                        throw new InvalidOperationException($"Cannot convert string '{strValue}' to number");

                    case "integer":
                    case "int":
                        if (int.TryParse(strValue, out var intValue))
                            return intValue;
                        throw new InvalidOperationException($"Cannot convert string '{strValue}' to integer");

                    case "boolean":
                    case "bool":
                        if (bool.TryParse(strValue, out var boolValue))
                            return boolValue;
                        // Handle "0"/"1" as boolean
                        if (strValue == "0") return false;
                        if (strValue == "1") return true;
                        throw new InvalidOperationException($"Cannot convert string '{strValue}' to boolean");

                    case "array":
                        // Try to parse as JSON array
                        try
                        {
                            return JsonSerializer.Deserialize<List<object>>(strValue) ?? new List<object>();
                        }
                        catch
                        {
                            // If not JSON, return as single-element list
                            return new List<object> { strValue };
                        }

                    case "object":
                        // Try to parse as JSON object
                        try
                        {
                            return JsonSerializer.Deserialize<Dictionary<string, object>>(strValue) ?? 
                                   new Dictionary<string, object>();
                        }
                        catch
                        {
                            // If not JSON, return as-is
                            return strValue;
                        }

                    case "string":
                        return strValue;

                    default:
                        // Unknown type, return as-is
                        return strValue;
                }
            }

            // For non-string values, use the existing conversion logic
            if (value is JsonElement jsonElement)
            {
                return ConvertJsonElementToBasicType(jsonElement);
            }
            return value;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to convert value {Value} to type {ExpectedType}", value, expectedType);
            return value; // Return original value as fallback
        }
    }

    /// <summary>
    /// Converts JsonElement to expected type based on string type definition (from MCP parameter type)
    /// </summary>
    public static object ConvertJsonElementToExpectedType(JsonElement element, string expectedType, ILogger? logger = null)
    {
        try
        {
            switch (expectedType.ToLower())
            {
                case "string":
                    return element.ValueKind == JsonValueKind.String
                        ? element.GetString() ?? string.Empty
                        : element.ToString();

                case "number":
                case "float":
                case "double":
                    if (element.ValueKind == JsonValueKind.Number)
                        return element.GetDouble();
                    if (element.ValueKind == JsonValueKind.String && double.TryParse(element.GetString(), out var d))
                        return d;
                    throw new InvalidOperationException($"Cannot convert {element.ValueKind} to number");

                case "integer":
                case "int":
                    if (element.ValueKind == JsonValueKind.Number)
                        return element.GetInt32();
                    if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var i))
                        return i;
                    throw new InvalidOperationException($"Cannot convert {element.ValueKind} to integer");

                case "boolean":
                case "bool":
                    if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                        return element.GetBoolean();
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        var str = element.GetString();
                        if (bool.TryParse(str, out var b))
                            return b;
                        if (str == "0") return false;
                        if (str == "1") return true;
                    }
                    throw new InvalidOperationException($"Cannot convert {element.ValueKind} to boolean");

                case "array":
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        var list = new List<object>();
                        foreach (var item in element.EnumerateArray())
                        {
                            list.Add(ConvertJsonElementToBasicType(item));
                        }
                        return list;
                    }
                    throw new InvalidOperationException($"Cannot convert {element.ValueKind} to array");

                case "object":
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (var prop in element.EnumerateObject())
                        {
                            dict[prop.Name] = ConvertJsonElementToBasicType(prop.Value);
                        }
                        return dict;
                    }
                    throw new InvalidOperationException($"Cannot convert {element.ValueKind} to object");

                default:
                    // Unknown type, use basic conversion
                    return ConvertJsonElementToBasicType(element);
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to convert JsonElement {ValueKind} to type {ExpectedType}", 
                element.ValueKind, expectedType);
            return ConvertJsonElementToBasicType(element); // Fallback to basic conversion
        }
    }

    /// <summary>
    /// Attempts to set an object property value, performing necessary type conversions
    /// </summary>
    public static bool TrySetPropertyValue(object target, string propertyName, object? value, ILogger? logger = null)
    {
        if (target == null || string.IsNullOrEmpty(propertyName))
            return false;

        var property = target.GetType().GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property == null || !property.CanWrite)
            return false;

        try
        {
            // Convert JsonElement (if needed)
            if (value is JsonElement jsonElement)
            {
                value = ConvertJsonElementToType(jsonElement, property.PropertyType);
            }

            if (value != null)
            {
                // Convert to target property type
                var convertedValue = Convert.ChangeType(value, property.PropertyType);
                property.SetValue(target, convertedValue);
                return true;
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Setting property {PropertyName} failed", property.Name);
        }

        return false;
    }
}