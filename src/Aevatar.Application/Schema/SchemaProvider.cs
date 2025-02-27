using System;
using System.Collections.Generic;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Validation;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Schema;

public class SchemaProvider : ISchemaProvider, ISingletonDependency
{
    private readonly object _lockObj = new object();
    private readonly Dictionary<Type, JsonSchema> _schemaDic = new Dictionary<Type, JsonSchema>();

    public JsonSchema GetTypeSchema(Type type)
    {
        lock (_lockObj)
        {
            if (_schemaDic.TryGetValue(type, out var queryData))
            {
                return queryData;
            }

            var settings = new SystemTextJsonSchemaGeneratorSettings
            {
                FlattenInheritanceHierarchy = true,
                GenerateEnumMappingDescription = true,
                SchemaProcessors ={ new IgnoreSpecificBaseProcessor() }
            };

            var schemaData = JsonSchema.FromType(type, settings);
            _schemaDic.Add(type, schemaData);
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