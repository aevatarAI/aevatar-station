using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Mcp;

/// <summary>
/// DTO for creating a new MCP server configuration
/// </summary>
public class CreateMcpServerDto
{
    /// <summary>
    /// Server name (unique identifier)
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string ServerName { get; set; } = string.Empty;
    
    /// <summary>
    /// Command to execute the MCP server
    /// </summary>
    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Command { get; set; } = string.Empty;
    
    /// <summary>
    /// Command line arguments for the MCP server
    /// </summary>
    public List<string> Args { get; set; } = [];
    
    /// <summary>
    /// Environment variables for the MCP server
    /// </summary>
    public Dictionary<string, string> Env { get; set; } = new();
    
    /// <summary>
    /// Server description
    /// </summary>
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Server URL (optional, for certain server types)
    /// </summary>
    [Url]
    public string? Url { get; set; }
}