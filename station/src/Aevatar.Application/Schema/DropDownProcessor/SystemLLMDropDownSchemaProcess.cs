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
        if (processorContext.Schema.Properties != null && processorContext.Schema.Properties.Count > 0)
        {
            // Look for the property schema using different naming conventions
            string[] possibleKeys =
            {
                property.Name.ToLowerInvariant(), // systemllm
                char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1), // systemLLM -> systemLLM
                property.Name // SystemLLM
            };

            foreach (var key in possibleKeys)
            {
                if (processorContext.Schema.Properties.TryGetValue(key, out var propertySchema))
                {
                    if (propertySchema.ExtensionData == null)
                        propertySchema.ExtensionData = new Dictionary<string, object>();

                    InjectSystemLLMConfigurations(propertySchema.ExtensionData, dropDownContext);

                    return;
                }
            }
        }
    }

    private void InjectSystemLLMConfigurations(IDictionary<string, object?> extensionData,
        DynamicDropDownContext? dropDownContext)
    {
        // Get AI model configurations from the dictionary
        if (!dropDownContext.AdditionalData.TryGetValue(OptionName, out var aiModelConfigsObj) ||
            aiModelConfigsObj is not List<SystemLLMConfigDto> aiModelConfigs)
        {
            return; // No configurations available
        }

        // Create a list of AI model configurations as objects
        var configObjectList = new List<object>();

        foreach (var config in aiModelConfigs)
        {
            var configObject = new
            {
                Name = config.Name,
                Strengths = config.Strengths,
                BestFor = config.BestFor
            };
            configObjectList.Add(configObject);
        }

        // Create enum structure like MCPServerType with integer type
        var enumNames = aiModelConfigs.Select(c => c.Name).ToArray();
        var enumValues = aiModelConfigs.Select((c, i) => i).ToArray(); // Use integer indices

        // Inject the real configurations with enum structure
        extensionData["x-descriptions"] = configObjectList;
        extensionData["x-enumNames"] = enumNames;
        extensionData["enum"] = enumValues;
    }
}