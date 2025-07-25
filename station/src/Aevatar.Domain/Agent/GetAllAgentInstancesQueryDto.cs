using System.ComponentModel.DataAnnotations;

namespace Aevatar.Agent;

public class GetAllAgentInstancesQueryDto
{
    /// <summary>
    /// Page index, starting from 0
    /// </summary>
    public int PageIndex { get; set; } = 0;
    
    /// <summary>
    /// Page size
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
    
    /// <summary>
    /// Agent type fuzzy query condition
    /// </summary>
    public string? AgentType { get; set; }
} 