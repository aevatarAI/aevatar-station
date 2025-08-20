using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Aevatar.GAgents.Basic.Common;
using NJsonSchema.Generation;

namespace Aevatar.Schema;

/// <summary>
/// Schema processor for handling DocumentationLinkAttribute
/// Adds documentation links to JSON schema for frontend integration
/// </summary>
public class DocumentationLinkProcessor : ISchemaProcessor
{
    public void Process(SchemaProcessorContext context)
    {
        // Only process class/object types, not enums or primitives
        if (context.ContextualType.Type.IsClass && !context.ContextualType.Type.IsEnum)
        {
            ProcessDocumentationLinks(context);
        }
    }

    private void ProcessDocumentationLinks(SchemaProcessorContext context)
    {
        var classType = context.ContextualType.Type;

        // Get all properties that have DocumentationLinkAttribute
        var properties = classType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            var docLinkAttributes = property.GetCustomAttributes<DocumentationLinkAttribute>(true);
            
            if (docLinkAttributes.Any())
            {
                var docLinkAttribute = docLinkAttributes.First();
                var propertyName = GetPropertyName(property.Name);
                
                // Find the corresponding property schema and add documentation URL directly
                if (context.Schema.Properties.TryGetValue(propertyName, out var propertySchema))
                {
                    // Add documentationUrl directly to the property schema
                    if (propertySchema.ExtensionData == null)
                    {
                        propertySchema.ExtensionData = new Dictionary<string, object>();
                    }
                    propertySchema.ExtensionData["documentationUrl"] = docLinkAttribute.DocumentationUrl;
                }
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