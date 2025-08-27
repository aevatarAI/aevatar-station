using Microsoft.AspNetCore.Http;

namespace Aevatar.Webhook;

public class DestroyWebhookDto

{
    public string WebhookId{ get; set; }
    public string Version{ get; set; }
}