using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aevatar.GAgents.Basic;
using NJsonSchema.Generation;

namespace Aevatar.Schema;

/// <summary>
/// DynamicDropDown processor that injects real AI model configurations into property schemas.
/// Uses AIModelConfigs from DynamicDropDownContext provided by AgentService.
/// </summary>
public class DynamicDropDownProcessor : ISchemaProcessor
{
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

        // Strategy 1: Check if this schema represents a property with DynamicDropDown attribute
        if (context.ContextualType?.Type != null)
        {
            var type = context.ContextualType.Type;
            
            // Check if we're processing a specific property schema (not the main class)
            if (!string.IsNullOrEmpty(context.Schema.Title) && 
                !string.Equals(context.Schema.Title, type.Name, StringComparison.OrdinalIgnoreCase))
            {
                // This might be a property schema - try to find the corresponding property
                var property = FindPropertyBySchemaTitle(type, context.Schema.Title);
                if (property != null)
                {
                    var dynamicDropDownAttribute = property.GetCustomAttribute<DynamicDropDownAttribute>();
                    if (dynamicDropDownAttribute != null)
                    {
                        // Set schema type to integer like MCPServerType
                        context.Schema.Type = NJsonSchema.JsonObjectType.Integer;
                        InjectSystemLLMConfigurations(context.Schema.ExtensionData);
                        return;
                    }
                }
            }
            
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
                               
                                // Set schema type to integer like MCPServerType
                                propertySchema.Type = NJsonSchema.JsonObjectType.Integer;
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
        // Create a dictionary of AI model configurations from the real data
        var aiModelConfigs = new Dictionary<string, object>();
        
        foreach (var config in _context.AIModelConfigs)
        {
            aiModelConfigs[config.Name] = new
            {
                Name = config.Name,
                Provider = config.Provider,
                Type = config.Type,
                Strengths = config.Strengths,
                BestFor = config.BestFor,
                Speed = config.Speed
            };
        }
        
        // Create enum structure like MCPServerType with integer type
        var enumNames = _context.AIModelConfigs.Select(c => c.Name).ToArray();
        var enumValues = _context.AIModelConfigs.Select((c, i) => i).ToArray(); // Use integer indices
        
        // Inject the real configurations with enum structure
        extensionData["x-enumLLMConfigs"] = aiModelConfigs;
        extensionData["x-enumNames"] = enumNames;
        extensionData["enum"] = enumValues;
    }

    private PropertyInfo? FindPropertyBySchemaTitle(Type type, string schemaTitle)
    {
        // Try different naming strategies
        var strategies = new[]
        {
            schemaTitle,  // Exact match
            char.ToUpperInvariant(schemaTitle[0]) + schemaTitle.Substring(1), // camelCase -> PascalCase
        };
        
        foreach (var strategy in strategies)
        {
            var property = type.GetProperty(strategy, BindingFlags.Public | BindingFlags.Instance);
            if (property != null) return property;
        }
        
        return null;
    }
}