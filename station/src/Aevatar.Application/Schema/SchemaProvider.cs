using System;
using System.Collections.Generic;
using System.Text.Json;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Validation;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Schema;

public class SchemaProvider : ISchemaProvider, ISingletonDependency
{
    private readonly object _lockObj = new object();
    private readonly Dictionary<Type, JsonSchema> _schemaDic = new Dictionary<Type, JsonSchema>();
    private readonly DynamicDropDownProcessor _dynamicDropDownProcessor;

    public SchemaProvider(DynamicDropDownProcessor dynamicDropDownProcessor)
    {
        _dynamicDropDownProcessor = dynamicDropDownProcessor;
    }

    public JsonSchema GetTypeSchema(Type type, DynamicDropDownContext? dynamicContext = null, SchemaProcessingContext? documentationContext = null)
    {
        lock (_lockObj)
        {
            // Skip caching when any context is provided to allow dynamic processing
            if (dynamicContext == null && documentationContext == null && _schemaDic.TryGetValue(type, out var queryData))
            {
                return queryData;
            }

            // 设置dynamic context到processor中
            _dynamicDropDownProcessor.SetContext(dynamicContext);

            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                FlattenInheritanceHierarchy = true,
                GenerateEnumMappingDescription = true,
                SchemaProcessors = { 
                    new IgnoreSpecificBaseProcessor(),
                    _dynamicDropDownProcessor  // 使用注入的实例
                }
            };
            
            // 如果有documentation context，添加DocumentationLinkProcessor
            if (documentationContext != null)
            {
                settings.SchemaProcessors.Add(new DocumentationLinkProcessor(documentationContext));
            }
            
            settings.SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var schemaData = JsonSchema.FromType(type, settings);
            // Only cache when no context is provided
            if (dynamicContext == null && documentationContext == null)
            {
                _schemaDic.Add(type, schemaData);
            }
            return schemaData;
        }
    }

    public Dictionary<string, string> ConvertValidateError(ICollection<ValidationError> errors)
    {
        var result = new Dictionary<string, string>();
        foreach (var item in errors)
        {
            if (item.Property != null)
            {
                result.Add(item.Property, ConvertValidateError(item));
            }
        }

        return result;
    }

    public string ConvertValidateError(ValidationError error)
    {
        var description = error.Schema.Description ?? "Field is incorrect";
        return description;
    }
}