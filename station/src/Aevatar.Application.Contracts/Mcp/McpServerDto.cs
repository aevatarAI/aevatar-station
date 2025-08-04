using System;
using System.Collections.Generic;

namespace Aevatar.Mcp;

/// <summary>
/// DTO representing an MCP server configuration
/// </summary>
public class McpServerDto
{
    /// <summary>
    /// Server name (unique identifier)
    /// </summary>
    public string ServerName { get; set; } = string.Empty;
    
    /// <summary>
    /// Command to execute the MCP server
    /// </summary>
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
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Server URL (optional, if empty indicates StdIO type, if present indicates other types like SSE/WebSocket/HTTP)
    /// </summary>
    public string? Url { get; set; }
    
    /// <summary>
    /// Server type based on URL presence (read-only)
    /// </summary>
    public string ServerType => string.IsNullOrEmpty(Url) ? "Stdio" : "StreamableHttp";
    
    /// <summary>
    /// Server creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
}