using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Aevatar.Account.Templates;
using Aevatar.Application.Constants;
using Aevatar.Application.Contracts.Services;
using Aevatar.Domain.Shared;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
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
    Task SendRegisterCodeAsync(string email, string code,GodGPTChatLanguage language = GodGPTChatLanguage.English);
    Task SendPasswordResetLinkAsync(IdentityUser user, string inputEmail, string resetToken,GodGPTChatLanguage language = GodGPTChatLanguage.English);
}

public class AevatarAccountEmailer : IAevatarAccountEmailer, ITransientDependency
{
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IEmailSender _emailSender;
    private readonly IStringLocalizer<AccountResource> _stringLocalizer;
    private readonly AccountOptions _accountOptions;
    private readonly IDistributedCache<string,string> _lastEmailCache;
    private readonly DistributedCacheEntryOptions _defaultCacheOptions;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<AevatarAccountEmailer> _logger;

    public AevatarAccountEmailer(IEmailSender emailSender, ITemplateRenderer templateRenderer,
        IStringLocalizer<AccountResource> stringLocalizer, IOptionsSnapshot<AccountOptions> accountOptions,
        IDistributedCache<string,string> lastEmailCache,ILocalizationService localizationService,ILogger<AevatarAccountEmailer> logger)
    {
        _emailSender = emailSender;
        _templateRenderer = templateRenderer;
        _stringLocalizer = stringLocalizer;
        _lastEmailCache = lastEmailCache;
        _accountOptions = accountOptions.Value;
        _localizationService = localizationService;
        _logger = logger;

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

        await CheckSendEmailAsync(email, language);
        
        var subject = GetEmailSubjectByLanguage(language);
        await _emailSender.SendAsync(
            email,
            subject,
            emailContent
        );
    }

    public async Task SendPasswordResetLinkAsync(IdentityUser user, string inputEmail, string resetToken, GodGPTChatLanguage language = GodGPTChatLanguage.English)
    {
        var url = _accountOptions.ResetPasswordUrl;
        var context = RequestContext.Get("IsCN");
        var isCN = RequestContext.Get("IsCN") is bool cnValue and true;        
        if (isCN)
        {
            url = _accountOptions.CNResetPasswordUrl;
            if (url.IsNullOrEmpty())
            {
                _logger.LogDebug("[AevatarAccountEmailer][SendPasswordResetLinkAsync] CNResetPasswordUrl not found");
                url = _accountOptions.ResetPasswordUrl;
            }
        }
        _logger.LogDebug("[AevatarAccountEmailer][SendPasswordResetLinkAsync] reset url:{url},requestContext:{context} isCN:{isCN}", url, context, isCN);

        var link = $"{url}?userId={user.Id}&email={UrlEncoder.Default.Encode(inputEmail)}&resetToken={UrlEncoder.Default.Encode(resetToken)}";

        var templateName = GetPasswordResetTemplateNameByLanguage(language);
        var emailContent = await _templateRenderer.RenderAsync(
            templateName,
            new { link = link }
        );
        
        await CheckSendEmailAsync(user.Email, language);
        
        var subject = GetPasswordResetSubjectByLanguage(language);
        await _emailSender.SendAsync(
            user.Email,
            subject,
            emailContent
        );
    }

    private async Task CheckSendEmailAsync(string email,GodGPTChatLanguage language = GodGPTChatLanguage.English)
    {

        var key = $"LastEmail_{email.ToLower()}";
        var lastSend = await _lastEmailCache.GetAsync(key);
        if (!lastSend.IsNullOrWhiteSpace())
        {
            var localizedMessage = _localizationService.GetLocalizedException(GodGPTExceptionMessageKeys.EmailFrequently, language);
            throw new UserFriendlyException(localizedMessage);
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

    private string GetPasswordResetTemplateNameByLanguage(GodGPTChatLanguage language)
    {
        return language switch
        {
            GodGPTChatLanguage.English => AevatarAccountEmailTemplates.PasswordResetLink,
            GodGPTChatLanguage.CN => $"{AevatarAccountEmailTemplates.PasswordResetLink}_zh-cn",
            GodGPTChatLanguage.TraditionalChinese => $"{AevatarAccountEmailTemplates.PasswordResetLink}_zh-tw",
            GodGPTChatLanguage.Spanish => $"{AevatarAccountEmailTemplates.PasswordResetLink}_es",
            _ => AevatarAccountEmailTemplates.PasswordResetLink // Default to English
        };
    }

    private string GetPasswordResetSubjectByLanguage(GodGPTChatLanguage language)
    {
        return _localizationService.GetLocalizedMessage("PasswordResetSubject", language, "emails");
    }
}