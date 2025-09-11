using System.Collections.Generic;
using Aevatar.Options;
using Orleans;

namespace Aevatar.Schema;

/// <summary>
/// Context for dynamic dropdown configuration during schema processing
/// </summary>
[GenerateSerializer]
public class DynamicDropDownContext
{
    /// <summary>
    /// Configuration data dictionary for dynamic dropdown processing
    /// </summary>
    [Id(0)]
    public Dictionary<string, object>? AIModelConfigs { get; set; }
    
    /// <summary>
    /// Additional context data for processors
    /// </summary>
    [Id(1)]
    public Dictionary<string, object>? AdditionalData { get; set; }

    public DynamicDropDownContext()
    {
        AIModelConfigs = new Dictionary<string, object>();
        AdditionalData = new Dictionary<string, object>();
    }
}
