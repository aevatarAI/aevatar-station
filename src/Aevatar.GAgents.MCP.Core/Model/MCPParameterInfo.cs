using Orleans;
using System.Text.Json;

namespace Aevatar.GAgents.MCP.Core.Model;

/// <summary>
/// Enhanced MCP parameter information supporting full JsonSchema to Orleans State to KernelParameterMetadata conversion
/// </summary>
[GenerateSerializer]
public class MCPParameterInfo
{
    #region Basic properties (backward compatible)
    /// <summary>
    /// Parameter name
    /// </summary>
    [Id(0)] public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameter type (simplified representation, maintaining backward compatibility)
    /// </summary>
    [Id(1)] public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Parameter description
    /// </summary>
    [Id(2)] public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the parameter is required
    /// </summary>
    [Id(3)] public bool Required { get; set; }
    
    /// <summary>
    /// Default value
    /// </summary>
    [Id(4)] public object? DefaultValue { get; set; }
    #endregion

    #region JsonSchema Extended Properties
    /// <summary>
    /// Complete JsonSchema raw data (serialized as string for storage)
    /// </summary>
    [Id(5)] public string? RawJsonSchema { get; set; }
    
    /// <summary>
    /// JsonSchema format constraints (e.g. date-time, email, etc.)
    /// </summary>
    [Id(6)] public string? Format { get; set; }
    
    /// <summary>
    /// String minimum length
    /// </summary>
    [Id(7)] public int? MinLength { get; set; }
    
    /// <summary>
    /// String maximum length
    /// </summary>
    [Id(8)] public int? MaxLength { get; set; }
    
    /// <summary>
    /// Number minimum value
    /// </summary>
    [Id(9)] public double? Minimum { get; set; }
    
    /// <summary>
    /// Number maximum value
    /// </summary>
    [Id(10)] public double? Maximum { get; set; }
    
    /// <summary>
    /// Regular expression pattern
    /// </summary>
    [Id(11)] public string? Pattern { get; set; }
    
    /// <summary>
    /// Enumeration value list
    /// </summary>
    [Id(12)] public List<string>? EnumValues { get; set; }
    
    /// <summary>
    /// Array item type information (for array type)
    /// </summary>
    [Id(13)] public MCPParameterInfo? ArrayItems { get; set; }
    
    /// <summary>
    /// Object property information (for object type)
    /// </summary>
    [Id(14)] public Dictionary<string, MCPParameterInfo>? ObjectProperties { get; set; }
    
    /// <summary>
    /// Object required property list
    /// </summary>
    [Id(15)] public List<string>? RequiredProperties { get; set; }
    
    /// <summary>
    /// Whether to allow additional properties (object type)
    /// </summary>
    [Id(16)] public bool? AdditionalProperties { get; set; }
    
    /// <summary>
    /// JsonSchema type details (supports union types like ["string", "null"])
    /// </summary>
    [Id(17)] public List<string>? TypeArray { get; set; }
    
    /// <summary>
    /// Example value list
    /// </summary>
    [Id(18)] public List<string>? Examples { get; set; }
    #endregion

