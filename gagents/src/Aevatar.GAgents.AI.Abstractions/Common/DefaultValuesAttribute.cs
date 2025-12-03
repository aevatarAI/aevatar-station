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
    /// Descriptions for each default value, empty string if no description
    /// </summary>
    public string[] Descriptions { get; }
    
    /// <summary>
    /// Constructor with values only
    /// </summary>
    /// <param name="values">Default values list, first element as the default value</param>
    public DefaultValuesAttribute(params object[] values)
    {
        Values = values ?? new object[0];
        Descriptions = new string[Values.Length];
    }
    
    /// <summary>
    /// Constructor with values and descriptions
    /// </summary>
    /// <param name="values">Default values list, first element as the default value</param>
    /// <param name="descriptions">Descriptions for each value, use empty string if no description</param>
    public DefaultValuesAttribute(object[] values, string[] descriptions)
    {
        Values = values ?? new object[0];
        Descriptions = descriptions ?? new string[Values.Length];
        
        // Ensure descriptions array matches values length
        if (Descriptions.Length != Values.Length)
        {
            var adjustedDescriptions = new string[Values.Length];
            for (int i = 0; i < Values.Length; i++)
            {
                adjustedDescriptions[i] = i < Descriptions.Length ? Descriptions[i] ?? string.Empty : string.Empty;
            }
            Descriptions = adjustedDescriptions;
        }
    }
} 