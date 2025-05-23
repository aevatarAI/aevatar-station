using Microsoft.AspNetCore.Http;

namespace Aevatar.AgentPlugins;

public class CreateAgentPluginDto
{
    public IFormFile Code { get; set; }
}