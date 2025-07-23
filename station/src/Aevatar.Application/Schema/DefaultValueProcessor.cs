using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NJsonSchema.Generation;

namespace Aevatar.Schema;

/// <summary>
/// Schema处理器：为属性添加默认值支持
/// </summary>
public class DefaultValueProcessor : ISchemaProcessor
{
    public void Process(SchemaProcessorContext context)
    {
        if (context.Schema.Properties != null)
        {
            var typeProperties = context.Type.GetProperties();
            
            foreach (var property in typeProperties)
            {
                var propertyName = GetJsonPropertyName(property.Name);
                
                if (context.Schema.Properties.TryGetValue(propertyName, out var propertySchema))
                {
                    // 添加values字段
                    var values = GetPropertyValues(property);
                    if (values.Any())
                    {
                        propertySchema.ExtensionData["values"] = JArray.FromObject(values);
                    }
                }
            }
        }
    }
    
    private string GetJsonPropertyName(string propertyName)
    {
        // 转为camelCase
        return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
    }
    
    private List<object> GetPropertyValues(PropertyInfo property)
    {
        // 1. 检查自定义DefaultValues特性
        var valuesAttr = property.GetCustomAttribute<DefaultValuesAttribute>();
        if (valuesAttr != null)
        {
            return valuesAttr.Values.ToList();
        }
        
        // 2. 检查系统DefaultValue特性
        var defaultValueAttr = property.GetCustomAttribute<DefaultValueAttribute>();
        if (defaultValueAttr != null)
        {
            return new List<object> { defaultValueAttr.Value };
        }
        
        // 3. 尝试从实例获取默认值
        try
        {
            if (property.DeclaringType != null)
            {
                var instance = Activator.CreateInstance(property.DeclaringType);
                var value = property.GetValue(instance);
                if (value != null)
                {
                    return new List<object> { value };
                }
            }
        }
        catch { }
        
        return new List<object>();
    }
}

/// <summary>
/// 默认值特性：支持多个预设值
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DefaultValuesAttribute : Attribute
{
    public object[] Values { get; }
    
    public DefaultValuesAttribute(params object[] values)
    {
        Values = values ?? new object[0];
    }
} 