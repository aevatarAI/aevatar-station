using System.ComponentModel.DataAnnotations;
using GroupChat.GAgent.Dto;

namespace Aevatar.GAgents.MCP.Options;

[GenerateSerializer]
public class MCPGAgentConfig : MemberConfigDto
{
    [Id(0)] 
    [Required(ErrorMessage = "Server configuration is required")]
    public MCPServerConfig ServerConfig { get; set; } = new();

    [Id(1)] 
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00", ErrorMessage = "Request timeout must be between 1 second and 1 day")]
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}