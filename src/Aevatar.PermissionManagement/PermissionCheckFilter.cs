using System.Reflection;
using System.Security.Claims;
using Orleans;
using Orleans.Runtime;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Security.Claims;

namespace Aevatar.PermissionManagement;

public class PermissionCheckFilter : IIncomingGrainCallFilter
{
    private readonly IPermissionChecker _permissionChecker;
    private readonly IPermissionManager _permissionManager;

    public PermissionCheckFilter(IPermissionChecker permissionChecker, IPermissionManager permissionManager)
    {
        _permissionChecker = permissionChecker;
        _permissionManager = permissionManager;
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var method = context.ImplementationMethod;
        var permissionAttribute = method.GetCustomAttribute<PermissionAttribute>();

        if (permissionAttribute != null)
        {
            if (RequestContext.Get("CurrentUser") is not UserContext currentUser) return;
            //var isGranted = await CheckPermissionViaPermissionCheckerAsync(currentUser, permissionAttribute);
            var isGranted = await CheckPermissionViaPermissionManagerAsync(currentUser, permissionAttribute);
            if (!isGranted)
            {
                throw new UnauthorizedAccessException(
                    $"Required permission '{permissionAttribute.Name}' is not granted."
                );
            }
        }

        await context.Invoke();
    }

    private async Task<bool> CheckPermissionViaPermissionManagerAsync(UserContext currentUser,
        PermissionAttribute permissionAttribute)
    {
        var permission =
            await _permissionManager.GetAsync(permissionAttribute.Name, "User", currentUser.UserId.ToString());
        return permission.IsGranted;
    }

    private async Task<bool> CheckPermissionViaPermissionCheckerAsync(UserContext currentUser,
        PermissionAttribute permissionAttribute)
    {
        var claimsPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity([
                new Claim(AbpClaimTypes.UserId, currentUser.UserId.ToString()),
                new Claim(AbpClaimTypes.Role, currentUser.Role)
            ], "Bearer"));

        if (claimsPrincipal.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var isGranted = await _permissionChecker.IsGrantedAsync(
            claimsPrincipal,
            permissionAttribute.Name
        );

        return isGranted;
    }
}