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
}
