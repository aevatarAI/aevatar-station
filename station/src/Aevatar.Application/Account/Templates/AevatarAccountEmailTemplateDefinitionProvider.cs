using Volo.Abp.Account.Localization;
using Volo.Abp.Emailing.Templates;
using Volo.Abp.TextTemplating;

namespace Aevatar.Account.Templates;

public class AevatarAccountEmailTemplateDefinitionProvider : TemplateDefinitionProvider
{
    public override void Define(ITemplateDefinitionContext context)
    {
        context.Add(
            new TemplateDefinition(
                AevatarAccountEmailTemplates.RegisterCode,
                layout: StandardEmailTemplates.Layout
            ).WithVirtualFilePath("/Aevatar/Account/Templates/RegisterCode.tpl", true)
        );
    }
}