using System.Reflection;
using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Security.Claims;

namespace Aevatar.PermissionManagement;

public class PermissionCheckFilter : IIncomingGrainCallFilter
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PermissionCheckFilter> _logger;
    private IPermissionChecker _permissionChecker;

    public PermissionCheckFilter(IServiceProvider serviceProvider, ILogger<PermissionCheckFilter> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var method = context.ImplementationMethod;
        var declaringType = method.DeclaringType!;
        var classPermissions = declaringType.GetCustomAttributes<PermissionAttribute>(inherit: true);
        var methodPermissions = method.GetCustomAttributes<PermissionAttribute>(inherit: true);

        var allPermissionNames = classPermissions.Concat(methodPermissions)
            .Select(attr => attr.Name)
            .Distinct()
            .ToList();

        if (allPermissionNames.Count == 0)
        {
            await context.Invoke();
            return;
        }

        if (RequestContext.Get("CurrentUser") is not UserContext currentUser)
        {
            throw new AuthenticationException("Request requires authentication");
        }

        using var scope = _serviceProvider.CreateScope();
        var checker = scope.ServiceProvider.GetRequiredService<IPermissionChecker>();

        var principal = BuildClaimsPrincipal(currentUser);

        _logger.LogInformation("Start permission checking for method {MethodName}", method.Name);

        foreach (var permissionName in allPermissionNames)
        {
            if (!await checker.IsGrantedAsync(principal, permissionName))
            {
                throw new AuthenticationException(
                    $"Missing required permission: {permissionName}, " +
                    $"userId: {currentUser.UserId.ToString()}, " +
                    $"userName: {currentUser.UserName}, " +
                    $"clientId: {currentUser.ClientId}");
            }
        }

        _logger.LogInformation("End permission checking of method {MethodName}", method.Name);

        await context.Invoke();
    }

    private static ClaimsPrincipal BuildClaimsPrincipal(UserContext user)
    {
        var claims = new List<Claim>
        {
            new(AbpClaimTypes.UserId, user.UserId.ToString()),
            new(AbpClaimTypes.UserName, user.UserName),
            new(AbpClaimTypes.Email, user.Email),
            new(AbpClaimTypes.ClientId, user.ClientId)
        };
        claims.AddRange(user.Roles.Select(role => new Claim(AbpClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
    }
}