using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aevatar.Options;
using NJsonSchema.Generation;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Schema;

public class SystemLLMDropDownSchemaProcess : DropDownSchemaProcessBase, ITransientDependency
{
    public override string OptionName => "SystemLLMConfigs";

    public override void ProcessSchema(SchemaProcessorContext processorContext, PropertyInfo property,
        DynamicDropDownContext? dropDownContext)
    {
        if (processorContext.Schema.Properties == null || processorContext.Schema.Properties.Count == 0)
            return;

        // Look for the property schema using different naming conventions
        string[] possibleKeys =
        {
            property.Name.ToLowerInvariant(),
            char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1),
            property.Name
        };

        foreach (var key in possibleKeys)
        {
            if (processorContext.Schema.Properties.TryGetValue(key, out var propertySchema))
            {
                if (propertySchema.ExtensionData == null)
                    propertySchema.ExtensionData = new Dictionary<string, object?>();

                InjectSystemLLMConfigurations(propertySchema.ExtensionData, dropDownContext);
                return;
            }
        }
    }

    private void InjectSystemLLMConfigurations(IDictionary<string, object?> extensionData,
        DynamicDropDownContext? dropDownContext)
    {
        if (dropDownContext?.AdditionalData == null)
            return;
        
        if (!dropDownContext.AdditionalData.TryGetValue(OptionName, out var aiModelConfigsObj) ||
            aiModelConfigsObj is not List<SystemLLMConfigDto> aiModelConfigs)
            return;

        var configObjectList = aiModelConfigs.Select(config => new
        {
            Name = config.Name,
            Strengths = config.Strengths,
            BestFor = config.BestFor
        }).ToList<object>();

        var enumNames = aiModelConfigs.Select(c => c.Name).ToArray();

        extensionData["x-descriptions"] = configObjectList;
        extensionData["x-enumNames"] = enumNames;
        extensionData["enum"] = enumNames;
    }
}