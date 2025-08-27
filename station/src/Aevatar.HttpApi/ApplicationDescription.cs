using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Volo.Abp.Account;
using Volo.Abp.Identity;

namespace Aevatar;

public class ApplicationDescription: IApplicationModelConvention
{
    public ApplicationDescription()
    {
    }

    public void Apply(ApplicationModel application)
    {
        application.Controllers.RemoveAll(x=>x.ControllerType == typeof(AccountController));
    }
}