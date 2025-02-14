using System.Reflection;
using System.Security.Claims;
using Aevatar.Core.Abstractions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Security.Claims;

namespace Aevatar;

public class PermissionCheckFilter : IIncomingGrainCallFilter
{
    private readonly IPermissionChecker _permissionChecker;

    public PermissionCheckFilter(IPermissionChecker permissionChecker)
    {
        _permissionChecker = permissionChecker;
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var method = context.ImplementationMethod;
        var permissionAttribute = method.GetCustomAttribute<PermissionAttribute>();

        if (permissionAttribute != null)
        {
            if (RequestContext.Get("CurrentUser") is not UserContext currentUser) return;

            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity([
                    new Claim(AbpClaimTypes.UserId, currentUser!.UserId.ToString()),
                    new Claim(ClaimTypes.Role, currentUser.Role)
                ], "Bearer"));

            if (claimsPrincipal.Identity is not { IsAuthenticated: true })
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var isGranted = await _permissionChecker.IsGrantedAsync(
                claimsPrincipal,
                permissionAttribute.Name
            );

            if (!isGranted)
            {
                throw new UnauthorizedAccessException(
                    $"Required permission '{permissionAttribute.Name}' is not granted."
                );
            }
        }

        await context.Invoke();
    }
}