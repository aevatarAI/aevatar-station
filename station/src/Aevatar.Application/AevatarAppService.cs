using System;
using System.Collections.Generic;
using System.Text;
using Aevatar.Localization;
using Volo.Abp.Application.Services;

namespace Aevatar;

/* Inherit your application services from this class.
 */
public abstract class AevatarAppService : ApplicationService
{
    protected AevatarAppService()
    {
        LocalizationResource = typeof(AevatarResource);
    }
}
