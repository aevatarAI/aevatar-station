using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace Aevatar.Domain.Shared.Util
{
    /// <summary>
    /// Helper class for converting JSON objects to basic .NET types that can be serialized by Orleans and other systems.
    /// Handles both Newtonsoft.Json.Linq types (JObject, JArray, etc.) and System.Text.Json types (JsonElement).
    /// </summary>
    public static class JsonConversionHelper
    {
        /// <summary>
        /// Converts a dictionary that may contain JSON objects to a dictionary with only basic .NET types.
        /// </summary>
        /// <param name="input">The input dictionary that may contain JObject, JsonElement, or other JSON types</param>
        /// <returns>A new dictionary with all values converted to basic .NET types</returns>
        public static Dictionary<string, object> ConvertToBasicTypes(Dictionary<string, object> input)
        {
            if (input == null) return new Dictionary<string, object>();

            var result = new Dictionary<string, object>();
            foreach (var kvp in input)
            {
                var convertedValue = ConvertValue(kvp.Value);
                if (convertedValue != null || kvp.Value == null)
                {
                    result[kvp.Key] = convertedValue;
                }
            }
            return result;
        }

        /// <summary>
        /// Converts a single value that may be a JSON object to a basic .NET type.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The converted value as a basic .NET type</returns>
        public static object? ConvertValue(object? value)
        {
            if (value == null) return null;

            // Handle Newtonsoft.Json.Linq types
            if (value is JObject jObject)
            {
                var dict = new Dictionary<string, object>();
                foreach (var prop in jObject.Properties())
                {
                    var convertedValue = ConvertValue(prop.Value);
                    if (convertedValue != null || prop.Value?.Type == JTokenType.Null)
                    {
                        dict[prop.Name] = convertedValue;
                    }
                }
                return dict;
            }

            if (value is JArray jArray)
            {
                var list = new List<object?>();
                foreach (var item in jArray)
                {
                    list.Add(ConvertValue(item));
                }
                return list;
            }

            if (value is JValue jValue)
            {
                return jValue.Value;
            }

            if (value is JToken jToken)
            {
                switch (jToken.Type)
                {
                    case JTokenType.Object:
                        return ConvertValue(jToken as JObject);
                    case JTokenType.Array:
                        return ConvertValue(jToken as JArray);
                    case JTokenType.Integer:
                        return jToken.ToObject<long>();
                    case JTokenType.Float:
                        return jToken.ToObject<double>();
                    case JTokenType.String:
                        return jToken.ToObject<string>();
                    case JTokenType.Boolean:
                        return jToken.ToObject<bool>();
                    case JTokenType.Date:
                        return jToken.ToObject<DateTime>();
                    case JTokenType.Guid:
                        return jToken.ToObject<Guid>();
                    case JTokenType.Uri:
                        return jToken.ToObject<Uri>()?.ToString();
                    case JTokenType.TimeSpan:
                        return jToken.ToObject<TimeSpan>();
                    case JTokenType.Null:
                        return null;
                    default:
                        return jToken.ToString();
                }
            }

            // Handle System.Text.Json types
            if (value is JsonElement element)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        return element.GetString();
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
                        return null;
                    case JsonValueKind.Array:
                        var list = new List<object?>();
                        foreach (var item in element.EnumerateArray())
                        {
                            list.Add(ConvertValue(item));
                        }
                        return list;
                    case JsonValueKind.Object:
                        var dict = new Dictionary<string, object>();
                        foreach (var prop in element.EnumerateObject())
                        {
                            var convertedValue = ConvertValue(prop.Value);
                            if (convertedValue != null || prop.Value.ValueKind == JsonValueKind.Null)
                            {
                                dict[prop.Name] = convertedValue;
                            }
                        }
                        return dict;
                    default:
                        return element.ToString();
                }
            }

            // Handle collections that might contain JSON objects
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                if (value is System.Collections.IDictionary dictionary)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (System.Collections.DictionaryEntry entry in dictionary)
                    {
                        var key = entry.Key?.ToString() ?? "";
                        var convertedValue = ConvertValue(entry.Value);
                        if (convertedValue != null || entry.Value == null)
                        {
                            dict[key] = convertedValue;
                        }
                    }
                    return dict;
                }
                else if (value.GetType().IsGenericType)
                {
                    var list = new List<object?>();
                    foreach (var item in enumerable)
                    {
                        list.Add(ConvertValue(item));
                    }
                    return list;
                }
            }

            // If it's already a basic type, return as-is
            var type = value.GetType();
            if (type.IsPrimitive || 
                type == typeof(string) || 
                type == typeof(DateTime) || 
                type == typeof(DateTimeOffset) || 
                type == typeof(TimeSpan) || 
                type == typeof(Guid) || 
                type == typeof(decimal) ||
                type.IsEnum)
            {
                return value;
            }

            // For other complex types, try to convert to string
            return value.ToString();
        }

        /// <summary>
        /// Converts a list that may contain JSON objects to a list with only basic .NET types.
        /// </summary>
        /// <param name="input">The input list that may contain JObject, JsonElement, or other JSON types</param>
        /// <returns>A new list with all values converted to basic .NET types</returns>
        public static List<object?> ConvertToBasicTypes(IEnumerable<object> input)
        {
            if (input == null) return new List<object?>();

            return input.Select(ConvertValue).ToList();
        }

        /// <summary>
        /// Checks if a type needs conversion (i.e., it's a JSON type that Orleans can't serialize).
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type needs conversion, false otherwise</returns>
        public static bool NeedsConversion(Type type)
        {
            return type.Namespace?.StartsWith("Newtonsoft.Json.Linq") == true ||
                   type == typeof(JsonElement) ||
                   type == typeof(JsonDocument);
        }
    }
} 