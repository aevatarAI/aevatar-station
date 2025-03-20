using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.PermissionManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using Orleans.Runtime;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;

namespace Aevatar.Handler;

public class AevatarAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new AuthorizationMiddlewareResultHandler();
    private readonly ILogger<AevatarAuthorizationMiddlewareResultHandler> _logger;

    public AevatarAuthorizationMiddlewareResultHandler(ILogger<AevatarAuthorizationMiddlewareResultHandler> logger)
    {
        _logger = logger;
    }

    public  async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
         
        var user = context.User;
      
        if (user.Identity!.IsAuthenticated)
        {
            var claims = "";
            foreach (var claim in user.Claims)
            {
                claims += claim.Type + ":  "+ claim.Value + "    ";
            }
            _logger.LogInformation("claims:{claims}",claims);
            var userId = user.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
            var roles = user.FindAll(OpenIddictConstants.Claims.Role).Select(c => c.Value).ToArray();
            var clientId = user.FindFirst(OpenIddictConstants.Claims.ClientId)?.Value;
            
            {
                RequestContext.Set("CurrentUser", new UserContext
                {
                    UserId = !userId.IsNullOrEmpty() ? userId.ToGuid(): Guid.Empty,
                    Roles = roles,
                    ClientId= clientId,
                });
            }
        }
        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}