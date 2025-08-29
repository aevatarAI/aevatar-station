using System;

namespace Aevatar.GAgents.AI.Common;

/// <summary>
/// Default values attribute for defining default value lists for Agent configuration properties
/// values[0] is always the default value, supports single default value or multiple options
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DefaultValuesAttribute : Attribute
{
    /// <summary>
    /// Default values list, values[0] as the default value
    /// </summary>
    public object[] Values { get; }
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="values">Default values list, first element as the default value</param>
    public DefaultValuesAttribute(params object[] values)
    {
        Values = values ?? new object[0];
    }
} 