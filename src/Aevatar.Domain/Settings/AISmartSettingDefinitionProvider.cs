using Volo.Abp.Settings;

namespace Aevatar.Settings;

public class AevatarSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(AevatarSettings.MySetting1));
    }
}
