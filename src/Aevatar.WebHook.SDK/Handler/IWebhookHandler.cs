using Microsoft.AspNetCore.Http;

namespace Aevatar.Webhook.SDK.Handler;

public interface IWebhookHandler
{
        string RelativePath  { get; }
        
        Task<Object> HandleAsync(HttpRequest request);
        
        string HttpMethod { get; }
        
        public string GetFullPath(string webhookId)
        { 
           return $"/{webhookId}/{RelativePath}";
        }
}