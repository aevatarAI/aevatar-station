using System.ComponentModel.DataAnnotations;

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
    /// Recaptcha verification token (required when rate limit exceeded for all platforms)
    /// </summary>
    public string? RecaptchaToken { get; set; }
}