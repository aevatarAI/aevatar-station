using System;
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
            Console.WriteLine("[DynamicDropDownProcessor] No dropdown context provided, skipping");
            return;
        }
        
        Console.WriteLine($"[DynamicDropDownProcessor] Processing schema for: '{context.Schema?.Title}' (Type: {context.ContextualType?.Type?.Name})");

        ProcessDynamicDropDown(context);
    }

    private void ProcessDynamicDropDown(SchemaProcessorContext context)
    {
        Console.WriteLine($"[DynamicDropDownProcessor] ProcessDynamicDropDown called");
        Console.WriteLine($"[DynamicDropDownProcessor] ContextualType: {context.ContextualType?.Type?.Name}");
        Console.WriteLine($"[DynamicDropDownProcessor] Title: {context.Schema?.Title}");
        
        // Check if this is a property being processed
        if (context.ContextualType?.Type == null || context.Schema?.Title == null)
            return;
            
        var parentType = context.ContextualType.Type;
        var propertyName = context.Schema.Title;
        
        Console.WriteLine($"[DynamicDropDownProcessor] Checking property '{propertyName}' on type '{parentType.Name}'");
        
        // Skip if this is the class itself (not a property)
        if (string.Equals(propertyName, parentType.Name, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[DynamicDropDownProcessor] Skipping class definition: {parentType.Name}");
            return;
        }
        
        // Try to find the property using reflection with multiple strategies
        PropertyInfo? property = null;
        
        // Strategy 1: Case-insensitive lookup
        property = parentType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        
        // Strategy 2: Handle camelCase JSON naming - convert first char to uppercase (systemLLM -> SystemLLM)
        if (property == null && !string.IsNullOrEmpty(propertyName))
        {
            var pascalCaseName = char.ToUpper(propertyName[0]) + propertyName.Substring(1);
            property = parentType.GetProperty(pascalCaseName, BindingFlags.Public | BindingFlags.Instance);
            Console.WriteLine($"[DynamicDropDownProcessor] Trying PascalCase: '{pascalCaseName}' for JSON field '{propertyName}'");
        }
        
        if (property != null)
        {
            Console.WriteLine($"[DynamicDropDownProcessor] Found property via reflection: {property.Name}");
            var dynamicDropDownAttribute = property.GetCustomAttribute<DynamicDropDownAttribute>();
            
            if (dynamicDropDownAttribute != null)
            {
                Console.WriteLine($"[DynamicDropDownProcessor] Found [DynamicDropDown] attribute on {property.Name}, adding metadata");
                AddDynamicDropDownMetadata(context);
            }
            else
            {
                Console.WriteLine($"[DynamicDropDownProcessor] No [DynamicDropDown] attribute found on {property.Name}");
            }
        }
        else
        {
            Console.WriteLine($"[DynamicDropDownProcessor] Could not find property '{propertyName}' on type {parentType.Name}");
            
            // Debug: List all properties on the type
            var allProperties = parentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Console.WriteLine($"[DynamicDropDownProcessor] Available properties on {parentType.Name}: {string.Join(", ", allProperties.Select(p => p.Name))}");
        }
    }

    private void AddDynamicDropDownMetadata(SchemaProcessorContext context)
    {
        Console.WriteLine("[DynamicDropDownProcessor] Adding dynamic dropdown metadata");
        
        if (context.Schema.ExtensionData == null)
        {
            context.Schema.ExtensionData = new Dictionary<string, object>();
        }

        // 先添加一个简单的测试字段验证processor工作
        context.Schema.ExtensionData["x-dynamic-dropdown-test"] = "found-dynamic-dropdown-attribute";
        
        Console.WriteLine("[DynamicDropDownProcessor] Added test field x-dynamic-dropdown-test");

        // 如果有AI model配置，再添加复杂的逻辑
        if (_dropDownContext?.AIModelConfigs != null && _dropDownContext.AIModelConfigs.Any())
        {
            var configs = _dropDownContext.AIModelConfigs;
            Console.WriteLine($"[DynamicDropDownProcessor] Found {configs.Count} AI model configs");
            
            // 添加配置数据
            context.Schema.ExtensionData["x-enumLLMConfigs"] = configs;
            
            // 生成模型名称列表
            var enumValues = configs.Select(config => config.Name).ToArray();
            context.Schema.ExtensionData["x-enumNames"] = enumValues;
            
            Console.WriteLine($"[DynamicDropDownProcessor] Added configs: {string.Join(", ", enumValues)}");
        }
        else
        {
            Console.WriteLine("[DynamicDropDownProcessor] No AI model configs found in context");
        }
    }
}
