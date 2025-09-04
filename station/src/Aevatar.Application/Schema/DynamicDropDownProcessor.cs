using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Aevatar.GAgents.Basic;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Aevatar.Schema;

public class DynamicDropDownProcessor : ISchemaProcessor
{
    private readonly DynamicDropDownContext? _dropDownContext;

    public DynamicDropDownProcessor() : this(null)
    {
    }

    public DynamicDropDownProcessor(DynamicDropDownContext? dropDownContext)
    {
        _dropDownContext = dropDownContext;
    }

    public void Process(SchemaProcessorContext context)
    {
        // If no dropdown context is provided, skip processing
        if (_dropDownContext == null)
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
        if (_dropDownContext?.AIModelConfigs != null && _dropDownContext.AIModelConfigs.Any())
        {
            var configs = _dropDownContext.AIModelConfigs;
            
            // Set schema type to integer
            context.Schema.Type = JsonObjectType.Integer;
            
            // Create enum names array from AIModelConfigs names
            var enumNames = configs.Select(config => config.Name).ToArray();
            context.Schema.ExtensionData["x-enumNames"] = enumNames;
            
            // Create enum values array (0, 1, 2, ...)
            var enumValues = Enumerable.Range(0, configs.Count).ToArray();
            context.Schema.Enumeration.Clear();
            foreach (var value in enumValues)
            {
                context.Schema.Enumeration.Add(value);
            }
            
            // Keep x-enumLLMConfigs with full configurations in corresponding order
            context.Schema.ExtensionData["x-enumLLMConfigs"] = configs;
            
            // Generate description showing enum mappings
            var descriptionLines = configs.Select((config, index) => $"{index} = {config.Name}");
            context.Schema.Description = string.Join("\n", descriptionLines);
        }
    }
}
