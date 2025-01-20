using System.Linq;
using Aevatar.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Aevatar.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class AevatarController : AbpControllerBase
{
    protected AevatarController()
    {
        LocalizationResource = typeof(AevatarResource);
    }
    
    protected string ClientId
    {
        get { return CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value; }
    }
}
