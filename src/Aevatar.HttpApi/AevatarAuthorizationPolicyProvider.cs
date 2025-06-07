using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Security.Claims;

namespace Aevatar;

public class AevatarAuthorizationPolicyProvider : AbpAuthorizationPolicyProvider
{
    public AevatarAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options,
        IPermissionDefinitionManager permissionDefinitionManager)
        : base(options, permissionDefinitionManager) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var builder = new AuthorizationPolicyBuilder();
        builder.RequireAssertion(context =>
        {
            var user = context.User;
            var httpContext = context.Resource as Microsoft.AspNetCore.Http.HttpContext;

            if (user == null || httpContext == null)
                return false;

            var permissionChecker = httpContext.RequestServices.GetRequiredService<IPermissionChecker>();
            var hasPermission = permissionChecker.IsGrantedAsync(policyName).Result;

            var hasApiRole = user.HasClaim(AbpClaimTypes.Role,"xxxxxx-ApiKey");
                
            return hasPermission || hasApiRole;
        });

        return builder.Build();
    }
}