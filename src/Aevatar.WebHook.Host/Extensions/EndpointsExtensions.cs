using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Webhook.SDK.Handler;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aevatar.Webhook.Extensions;

public static class EndpointsExtensions
{
    /// <param name="endpoints">Endpoints </param>
    /// <param name="webhookHandlers">webhookHandlers </param>
    /// <param name="webhookId"></param>
    public static void MapWebhookHandlers(this IEndpointRouteBuilder endpoints, IEnumerable<IWebhookHandler> webhookHandlers,string webhookId)
    {
        foreach (var webhook in webhookHandlers)
        {
            switch (webhook.HttpMethod.ToUpperInvariant())
            {
                case "POST":
                    endpoints.MapPost(webhook.GetFullPath(webhookId), async context =>
                    {
                        await ExecuteWebhookHandler(webhook, context);
                    });
                    break;
                case "GET":
                    endpoints.MapGet(webhook.GetFullPath(webhookId), async context =>
                    {
                        await ExecuteWebhookHandler(webhook, context);
                    });
                    break;
                default:
                    throw new NotSupportedException($"HTTP method {webhook.HttpMethod} is not supported for webhook {webhook.RelativePath}");
            }
        }
    }
    
    private static async Task ExecuteWebhookHandler(IWebhookHandler webhook, HttpContext context)
    {
        try
        {
            await webhook.HandleAsync(context.Request);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync($"Webhook {webhook.RelativePath} successfully processed.");
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync($"Error processing Webhook {webhook.RelativePath}: {ex.Message}");
        }
    }
}