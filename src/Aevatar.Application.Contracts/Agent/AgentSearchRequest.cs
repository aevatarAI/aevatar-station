using System.Collections.Generic;

namespace Aevatar.Agent;

// Agent search filter request DTO
public class AgentSearchRequest
{
    // Search keyword (matches name, description)
    public string? SearchTerm { get; set; }
    
    // Multi-type filter ["ChatAgent", "WorkflowAgent"]
    public List<string>? Types { get; set; }
    
    // Sort field CreateTime/Name/UpdateTime/Relevance
    public string? SortBy { get; set; } = "CreateTime";
    
    // Sort direction Asc/Desc
    public string? SortOrder { get; set; } = "Desc";
} 