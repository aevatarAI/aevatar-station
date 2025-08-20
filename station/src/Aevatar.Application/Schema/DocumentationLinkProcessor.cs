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
        var documentationLinks = new Dictionary<string, object>();

        // Get all properties that have DocumentationLinkAttribute
        var properties = classType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            var docLinkAttributes = property.GetCustomAttributes<DocumentationLinkAttribute>(true);
            
            if (docLinkAttributes.Any())
            {
                var docLinkAttribute = docLinkAttributes.First();
                var propertyName = GetPropertyName(property.Name);
                
                // Add documentation URL to the metadata
                documentationLinks[propertyName] = new
                {
                    documentationUrl = docLinkAttribute.DocumentationUrl
                };
            }
        }

        // If we found any documentation links, add them to the schema
        if (documentationLinks.Any())
        {
            AddDocumentationLinksToSchema(context, documentationLinks);
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

    /// <summary>
    /// Add documentation links metadata to the schema's extension data
    /// </summary>
    private void AddDocumentationLinksToSchema(SchemaProcessorContext context, Dictionary<string, object> documentationLinks)
    {
        if (context.Schema.ExtensionData == null)
        {
            context.Schema.ExtensionData = new Dictionary<string, object>();
        }

        // Add documentation links under x-documentationLinks extension
        context.Schema.ExtensionData["x-documentationLinks"] = documentationLinks;
    }
}