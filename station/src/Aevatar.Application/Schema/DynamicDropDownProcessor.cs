using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Aevatar.GAgents.Basic;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Aevatar.Schema;

public class DynamicDropDownProcessor : ISchemaProcessor
{
    private readonly SchemaProcessingContext? _processingContext;

    public DynamicDropDownProcessor() : this(null)
    {
    }

    public DynamicDropDownProcessor(SchemaProcessingContext? processingContext)
    {
        _processingContext = processingContext;
    }

    public void Process(SchemaProcessorContext context)
    {
        // If no processing context is provided, skip processing
        if (_processingContext == null)
        {
            return;
        }
        
        // Check if this is a property schema processing by checking schema type and title
        if (context.Schema?.Type == JsonObjectType.String && !string.IsNullOrEmpty(context.Schema.Title))
        {
            ProcessDynamicDropDown(context);
        }
    }

    private void ProcessDynamicDropDown(SchemaProcessorContext context)
    {
        // Try to get property info from the parent type using reflection
        var parentType = context.ContextualType?.Type;
        var propertyName = context.Schema?.Title;
        
        if (parentType == null || string.IsNullOrEmpty(propertyName))
            return;
            
        var property = parentType.GetProperty(propertyName);
        
        if (property != null)
        {
            var dynamicDropDownAttribute = property.GetCustomAttribute<DynamicDropDownAttribute>();
            
            if (dynamicDropDownAttribute != null)
            {
                AddDynamicDropDownMetadata(context);
            }
        }
    }

    private void AddDynamicDropDownMetadata(SchemaProcessorContext context)
    {
        if (context.Schema.ExtensionData == null)
        {
            context.Schema.ExtensionData = new Dictionary<string, object>();
        }

        // Add AI model configurations if available in context
        if (_processingContext?.AIModelConfigs != null && _processingContext.AIModelConfigs.Any())
        {
            context.Schema.ExtensionData["x-enumLLMConfigs"] = _processingContext.AIModelConfigs;
        }
    }
}
