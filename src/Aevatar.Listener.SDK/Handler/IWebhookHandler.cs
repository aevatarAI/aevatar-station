using Microsoft.AspNetCore.Http;

namespace Aevatar.Listener.SDK.Handler;

public interface IWebhookHandler
{
        string Path { get; }
        
        Task HandleAsync(HttpRequest request);
        
        string HttpMethod { get; }
}