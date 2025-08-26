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
        if (ShouldProcessType(context)) ProcessGenericMetaData(context);
    }

    protected virtual bool ShouldProcessType(SchemaProcessorContext context) => context.ContextualType.Type.IsEnum;

    private void ProcessGenericMetaData(SchemaProcessorContext context)
    {
        var enumType = context.ContextualType.Type;
        var enumMetadata = BuildEnumMetadata(enumType);
        if (HasAnyMetadata(enumMetadata)) AddMetadataToSchema(context, enumMetadata);
    }

    protected virtual Dictionary<string, object> BuildEnumMetadata(System.Type enumType)
    {
        var enumMetadata = new Dictionary<string, object>();
        foreach (var enumValue in System.Enum.GetValues(enumType))
        {
            var enumValueData = ProcessSingleEnumValue(enumValue, enumType);
            if (enumValueData.HasValue) enumMetadata[enumValueData.Value.Name] = enumValueData.Value.Metadata;
        }
        return enumMetadata;
    }

    protected virtual (string Name, Dictionary<string, object> Metadata)? ProcessSingleEnumValue(object enumValue, System.Type enumType)
    {
        var enumName = enumValue.ToString();
        if (enumName == null) return null;
        var fieldInfo = enumType.GetField(enumName);
        if (fieldInfo == null) return null;
        var attributes = GetGenericMetaAttributes(fieldInfo);
        var metadata = ProcessEnumValueMetadata(attributes);
        if (!HasAnyMetadata(metadata)) return null;
        return (enumName, metadata);
    }

    protected virtual IEnumerable<GenericMetaAttribute> GetGenericMetaAttributes(FieldInfo fieldInfo) => fieldInfo.GetCustomAttributes<GenericMetaAttribute>(true);

    protected static bool HasAnyMetadata(Dictionary<string, object> metadata) => metadata.Any();

    protected virtual Dictionary<string, object> ProcessEnumValueMetadata(IEnumerable<GenericMetaAttribute> attributes)
    {
        var metadata = new Dictionary<string, object>();
        var attributeGroups = GroupAttributesByType(attributes);
        ProcessPathAttributes(metadata, attributeGroups.PathAttributes);
        ProcessDirectAttributes(metadata, attributeGroups.DirectAttributes);
        return metadata;
    }

    protected static (IEnumerable<GenericMetaAttribute> PathAttributes, IEnumerable<GenericMetaAttribute> DirectAttributes) GroupAttributesByType(IEnumerable<GenericMetaAttribute> attributes)
    {
        var pathAttributes = attributes.Where(attr => HasValidPathLevels(attr));
        var directAttributes = attributes.Where(attr => !HasValidPathLevels(attr));
        return (pathAttributes, directAttributes);
    }

    protected static bool HasValidPathLevels(GenericMetaAttribute attr) => attr.PathLevels != null && attr.PathLevels.Length > 0;

    protected virtual void ProcessPathAttributes(Dictionary<string, object> metadata, IEnumerable<GenericMetaAttribute> pathAttributes)
    {
        foreach (var attr in pathAttributes)
            if (attr.PathLevels != null) SetNestedValue(metadata, attr.PathLevels, attr.Key, attr.Value);
    }

    protected static void ProcessDirectAttributes(Dictionary<string, object> metadata, IEnumerable<GenericMetaAttribute> directAttributes)
    {
        foreach (var attr in directAttributes) metadata[attr.Key] = attr.Value;
    }

    public static void SetNestedValue(Dictionary<string, object> root, string[] pathParts, string key, object value)
    {
        var current = root;
        for (int i = 0; i < pathParts.Length; i++)
        {
            var part = pathParts[i];
            if (!current.ContainsKey(part)) current[part] = new Dictionary<string, object>();
            if (i < pathParts.Length - 1) current = (Dictionary<string, object>)current[part];
            else ((Dictionary<string, object>)current[part])[key] = value;
        }
    }

    protected virtual void AddMetadataToSchema(SchemaProcessorContext context, Dictionary<string, object> enumMetadata)
    {
        EnsureSchemaExtensionData(context);
        SetEnumMetadataExtension(context, enumMetadata);
    }

    protected static void EnsureSchemaExtensionData(SchemaProcessorContext context)
    {
        if (context.Schema.ExtensionData == null) context.Schema.ExtensionData = new Dictionary<string, object>();
    }

    protected static void SetEnumMetadataExtension(SchemaProcessorContext context, Dictionary<string, object> enumMetadata)
    {
        context.Schema.ExtensionData["x-enumMetadatas"] = enumMetadata;
    }
}