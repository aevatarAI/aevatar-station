using Microsoft.AspNetCore.Http;

namespace Aevatar.Webhook;

public class CreateWebhookDto
{
    public IFormFileCollection Code { get; set; }
}