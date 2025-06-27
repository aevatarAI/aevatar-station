using System;
using System.Security.Principal;
using System.Threading.Tasks;
using Aevatar.Plugins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Authorization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Security.Claims;
using Volo.Abp.Threading;

namespace Aevatar;

public class AevatarAuthorizationPolicyProvider : AbpAuthorizationPolicyProvider
{
    private readonly PluginGAgentLoadOptions _pluginGAgentLoadOptions;

    public AevatarAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options,
        IPermissionDefinitionManager permissionDefinitionManager,
        IOptions<PluginGAgentLoadOptions> pluginGAgentLoadOptions)
        : base(options, permissionDefinitionManager)
    {
        _pluginGAgentLoadOptions = pluginGAgentLoadOptions.Value;
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.IsNullOrWhiteSpace())
        {
            return null;
        }

        var builder = new AuthorizationPolicyBuilder();
        builder.RequireAssertion(context =>
        {
            var user = context.User;
            var httpContext = context.Resource as Microsoft.AspNetCore.Http.HttpContext;

            if (user == null || httpContext == null)
            {
                return false;
            }

            var permissionChecker = httpContext.RequestServices.GetRequiredService<IPermissionChecker>();
            var hasPermission = AsyncHelper.RunSync(async () => await permissionChecker.IsGrantedAsync(policyName));
            if (hasPermission)
            {
                return true;
            }

            var tenantId = _pluginGAgentLoadOptions.TenantId;
            var hasApiRole = user.HasClaim(AbpClaimTypes.Role,$"{tenantId.ToString()}_ApiKey");
            
            return hasApiRole;
        });

        return builder.Build();
    }
}