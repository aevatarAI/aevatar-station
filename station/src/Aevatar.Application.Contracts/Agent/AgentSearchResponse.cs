using System.Collections.Generic;
using Aevatar.Agent;

namespace Aevatar.Agent;

// Agent search response DTO
public class AgentSearchResponse
{
    // Agent list
    public List<AgentInstanceDto> Agents { get; set; } = new List<AgentInstanceDto>();
    
    // Total count
    public int Total { get; set; }
    
    // Page index
    public int PageIndex { get; set; }
    
    // Page size
    public int PageSize { get; set; }
    
    // Whether there is more data
    public bool HasMore { get; set; }
} 