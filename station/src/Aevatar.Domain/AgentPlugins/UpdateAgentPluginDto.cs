using System;
using Microsoft.AspNetCore.Http;

namespace Aevatar.AgentPlugins;

public class UpdateAgentPluginDto
{
    public Guid PluginCodeId { get; set; }
    public IFormFile Code { get; set; }
}