using System.Collections.Generic;

namespace Aevatar.Schema;

/// <summary>
/// Context for schema processing operations
/// Provides additional information to schema processors during schema generation
/// </summary>
public class SchemaProcessingContext
{
    /// <summary>
    /// Set of invalid/inaccessible documentation URLs that should be excluded from schema
    /// </summary>
    public HashSet<string> InvalidUrls { get; set; } = new HashSet<string>();
}
