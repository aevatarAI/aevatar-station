using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.PermissionManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using OpenIddict.Abstractions;
using Orleans.Runtime;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;

namespace Aevatar.Handler;

public class AevatarAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new AuthorizationMiddlewareResultHandler();
    public  async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {

        var user = context.User;
        if (user.Identity!.IsAuthenticated)
        {
          
            var userId = user.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
            var roles = user.FindAll(OpenIddictConstants.Claims.Role).Select(c => c.Value).ToArray();
            if (userId!= null)
            {
                RequestContext.Set("CurrentUser", new UserContext
                {
                    UserId = userId.ToGuid(),
                    Roles = roles,
                });
            }
        }
        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}