    #region Static Factory Methods
    /// <summary>
    /// Create MCPParameterInfo from official SDK's JsonElement
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <param name="schema">JsonSchema element</param>
    /// <param name="required">Whether required</param>
    /// <returns>MCPParameterInfo instance</returns>
    public static MCPParameterInfo FromJsonSchema(string name, JsonElement schema, bool required = false)
    {
        var paramInfo = new MCPParameterInfo
        {
            Name = name,
            Required = required,
            RawJsonSchema = schema.GetRawText()
        };

        // Parse basic type
        if (schema.TryGetProperty("type", out var typeElement))
        {
            paramInfo.Type = GetPrimaryType(typeElement);
            paramInfo.TypeArray = GetTypeArray(typeElement);
        }

        // Parse description
        if (schema.TryGetProperty("description", out var descElement))
        {
            paramInfo.Description = descElement.GetString() ?? string.Empty;
        }

        // Parse format
        if (schema.TryGetProperty("format", out var formatElement))
        {
            paramInfo.Format = formatElement.GetString();
        }

        // Parse string constraints
        if (schema.TryGetProperty("minLength", out var minLengthElement))
        {
            paramInfo.MinLength = minLengthElement.GetInt32();
        }
        if (schema.TryGetProperty("maxLength", out var maxLengthElement))
        {
            paramInfo.MaxLength = maxLengthElement.GetInt32();
        }
        if (schema.TryGetProperty("pattern", out var patternElement))
        {
            paramInfo.Pattern = patternElement.GetString();
        }

        // Parse number constraints
        if (schema.TryGetProperty("minimum", out var minimumElement))
        {
            paramInfo.Minimum = minimumElement.GetDouble();
        }
        if (schema.TryGetProperty("maximum", out var maximumElement))
        {
            paramInfo.Maximum = maximumElement.GetDouble();
        }

        // Parse enumeration values
        if (schema.TryGetProperty("enum", out var enumElement) && enumElement.ValueKind == JsonValueKind.Array)
        {
            paramInfo.EnumValues = enumElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .ToList();
        }

        // Parse default value
        if (schema.TryGetProperty("default", out var defaultElement))
        {
            paramInfo.DefaultValue = ConvertJsonElementToBasicType(defaultElement);
        }

        // Parse examples
        if (schema.TryGetProperty("examples", out var examplesElement) && examplesElement.ValueKind == JsonValueKind.Array)
        {
            paramInfo.Examples = examplesElement.EnumerateArray()
                .Select(e => e.GetRawText())
                .ToList();
        }

        // Parse array items
        if (paramInfo.Type == "array" && schema.TryGetProperty("items", out var itemsElement))
        {
            paramInfo.ArrayItems = FromJsonSchema($"{name}_item", itemsElement);
        }

        // Parse object properties
        if (paramInfo.Type == "object")
        {
            if (schema.TryGetProperty("properties", out var propertiesElement))
            {
                paramInfo.ObjectProperties = new Dictionary<string, MCPParameterInfo>();
                foreach (var prop in propertiesElement.EnumerateObject())
                {
                    paramInfo.ObjectProperties[prop.Name] = FromJsonSchema(prop.Name, prop.Value);
                }
            }

            if (schema.TryGetProperty("required", out var requiredElement) && requiredElement.ValueKind == JsonValueKind.Array)
            {
                paramInfo.RequiredProperties = requiredElement.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .ToList();
            }

            if (schema.TryGetProperty("additionalProperties", out var addPropsElement))
            {
                paramInfo.AdditionalProperties = addPropsElement.ValueKind == JsonValueKind.True;
            }
        }

        return paramInfo;
    }

