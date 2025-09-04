using System.Collections.Generic;
using Aevatar.Options;

namespace Aevatar.Schema;

/// <summary>
/// Context for dynamic dropdown configuration during schema processing
/// </summary>
public class DynamicDropDownContext
{
    /// <summary>
    /// AI model configurations available for dynamic dropdown processing
    /// </summary>
    public List<SystemLLMConfigDto>? AIModelConfigs { get; set; }
    
    /// <summary>
    /// Additional context data for processors
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }

    public DynamicDropDownContext()
    {
        AdditionalData = new Dictionary<string, object>();
    }
}
