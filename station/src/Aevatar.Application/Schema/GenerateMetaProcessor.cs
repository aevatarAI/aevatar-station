using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Aevatar.GAgents.Basic;
using NJsonSchema.Generation;

namespace Aevatar.Schema;

public class GenericMetaProcessor : ISchemaProcessor
{
    public void Process(SchemaProcessorContext context)
    {
        if (context.ContextualType.Type.IsEnum)
        {
            ProcessGenericMetaData(context);
        }
    }

    private void ProcessGenericMetaData(SchemaProcessorContext context)
    {
        var enumType = context.ContextualType.Type;
        var enumMetadata = new Dictionary<string, object>();

        foreach (var enumValue in System.Enum.GetValues(enumType))
        {
            var enumName = enumValue.ToString();
            if (enumName != null)
            {
                var fieldInfo = enumType.GetField(enumName);

                if (fieldInfo != null)
                {
                    var genericMetaAttributes = fieldInfo.GetCustomAttributes<GenericMetaAttribute>(true);
                    var enumValueMetadata = ProcessEnumValueMetadata(genericMetaAttributes);

                    if (enumValueMetadata.Any())
                    {
                        enumMetadata[enumName] = enumValueMetadata;
                    }
                }
            }
        }

        if (enumMetadata.Any()) AddMetadataToSchema(context, enumMetadata);
    }

    private Dictionary<string, object> ProcessEnumValueMetadata(IEnumerable<GenericMetaAttribute> attributes)
    {
        var metadata = new Dictionary<string, object>();

        var pathAttributes = attributes.Where(attr => attr.PathLevels != null && attr.PathLevels.Length > 0);
        var directAttributes = attributes.Where(attr => attr.PathLevels == null || attr.PathLevels.Length == 0);

        foreach (var attr in pathAttributes)
        {
            if (attr.PathLevels != null)
            {
                SetNestedValue(metadata, attr.PathLevels, attr.Key, attr.Value);
            }
        }

        foreach (var attr in directAttributes)
        {
            metadata[attr.Key] = attr.Value;
        }

        return metadata;
    }

    private void SetNestedValue(Dictionary<string, object> root, string[] pathParts, string key, object value)
    {
        var current = root;

        for (int i = 0; i < pathParts.Length; i++)
        {
            var part = pathParts[i];

            if (!current.ContainsKey(part))
            {
                current[part] = new Dictionary<string, object>();
            }

            if (i < pathParts.Length - 1)
            {
                current = (Dictionary<string, object>)current[part];
            }
            else
            {
                var finalDict = (Dictionary<string, object>)current[part];
                finalDict[key] = value;
            }
        }
    }

    private void AddMetadataToSchema(SchemaProcessorContext context, Dictionary<string, object> enumMetadata)
    {
        if (context.Schema.ExtensionData == null)
        {
            context.Schema.ExtensionData = new Dictionary<string, object>();
        }

        context.Schema.ExtensionData["x-enumMetadatas"] = enumMetadata;
    }
}