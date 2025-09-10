using System;
using Microsoft.AspNetCore.Http;

namespace Aevatar.Plugins;

public class CreatePluginDto
{
    public Guid ProjectId { get; set; }
    public IFormFile Code { get; set; }
}