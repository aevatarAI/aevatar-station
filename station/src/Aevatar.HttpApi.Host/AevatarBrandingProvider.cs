using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace Aevatar;

[Dependency(ReplaceServices = true)]
public class AevatarBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "Aevatar";
}
