using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Aevatar.Mcp;

/// <summary>
/// DTO for updating an existing MCP server configuration
/// </summary>
public class UpdateMcpServerDto
{
    /// <summary>
    /// Command to execute the MCP server
    /// </summary>
    [StringLength(500, MinimumLength = 1)]
    public string? Command { get; set; }
    
    /// <summary>
    /// Command line arguments for the MCP server
    /// </summary>
    public List<string>? Args { get; set; }
    
    /// <summary>
    /// Environment variables for the MCP server
    /// </summary>
    public Dictionary<string, string>? Env { get; set; }
    
    /// <summary>
    /// Server description
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Server URL (optional, if empty indicates StdIO type, if present indicates other types like SSE/WebSocket/HTTP)
    /// </summary>
    [Url]
    public string? Url { get; set; }
}