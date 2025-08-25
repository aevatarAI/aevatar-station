using Volo.Abp.Account.Localization;
using Volo.Abp.Emailing.Templates;
using Volo.Abp.TextTemplating;

namespace Aevatar.Account.Templates;

public class AevatarAccountEmailTemplateDefinitionProvider : TemplateDefinitionProvider
{
    public override void Define(ITemplateDefinitionContext context)
    {
        // English template (default)
        context.Add(
            new TemplateDefinition(
                AevatarAccountEmailTemplates.RegisterCode,
                layout: StandardEmailTemplates.Layout
            ).WithVirtualFilePath("/Aevatar/Account/Templates/RegisterCode.tpl", true)
        );
        
        // Simplified Chinese template
        context.Add(
            new TemplateDefinition(
                $"{AevatarAccountEmailTemplates.RegisterCode}_zh-cn",
                layout: StandardEmailTemplates.Layout
            ).WithVirtualFilePath("/Aevatar/Account/Templates/RegisterCode_zh-cn.tpl", true)
        );
        
        // Traditional Chinese template
        context.Add(
            new TemplateDefinition(
                $"{AevatarAccountEmailTemplates.RegisterCode}_zh-tw",
                layout: StandardEmailTemplates.Layout
            ).WithVirtualFilePath("/Aevatar/Account/Templates/RegisterCode_zh-tw.tpl", true)
        );
        
        // Spanish template
        context.Add(
            new TemplateDefinition(
                $"{AevatarAccountEmailTemplates.RegisterCode}_es",
                layout: StandardEmailTemplates.Layout
            ).WithVirtualFilePath("/Aevatar/Account/Templates/RegisterCode_es.tpl", true)
        );
    }
}