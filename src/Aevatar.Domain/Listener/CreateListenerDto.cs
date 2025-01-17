using Microsoft.AspNetCore.Http;

namespace Aevatar.Listener;

public class CreateListenerDto
{
    public IFormFile Code { get; set; }
}