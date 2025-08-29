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
                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null!;
            case JsonValueKind.Array:
                var list = element.EnumerateArray().Select(ConvertJsonElementToBasicType).ToList();

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