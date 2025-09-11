using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using GroupChat.GAgent.Dto;

namespace Aevatar.GAgents.MCP.Options;

[GenerateSerializer]
public class MCPGAgentConfig : MemberConfigDto
{
    [Id(0)] 
    [Required(ErrorMessage = "Server configuration is required")]
    [Description("Configuration for the MCP (Model Context Protocol) server that provides tools and capabilities")]
    public MCPServerConfig ServerConfig { get; set; } = new();

    [Id(1)] 
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00", ErrorMessage = "Request timeout must be between 1 second and 1 day")]
    [Description("Timeout duration for MCP server requests to prevent hanging operations")]
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}