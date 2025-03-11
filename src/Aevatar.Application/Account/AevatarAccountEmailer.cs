using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Aevatar.Account.Templates;
using Microsoft.Extensions.Localization;
using Volo.Abp.Account.Emailing;
using Volo.Abp.Account.Emailing.Templates;
using Volo.Abp.Account.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Emailing;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TextTemplating;
using Volo.Abp.UI.Navigation.Urls;

namespace Aevatar.Account;

public interface IAevatarAccountEmailer
{
    Task SendRegisterCodeAsync(string email, string code);
    Task SendPasswordResetLinkAsync(IdentityUser user, string resetToken);
}

public class AevatarAccountEmailer:IAevatarAccountEmailer, ITransientDependency
{
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IEmailSender _emailSender;
    private readonly IStringLocalizer<AccountResource> _stringLocalizer;
    
    public AevatarAccountEmailer(IEmailSender emailSender, ITemplateRenderer templateRenderer,
        IStringLocalizer<AccountResource> stringLocalizer)
    {
        _emailSender = emailSender;
        _templateRenderer = templateRenderer;
        _stringLocalizer = stringLocalizer;
    }

    public async Task SendRegisterCodeAsync(string email, string code)
    {
        var emailContent = await _templateRenderer.RenderAsync(
            AevatarAccountEmailTemplates.RegisterCode,
            new { code = code }
        );

        await _emailSender.SendAsync(
            email,
            "Registration",
            emailContent
        );
    }

    public async Task SendPasswordResetLinkAsync(IdentityUser user, string resetToken)
    {
        var url = "";

        var link = $"{url}?userId={user.Id}&resetToken={UrlEncoder.Default.Encode(resetToken)}";

        var emailContent = await _templateRenderer.RenderAsync(
            AccountEmailTemplates.PasswordResetLink,
            new { link = link }
        );

        await _emailSender.SendAsync(
            user.Email,
            _stringLocalizer["PasswordReset"],
            emailContent
        );
    }
}