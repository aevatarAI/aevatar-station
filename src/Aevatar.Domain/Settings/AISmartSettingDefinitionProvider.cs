using Volo.Abp.Settings;

namespace Aevatar.Settings;

public class AevatarSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        var smtpPassword = context.GetOrNull("Abp.Mailing.Smtp.Password");
        if (smtpPassword != null)
        {
            smtpPassword.IsEncrypted = false;
        }
    }
}
