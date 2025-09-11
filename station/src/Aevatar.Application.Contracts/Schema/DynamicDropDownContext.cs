using System;
using System.Collections.Generic;
using Aevatar.Options;
using Orleans;

namespace Aevatar.Schema;

/// <summary>
/// Context for dynamic dropdown configuration during schema processing
/// All configuration data is stored in the AdditionalData dictionary
/// </summary>
[GenerateSerializer]
public class DynamicDropDownContext
{
    /// <summary>
    /// All configuration data for dynamic dropdown processing
    /// Various processors can store their configuration data using different keys
    /// </summary>
    [Id(0)]
    public Dictionary<string, object>? AdditionalData { get; set; }

    public DynamicDropDownContext()
    {
        AdditionalData = new Dictionary<string, object>();
    }
}
