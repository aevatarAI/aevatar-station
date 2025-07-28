using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Aevatar.ApiRequests;

public class ApiRequestStatisticsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IApiRequestProvider _apiRequestProvider;

    public ApiRequestStatisticsMiddleware(RequestDelegate next, IApiRequestProvider apiRequestProvider)
    {
        _next = next;
        _apiRequestProvider = apiRequestProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = context.User.FindFirst("client_id")?.Value;
        if(!clientId.IsNullOrWhiteSpace())
        {
            await _apiRequestProvider.IncreaseRequestAsync(clientId, DateTime.UtcNow);
        }
        
        await _next(context);
    }
}