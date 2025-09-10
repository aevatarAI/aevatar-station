using Microsoft.AspNetCore.Http;

namespace Aevatar.Plugins;

public class UpdatePluginDto
{
    public IFormFile Code { get; set; }
}