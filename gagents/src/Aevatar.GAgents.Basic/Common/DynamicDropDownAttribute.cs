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
    /// Gets the option name that specifies which configuration provider to use for this dropdown.
    /// If null, the system will use default behavior ("systemLLMConfig").
    /// </summary>
    public string OptionName { get; }
    
    /// <summary>
    /// Initializes a new instance of the DynamicDropDownAttribute class.
    /// </summary>
    /// <param name="optionName">Optional. The name of the configuration provider to use for this dropdown. 
    /// Examples: "systemLLMConfig", "databaseConfig", etc. If not specified, defaults to "systemLLMConfig".</param>
    public DynamicDropDownAttribute(string? optionName = "systemLLMConfig")
    {
        OptionName = optionName ?? "systemLLMConfig";
    }
}
