using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aevatar.GAgents.Basic;
using Aevatar.Options;
using NJsonSchema.Generation;

namespace Aevatar.Schema;

/// <summary>
/// DynamicDropDown processor that injects real AI model configurations into property schemas.
/// Uses AIModelConfigs from DynamicDropDownContext provided by AgentService.
/// </summary>
public class DynamicDropDownProcessor : ISchemaProcessor
{
    private const string AI_MODEL_CONFIGS_KEY = "systemLLMConfig";
    private readonly DynamicDropDownContext? _context;

    public DynamicDropDownProcessor(DynamicDropDownContext? context = null)
    {
        _context = context;
    }

    public void Process(SchemaProcessorContext context)
    {
        // Skip null schemas
        if (context.Schema == null) return;

        // Initialize ExtensionData if needed
        if (context.Schema.ExtensionData == null)
        {
            context.Schema.ExtensionData = new Dictionary<string, object>();
        }

        // Check all properties for DynamicDropDown attribute
        if (context.ContextualType?.Type != null)
        {
            var type = context.ContextualType.Type;
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var dynamicDropDownAttribute = property.GetCustomAttribute<DynamicDropDownAttribute>();
                if (dynamicDropDownAttribute != null)
                {   
                    // Try to find the property schema in the current schema's properties
                    if (context.Schema.Properties != null && context.Schema.Properties.Count > 0)
                    {
                        // Look for the property schema using different naming conventions
                        string[] possibleKeys = { 
                            property.Name.ToLowerInvariant(),           // systemllm
                            char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1), // systemLLM -> systemLLM
                            property.Name                                // SystemLLM
                        };
                        
                        foreach (var key in possibleKeys)
                        {
                            if (context.Schema.Properties.TryGetValue(key, out var propertySchema))
                            {   
                                if (propertySchema.ExtensionData == null)    propertySchema.ExtensionData = new Dictionary<string, object>();
                               
                                InjectSystemLLMConfigurations(propertySchema.ExtensionData);
                              
                                return;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Injects real SystemLLM configurations from the context into the property schema.
    /// </summary>
    private void InjectSystemLLMConfigurations(IDictionary<string, object?> extensionData)
    {  
        // Get AI model configurations from the dictionary
        if (_context?.AIModelConfigs == null || 
            !_context.AIModelConfigs.TryGetValue(AI_MODEL_CONFIGS_KEY, out var aiModelConfigsObj) ||
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
                Provider = config.Provider,
                Type = config.Type,
                Strengths = config.Strengths,
                BestFor = config.BestFor,
                Speed = config.Speed
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