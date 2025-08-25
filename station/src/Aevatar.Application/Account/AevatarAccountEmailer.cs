using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Aevatar.Account.Templates;
using Aevatar.Application.Contracts.Services;
using Aevatar.Domain.Shared;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Account.Emailing;
using Volo.Abp.Account.Emailing.Templates;
using Volo.Abp.Account.Localization;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Emailing;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TextTemplating;
using Volo.Abp.UI.Navigation.Urls;

namespace Aevatar.Account;

public interface IAevatarAccountEmailer
{
    Task SendRegisterCodeAsync(string email, string code, GodGPTChatLanguage godGptChatLanguage);
    Task SendPasswordResetLinkAsync(IdentityUser user, string resetToken);
}

public class AevatarAccountEmailer : IAevatarAccountEmailer, ITransientDependency
{
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IEmailSender _emailSender;
    private readonly IStringLocalizer<AccountResource> _stringLocalizer;
    private readonly ILocalizationService _localizationService;
    private readonly AccountOptions _accountOptions;
    private readonly IDistributedCache<string,string> _lastEmailCache;
    private readonly DistributedCacheEntryOptions _defaultCacheOptions;

    public AevatarAccountEmailer(IEmailSender emailSender, ITemplateRenderer templateRenderer,
        IStringLocalizer<AccountResource> stringLocalizer, ILocalizationService localizationService,
        IOptionsSnapshot<AccountOptions> accountOptions, IDistributedCache<string,string> lastEmailCache)
    {
        _emailSender = emailSender;
        _templateRenderer = templateRenderer;
        _stringLocalizer = stringLocalizer;
        _localizationService = localizationService;
        _lastEmailCache = lastEmailCache;
        _accountOptions = accountOptions.Value;

        _defaultCacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_accountOptions.MailSendingInterval)
        };
    }

    public async Task SendRegisterCodeAsync(string email, string code, GodGPTChatLanguage language = GodGPTChatLanguage.English)
    {
        var templateName = GetTemplateNameByLanguage(language);
        var emailContent = await _templateRenderer.RenderAsync(
            templateName,
            new { code = code }
        );

        await CheckSendEmailAsync(email);
        
        var subject = GetEmailSubjectByLanguage(language);
        await _emailSender.SendAsync(
            email,
            subject,
            emailContent
        );
    }

    public async Task SendPasswordResetLinkAsync(IdentityUser user, string resetToken)
    {
        var url = _accountOptions.ResetPasswordUrl;
        var link = $"{url}?userId={user.Id}&resetToken={UrlEncoder.Default.Encode(resetToken)}";

        var emailContent = await _templateRenderer.RenderAsync(
            AccountEmailTemplates.PasswordResetLink,
            new { link = link }
        );
        
        await CheckSendEmailAsync(user.Email);
        
        await _emailSender.SendAsync(
            user.Email,
            "PasswordReset",
            emailContent
        );
    }

    private async Task CheckSendEmailAsync(string email)
    {
        var key = $"LastEmail_{email.ToLower()}";
        var lastSend = await _lastEmailCache.GetAsync(key);
        if (!lastSend.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException("Email sent too frequently. Please try again later.");
        }
        
        await _lastEmailCache.SetAsync(key, email, _defaultCacheOptions);
    }

    private string GetTemplateNameByLanguage(GodGPTChatLanguage language)
    {
        return language switch
        {
            GodGPTChatLanguage.English => AevatarAccountEmailTemplates.RegisterCode,
            GodGPTChatLanguage.CN => $"{AevatarAccountEmailTemplates.RegisterCode}_zh-cn",
            GodGPTChatLanguage.TraditionalChinese => $"{AevatarAccountEmailTemplates.RegisterCode}_zh-tw",
            GodGPTChatLanguage.Spanish => $"{AevatarAccountEmailTemplates.RegisterCode}_es",
            _ => AevatarAccountEmailTemplates.RegisterCode // Default to English
        };
    }

    private string GetEmailSubjectByLanguage(GodGPTChatLanguage language)
    {
        return _localizationService.GetLocalizedMessage("RegistrationSubject", language, "emails");
    }
}