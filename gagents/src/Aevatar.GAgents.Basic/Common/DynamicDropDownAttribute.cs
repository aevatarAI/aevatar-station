using System;

namespace Aevatar.GAgents.Basic;

/// <summary>
/// Indicates that this property should be rendered as a dynamic dropdown in the UI.
/// The dropdown options will be populated dynamically based on the property type and context.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class DynamicDropDownAttribute : Attribute
{
}
