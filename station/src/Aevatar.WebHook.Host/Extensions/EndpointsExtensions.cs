using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Webhook.SDK.Handler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Aevatar.Webhook.Extensions;

public static class EndpointsExtensions
{
    /// <param name="endpoints">Endpoints </param>
    /// <param name="webhookHandlers">webhookHandlers </param>
    /// <param name="webhookId"></param>
    /// <param name="logger"></param>
    public static void MapWebhookHandlers(this IEndpointRouteBuilder endpoints,
        IEnumerable<IWebhookHandler> webhookHandlers, string webhookId, ILogger<IWebhookHandler> logger)
    {
        foreach (var webhook in webhookHandlers)
        {
            switch (webhook.HttpMethod.ToUpperInvariant())
            {
                case "POST":
                    endpoints.MapPost(webhook.RelativePath,
                        async context => { await ExecuteWebhookHandler(webhook, context, logger); });
                    break;
                case "GET":
                    endpoints.MapGet(webhook.RelativePath,
                        async context => { await ExecuteWebhookHandler(webhook, context, logger); });
                    break;
                default:
                    logger.LogError("Unsupported HTTP method {Method} for webhook {Path}", webhook.HttpMethod,
                        webhook.RelativePath);
                    throw new NotSupportedException(
                        $"HTTP method {webhook.HttpMethod} is not supported for webhook {webhook.RelativePath}");
            }
        }
    }

    private static async Task ExecuteWebhookHandler(IWebhookHandler webhook, HttpContext context,
        ILogger<IWebhookHandler> logger)
    {
        try
        {
            var result = await webhook.HandleAsync(context.Request);

            context.Response.ContentType = "application/json";
            if (result != null)
            {
                await context.Response.WriteAsJsonAsync(result);
            }
            else
            {
                await context.Response.WriteAsync($"Webhook {webhook.RelativePath} successfully processed.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing webhook {RelativePath}", webhook.RelativePath);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync($"Error processing Webhook {webhook.RelativePath}: {ex.Message}");
        }
    }
}