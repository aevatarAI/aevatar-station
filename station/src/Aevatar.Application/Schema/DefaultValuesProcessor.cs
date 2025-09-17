using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Aevatar.GAgents.AI.Common;
using NJsonSchema.Generation;

namespace Aevatar.Schema;

/// <summary>
/// Schema processor for handling DefaultValuesAttribute
/// Adds descriptions to JSON schema for frontend integration
/// Provides description information for each default value option
/// </summary>
public class DefaultValuesProcessor : ISchemaProcessor
{
    public void Process(SchemaProcessorContext context)
    {
        // Handle null context gracefully
        if (context == null)
        {
            return;
        }

        // Only process class/object types, not enums or primitives
        if (context.ContextualType.Type.IsClass && !context.ContextualType.Type.IsEnum)
        {
            ProcessDefaultValuesDescriptions(context);
        }
    }

    private void ProcessDefaultValuesDescriptions(SchemaProcessorContext context)
    {
        var classType = context.ContextualType.Type;

        // Get all properties that have DefaultValuesAttribute
        var properties = classType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            var defaultValuesAttributes = property.GetCustomAttributes<DefaultValuesAttribute>(true);

            if (!defaultValuesAttributes.Any()) continue;
            
            var defaultValuesAttribute = defaultValuesAttributes.First();
            var propertyName = GetPropertyName(property.Name);
            var descriptions = defaultValuesAttribute.Descriptions;
                
            // Find the corresponding property schema and add descriptions
            if (!context.Schema.Properties.TryGetValue(propertyName, out var propertySchema)) continue;
            
            // Only add x-descriptions if there's at least one non-empty description
            // and the descriptions array length matches values array length
            var hasValidDescriptions = descriptions != null && 
                                     descriptions.Length == defaultValuesAttribute.Values.Length &&
                                     descriptions.Any(d => !string.IsNullOrEmpty(d));
            
            if (hasValidDescriptions)
            {
                // Add x-descriptions to the property schema
                propertySchema.ExtensionData ??= new Dictionary<string, object>();
                propertySchema.ExtensionData["x-descriptions"] = descriptions;
            }
        }
    }

    /// <summary>
    /// Convert property name to camelCase to match JSON naming convention
    /// </summary>
    private string GetPropertyName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return propertyName;
            
        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}
