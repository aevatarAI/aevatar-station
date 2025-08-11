using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace Aevatar.Mcp;

/// <summary>
/// DTO for filtering MCP server list requests with enhanced pagination support
/// </summary>
public class GetMcpServerListDto : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Default constructor with sensible defaults
    /// </summary>
    public GetMcpServerListDto()
    {
        MaxResultCount = 10; // Default page size
        SkipCount = 0;
        Sorting = "serverName asc"; // Default sorting
    }

    /// <summary>
    /// Filter by server name (partial match)
    /// </summary>
    public string? ServerName { get; set; }
    
    /// <summary>
    /// Filter by server type ("Stdio" or "StreamableHttp")
    /// </summary>
    public string? ServerType { get; set; }
    
    /// <summary>
    /// Search term for server name, command, or description
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Override MaxResultCount with validation to prevent excessive results
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public override int MaxResultCount { get; set; } = 10;

    /// <summary>
    /// Page number (1-based), calculated from SkipCount
    /// </summary>
    public int PageNumber 
    { 
        get => (SkipCount / MaxResultCount) + 1;
        set => SkipCount = (value - 1) * MaxResultCount;
    }

    /// <summary>
    /// Available sorting fields for reference
    /// </summary>
    public static readonly string[] AvailableSortFields = 
    {
        "serverName", "command", "description", "serverType", "createdAt", "modifiedAt"
    };

    /// <summary>
    /// Get a user-friendly sorting description
    /// </summary>
    public string GetSortingDescription()
    {
        if (string.IsNullOrEmpty(Sorting))
            return "Default (Server Name Ascending)";

        var parts = Sorting.Split(' ');
        var field = parts[0];
        var direction = parts.Length > 1 && parts[1].ToLower() == "desc" ? "Descending" : "Ascending";
        
        return $"{field} ({direction})";
    }
}