using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace Aevatar.AuthServer;

[Dependency(ReplaceServices = true)]
public class AevatarAuthServerBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "AevatarAuthServer";
}
