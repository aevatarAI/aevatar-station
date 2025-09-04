using System.Collections.Generic;
using Aevatar.Options;

namespace Aevatar.Schema;

/// <summary>
/// Context for schema processing that contains additional metadata and configuration
/// </summary>
public class SchemaProcessingContext
{
    /// <summary>
    /// AI model configurations available for dynamic dropdown processing
    /// </summary>
    public List<SystemLLMConfigDto>? AIModelConfigs { get; set; }
    
    /// <summary>
    /// Additional context data for processors
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; set; }

    public SchemaProcessingContext()
    {
        AdditionalData = new Dictionary<string, object>();
    }
}