    /// <summary>
    /// Create parameter information in batch from MCP tool JsonSchema's properties section
    /// </summary>
    /// <param name="inputSchema">MCP tool's input schema</param>
    /// <returns>Parameter information dictionary</returns>
    public static Dictionary<string, MCPParameterInfo> FromMCPToolSchema(JsonElement inputSchema)
    {
        var parameters = new Dictionary<string, MCPParameterInfo>();

        if (inputSchema.ValueKind != JsonValueKind.Object)
            return parameters;

        // Get required parameter list
        var requiredParams = new HashSet<string>();
        if (inputSchema.TryGetProperty("required", out var requiredElement) && 
            requiredElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in requiredElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    requiredParams.Add(item.GetString()!);
                }
            }
        }

        // Parse properties
        if (inputSchema.TryGetProperty("properties", out var propertiesElement))
        {
            foreach (var prop in propertiesElement.EnumerateObject())
            {
                var isRequired = requiredParams.Contains(prop.Name);
                parameters[prop.Name] = FromJsonSchema(prop.Name, prop.Value, isRequired);
            }
        }

        return parameters;
    }
    #endregion

    #region Conversion Methods
    /// <summary>
    /// Convert to Semantic Kernel's KernelParameterMetadata
    /// </summary>
    /// <returns>KernelParameterMetadata instance</returns>
    public object ToKernelParameterMetadata()
    {
        // Use reflection to create KernelParameterMetadata because constructor may have different versions
        // Try different assembly names (ordered by priority)
        var possibleAssemblyNames = new[]
        {
            "Microsoft.SemanticKernel.KernelParameterMetadata, Microsoft.SemanticKernel.Abstractions",
            "Microsoft.SemanticKernel.KernelParameterMetadata, Microsoft.SemanticKernel",
            "Microsoft.SemanticKernel.KernelParameterMetadata, Microsoft.SemanticKernel.Core"
        };
        
        Type? metadataType = null;
        foreach (var typeName in possibleAssemblyNames)
        {
            metadataType = System.Type.GetType(typeName);
            if (metadataType != null)
                break;
        }
        
        if (metadataType == null)
        {
            throw new InvalidOperationException("Cannot find KernelParameterMetadata type, tried the following assemblies: " + string.Join(", ", possibleAssemblyNames));
        }

        // Try to use basic constructor
        var constructors = metadataType.GetConstructors();
        var simpleConstructor = constructors.FirstOrDefault(c => 
            c.GetParameters().Length == 1 && 
            c.GetParameters()[0].ParameterType == typeof(string));

        if (simpleConstructor == null)
        {
            throw new InvalidOperationException("Cannot find suitable KernelParameterMetadata constructor");
        }

        // For array types, try to use constructor with schema
        if (Type == "array")
        {
            try
            {
                // Find constructor with schema parameter
                var schemaConstructor = constructors.FirstOrDefault(c =>
                {
                    var parameters = c.GetParameters();
                    return parameters.Length >= 2 &&
                           parameters[0].ParameterType == typeof(string) &&
                           parameters.Any(p => p.ParameterType.Name == "KernelJsonSchema");
                });

                if (schemaConstructor != null)
                {
                    // Try to create KernelJsonSchema
                    var schemaBuilderType = System.Type.GetType("Microsoft.SemanticKernel.KernelJsonSchemaBuilder, Microsoft.SemanticKernel")
                                         ?? System.Type.GetType("Microsoft.SemanticKernel.KernelJsonSchemaBuilder, Microsoft.SemanticKernel.Abstractions");

                    if (schemaBuilderType != null)
                    {
                        var buildMethod = schemaBuilderType.GetMethod("Build", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (buildMethod != null)
                        {
                            var schema = GenerateJsonSchema();
                            var schemaJson = System.Text.Json.JsonSerializer.Serialize(schema);
                            var kernelSchema = buildMethod.Invoke(null, new object[] { schemaJson });

                            var ctorParams = schemaConstructor.GetParameters();
                            var args = new object[ctorParams.Length];
                            args[0] = Name;

                            for (int i = 1; i < ctorParams.Length; i++)
                            {
                                if (ctorParams[i].ParameterType.Name == "KernelJsonSchema")
                                {
                                    args[i] = kernelSchema;
                                }
                                else if (ctorParams[i].HasDefaultValue)
                                {
                                    args[i] = ctorParams[i].DefaultValue;
                                }
                            }

                            var metadataWithSchema = schemaConstructor.Invoke(args);
                            
                            // Set other properties
                            SetPropertySafely(metadataWithSchema, "Description", Description);
                            SetPropertySafely(metadataWithSchema, "IsRequired", Required);
                            if (DefaultValue != null)
                            {
                                SetPropertySafely(metadataWithSchema, "DefaultValue", DefaultValue);
                            }
                            
                            return metadataWithSchema;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If failed, fallback to default approach
                Console.WriteLine($"Failed to create KernelParameterMetadata with schema: {ex.Message}");
            }
        }
        
        var metadata = simpleConstructor.Invoke([Name]);

        // Use reflection to set properties
        SetPropertySafely(metadata, "Description", Description);
        SetPropertySafely(metadata, "IsRequired", Required);
        
        // Try to set default value
        if (DefaultValue != null)
        {
            SetPropertySafely(metadata, "DefaultValue", DefaultValue);
        }

        // If there's type information, try to set it
        if (!string.IsNullOrEmpty(Type))
        {
            SetPropertySafely(metadata, "ParameterType", GetDotNetType());
        }

        // Try to set Schema property (for supporting complex types like arrays)
        var schemaProp = metadataType.GetProperty("Schema");
        if (schemaProp != null && schemaProp.CanWrite)
        {
            try
            {
                var schema = GenerateJsonSchema();
                var schemaJson = System.Text.Json.JsonSerializer.Serialize(schema);
                
                // Try to create KernelJsonSchema (the actual type of Schema property)
                var kernelJsonSchemaType = System.Type.GetType("Microsoft.SemanticKernel.KernelJsonSchema, Microsoft.SemanticKernel.Abstractions")
                    ?? System.Type.GetType("Microsoft.SemanticKernel.KernelJsonSchema, Microsoft.SemanticKernel");
                
                if (kernelJsonSchemaType != null)
                {
                    // KernelJsonSchema has a constructor that accepts string parameter
                    var ctor = kernelJsonSchemaType.GetConstructor(new Type[] { typeof(string) });
                    if (ctor != null)
                    {
                        var kernelJsonSchema = ctor.Invoke(new object[] { schemaJson });
                        schemaProp.SetValue(metadata, kernelJsonSchema);
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignore errors, maintain backward compatibility
                System.Diagnostics.Debug.WriteLine($"Failed to set Schema: {ex.Message}");
            }
        }

        return metadata;
    }

    /// <summary>
    /// Safely set object property
    /// </summary>
    private static void SetPropertySafely(object target, string propertyName, object? value)
    {
        try
        {
            var property = target.GetType().GetProperty(propertyName);
            if (property != null && property.CanWrite && value != null)
            {
                // Special handling for Schema property (requires KernelJsonSchema type)
                if (propertyName == "Schema" && property.PropertyType.Name == "KernelJsonSchema")
                {
                    var jsonString = System.Text.Json.JsonSerializer.Serialize(value);
                    
                    var kernelJsonSchemaType = property.PropertyType;
                    var ctor = kernelJsonSchemaType.GetConstructor(new Type[] { typeof(string) });
                    if (ctor != null)
                    {
                        var kernelJsonSchema = ctor.Invoke(new object[] { jsonString });
                        property.SetValue(target, kernelJsonSchema);
                    }
                }
                else
                {
                    property.SetValue(target, value);
                }
            }
        }
        catch
        {
            // Ignore failed settings, maintain backward compatibility
        }
    }

    /// <summary>
    /// Generate complete JsonSchema object
    /// </summary>
    private object GenerateJsonSchema()
    {
        var schema = new Dictionary<string, object>
        {
            ["type"] = Type ?? "string"
        };

        if (!string.IsNullOrEmpty(Description))
            schema["description"] = Description;

        // For array types, must include items property
        if (Type == "array")
        {
            if (ArrayItems != null)
            {
                schema["items"] = ArrayItems.GenerateJsonSchema();
            }
            else
            {
                // Default items to object type
                schema["items"] = new Dictionary<string, object> { ["type"] = "object" };
            }
        }

        // For object types
        if (Type == "object" && ObjectProperties != null)
        {
            var properties = new Dictionary<string, object>();
            foreach (var (propName, propInfo) in ObjectProperties)
            {
                properties[propName] = propInfo.GenerateJsonSchema();
            }
            schema["properties"] = properties;

            if (RequiredProperties?.Any() == true)
            {
                schema["required"] = RequiredProperties;
            }

            if (AdditionalProperties.HasValue)
            {
                schema["additionalProperties"] = AdditionalProperties.Value;
            }
        }

        // Add constraints
        if (EnumValues?.Any() == true)
            schema["enum"] = EnumValues;

        if (MinLength.HasValue)
            schema["minLength"] = MinLength.Value;

        if (MaxLength.HasValue)
            schema["maxLength"] = MaxLength.Value;

        if (Minimum.HasValue)
            schema["minimum"] = Minimum.Value;

        if (Maximum.HasValue)
            schema["maximum"] = Maximum.Value;

        if (!string.IsNullOrEmpty(Pattern))
            schema["pattern"] = Pattern;

        if (!string.IsNullOrEmpty(Format))
            schema["format"] = Format;

        if (DefaultValue != null)
            schema["default"] = DefaultValue;

        return schema;
    }

    /// <summary>
    /// Get corresponding .NET type
    /// </summary>
    /// <returns>.NET type</returns>
    public System.Type GetDotNetType()
    {
        return Type switch
        {
            "string" => typeof(string),
            "integer" => typeof(int),
            "number" => typeof(double),
            "boolean" => typeof(bool),
            "array" => typeof(object[]),
            "object" => typeof(object),
            _ => typeof(object)
        };
    }

    /// <summary>
    /// Generate parameter description for Semantic Kernel
    /// </summary>
    /// <returns>Enhanced parameter description</returns>
    public string GetEnhancedDescription()
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(Description))
        {
            parts.Add(Description);
        }

        // For array types, add complete JSON Schema information
        if (Type == "array")
        {
            var schema = GenerateJsonSchema();
            var schemaJson = System.Text.Json.JsonSerializer.Serialize(schema, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = false 
            });
            parts.Add($"JSON Schema: {schemaJson}");
        }
        else
        {
            // Add type information
            if (!string.IsNullOrEmpty(Type))
            {
                parts.Add($"Type: {Type}");
            }
        }

        // Add constraint information
        if (EnumValues?.Any() == true)
        {
            parts.Add($"Allowed values: {string.Join(", ", EnumValues)}");
        }

        if (MinLength.HasValue || MaxLength.HasValue)
        {
            var lengthInfo = $"Length: {MinLength ?? 0}-{MaxLength?.ToString() ?? "unlimited"}";
            parts.Add(lengthInfo);
        }

        if (Minimum.HasValue || Maximum.HasValue)
        {
            var rangeInfo = $"Range: {Minimum?.ToString() ?? "unlimited"}-{Maximum?.ToString() ?? "unlimited"}";
            parts.Add(rangeInfo);
        }

        if (!string.IsNullOrEmpty(Format))
        {
            parts.Add($"Format: {Format}");
        }

        if (!string.IsNullOrEmpty(Pattern))
        {
            parts.Add($"Pattern: {Pattern}");
        }

        return string.Join(". ", parts);
    }
    #endregion

    #region Helper Methods
    private static string GetPrimaryType(JsonElement typeElement)
    {
        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return typeElement.GetString() ?? "any";
        }
        else if (typeElement.ValueKind == JsonValueKind.Array)
        {
            // For union types, choose the first non-null type
            foreach (var item in typeElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var typeStr = item.GetString();
                    if (typeStr != null && typeStr != "null")
                    {
                        return typeStr;
                    }
                }
            }
        }
        return "any";
    }

    private static List<string>? GetTypeArray(JsonElement typeElement)
    {
        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            return typeElement.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .ToList();
        }
        return null;
    }

    private static object? ConvertJsonElementToBasicType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }
    #endregion
}
