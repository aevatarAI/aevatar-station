using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.PermissionManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Orleans.Runtime;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;

namespace Aevatar.Handler;

public class AevatarAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new AuthorizationMiddlewareResultHandler();
    private readonly IPermissionManager _permissionManager; 
    
    public AevatarAuthorizationMiddlewareResultHandler(IPermissionManager permissionManager)
    {
        _permissionManager = permissionManager;
    }
    public  async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {

        var user = context.User;
        if (user.Identity!.IsAuthenticated)
        {
          
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = user.FindAll(AbpClaimTypes.Role).Select(c => c.Value).ToArray();
            RequestContext.Set("CurrentUser", new UserContext
            {
                UserId = userId.ToGuid(),
                Roles = roles,
            });
        }
        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}