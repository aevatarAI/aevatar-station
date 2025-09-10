using System;

namespace Aevatar.GAgents.Basic;

/// <summary>
/// Indicates that this property should be rendered as a dynamic dropdown in the UI.
/// The dropdown options will be populated dynamically based on the specified option source or property type and context.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DynamicDropDownAttribute : Attribute
{
    /// <summary>
    /// Gets the option source name that specifies which configuration field to retrieve metadata from.
    /// If null, the system will use default behavior to determine the appropriate option source.
    /// </summary>
    public string? OptionSource { get; }
    
    /// <summary>
    /// Initializes a new instance of the DynamicDropDownAttribute class.
    /// </summary>
    /// <param name="optionSource">Optional. The name of the option source field to retrieve metadata from. 
    /// Examples: "SystemLLMConfigs", "DatabaseConfigs", etc. If not specified, default behavior is used.</param>
    public DynamicDropDownAttribute(string? optionSource = null)
    {
        OptionSource = optionSource;
    }
}
