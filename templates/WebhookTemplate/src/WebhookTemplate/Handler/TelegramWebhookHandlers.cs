using Aevatar.Webhook.SDK.Handler;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aevatar.Webhook.Template.Handler;

public class TelegramWebhookHandler : IWebhookHandler
{
    private readonly ILogger<TelegramWebhookHandler> _logger;

    public TelegramWebhookHandler(ILogger<TelegramWebhookHandler> logger)
    {
        _logger = logger;
    }

    public string RelativePath => "api/webhooks/telegram";
    
    public string HttpMethod => "POST";

    public async Task<object> HandleAsync(HttpRequest request)
    {
        var headers = request.Headers;
        var token = headers["X-Telegram-Bot-Api-Secret-Token"].ToString();
        _logger.LogInformation("Receive update message from telegram.{specificHeader}", token);
        using var reader = new StreamReader(request.Body);
        var bodyString = await reader.ReadToEndAsync();
        _logger.LogInformation("Receive update message from telegram.{message}", bodyString);
        return bodyString;
    }

   
}