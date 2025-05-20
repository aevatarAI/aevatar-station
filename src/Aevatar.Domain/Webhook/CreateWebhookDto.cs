using Microsoft.AspNetCore.Http;

namespace Aevatar.Webhook;

public class CreateWebhookDto

{
    public IFormFile Code { get; set; }
   
}