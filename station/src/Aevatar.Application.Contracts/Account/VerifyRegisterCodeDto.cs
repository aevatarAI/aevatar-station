using System.ComponentModel.DataAnnotations;

namespace Aevatar.Account;

public class VerifyRegisterCodeDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Code { get; set; }
} 