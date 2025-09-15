using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aevatar.GAgents.Basic;
using NJsonSchema.Generation;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Schema;

/// <summary>
/// DynamicDropDown processor that coordinates with IDropDownSchemaProcess implementations 
/// to inject configuration data into property schemas based on DynamicDropDownAttribute.
/// </summary>
public class DynamicDropDownProcessor : ISchemaProcessor, ITransientDependency
{
    private readonly IEnumerable<IDropDownSchemaProcess> _schemaProcesses;
    private DynamicDropDownContext? _context;

    public DynamicDropDownProcessor(IEnumerable<IDropDownSchemaProcess> schemaProcesses)
    {
        _schemaProcesses = schemaProcesses;
    }
    
    public void SetContext(DynamicDropDownContext? context)
    {
        _context = context;
    }

    public void Process(SchemaProcessorContext context)
    {
        // Skip null schemas or if no properties exist
        if (context.Schema?.Properties == null) return;

        // Check all properties for DynamicDropDown attribute
        if (context.ContextualType?.Type != null)
        {
            var type = context.ContextualType.Type;
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (var property in properties)
            {
                var dynamicDropDownAttribute = property.GetCustomAttribute<DynamicDropDownAttribute>();
                if (dynamicDropDownAttribute == null) continue;

                // 获取 OptionName
                var optionName = dynamicDropDownAttribute.OptionName;
                if (string.IsNullOrEmpty(optionName)) continue;

                // 根据 OptionName 找到对应的处理器
                var schemaProcess = _schemaProcesses.FirstOrDefault(p => p.OptionName == optionName);
                if (schemaProcess == null) continue;

                // 调用对应的处理器进行Schema增强
                schemaProcess.ProcessSchema(context,property, _context);
            }
        }
    }
}