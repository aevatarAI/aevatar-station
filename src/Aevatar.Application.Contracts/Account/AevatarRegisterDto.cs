using System.ComponentModel.DataAnnotations;
using Volo.Abp.Account;

namespace Aevatar.Account;

public class AevatarRegisterDto : RegisterDto
{
    [Required]
    public string Code { get; set; }
}