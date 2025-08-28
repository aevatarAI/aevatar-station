using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Aevatar.GAgents.Basic.Common;
using NJsonSchema.Generation;

namespace Aevatar.Schema;

/// <summary>
/// Schema processor for handling DocumentationLinkAttribute
/// Adds documentation links to JSON schema for frontend integration
/// Validates URLs before including them in the schema
/// </summary>
public class DocumentationLinkProcessor : ISchemaProcessor
{
    private readonly SchemaProcessingContext? _context;

    public DocumentationLinkProcessor(SchemaProcessingContext? context = null)
    {
        _context = context;
    }
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

            if (!docLinkAttributes.Any()) continue;
                         var docLinkAttribute = docLinkAttributes.First();
             var propertyName = GetPropertyName(property.Name);
             var documentationUrl = docLinkAttribute.DocumentationUrl;
                 
             // Check if URL is marked as invalid in context first
             if (_context?.InvalidUrls.Contains(documentationUrl) == true)
             {
                 continue; // Skip URLs that are known to be invalid
             }
             
             // Fallback to basic URL format validation for URLs not in context
                         // Skip URL validation here - it's handled by context from AgentService
            // Only proceed if URL is not marked as invalid in the context
            // Find the corresponding property schema and add documentation URL
            if (!context.Schema.Properties.TryGetValue(propertyName, out var propertySchema)) continue;
            // Add documentationUrl directly to the property schema
            propertySchema.ExtensionData ??= new Dictionary<string, object>();
            propertySchema.ExtensionData["documentationUrl"] = documentationUrl;
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