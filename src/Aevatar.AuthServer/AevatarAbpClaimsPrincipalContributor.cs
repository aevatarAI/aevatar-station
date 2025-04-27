using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Security.Claims;

namespace Aevatar;

public class AevatarAbpClaimsPrincipalContributor : IAbpClaimsPrincipalContributor, ITransientDependency
{
    public Task ContributeAsync(AbpClaimsPrincipalContributorContext context)
    {
        var identity = context.ClaimsPrincipal.Identities.FirstOrDefault();
        if (identity != null)
        {
            var options = context.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;
            var claim = identity.FindFirst(options.ClaimsIdentity.SecurityStampClaimType);
            if (claim != null)
            {
                identity.AddIfNotContains(new Claim(AevatarConsts.SecurityStampClaimType, claim.Value));
            }

        }

        return Task.CompletedTask;

    }
}
