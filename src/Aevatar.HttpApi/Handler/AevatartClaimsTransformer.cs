using Volo.Abp.Security.Claims;

namespace Aevatar.Handler;

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

public class AevatartClaimsTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity != null && principal.Identity.IsAuthenticated)
        {
            var claimsIdentity = (ClaimsIdentity)principal.Identity;

            if (!claimsIdentity.HasClaim(claim => claim.Type == AbpClaimTypes.Role) )
            {
               var role = claimsIdentity.FindFirst(claim => claim.Type == ClaimTypes.Role);
               if ( role != null)
               {
                   claimsIdentity.AddClaim(new Claim(AbpClaimTypes.Role, role.Value));
               }
            }
        }
        return Task.FromResult(principal);
    }
}