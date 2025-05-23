using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Volo.Abp.OpenIddict.Controllers;

namespace Aevatar;

public class ApplicationDescription: IApplicationModelConvention
{
    public ApplicationDescription()
    {
    }

    public void Apply(ApplicationModel application)
    {
        application.Controllers.RemoveAll(x=>x.ControllerType == typeof(TokenController));
    }
}