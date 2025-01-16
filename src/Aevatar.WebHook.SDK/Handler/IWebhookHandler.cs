using Microsoft.AspNetCore.Http;

namespace Aevatar.Listener.SDK.Handler;

public interface IWebhookHandler
{
        string RelativePath  { get; }
        
        Task HandleAsync(HttpRequest request);
        
        string HttpMethod { get; }
        
        public string GetFullPath(string webhookId)
        { 
           return $"{webhookId}/{RelativePath}";
        }
}