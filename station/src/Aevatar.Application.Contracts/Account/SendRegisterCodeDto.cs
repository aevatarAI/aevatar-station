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
    /// Platform type for the client application
    /// </summary>
    [Required]
    public PlatformType Platform { get; set; } = PlatformType.Web;

    /// <summary>
    /// reCAPTCHA verification token (required for Web platform when rate limit exceeded)
    /// </summary>
    public string? ReCAPTCHAToken { get; set; }

    /// <summary>
    /// Firebase App Check token (used for mobile platform verification)
    /// </summary>
    public string? AcToken { get; set; }
}