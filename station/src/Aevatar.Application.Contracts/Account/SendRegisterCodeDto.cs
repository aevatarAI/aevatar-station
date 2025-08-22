using System.ComponentModel.DataAnnotations;

using Aevatar.Common;
using Volo.Abp.Identity;
using Volo.Abp.Validation;

namespace Aevatar.Account;

public class SendRegisterCodeDto
{
    [Required]
    [EmailAddress]
    [DynamicStringLength(typeof(IdentityUserConsts), nameof(IdentityUserConsts.MaxEmailLength))]
    public string Email { get; set; }
    
    [Required]
    public string AppName { get; set; }

    /// <summary>
    /// Platform type (Web, iOS, Android)
    /// </summary>
    public PlatformType Platform { get; set; } = PlatformType.Web;

    /// <summary>
    /// Recaptcha verification token (required when rate limit exceeded on Web platform)
    /// </summary>
    public string? RecaptchaToken { get; set; }
}