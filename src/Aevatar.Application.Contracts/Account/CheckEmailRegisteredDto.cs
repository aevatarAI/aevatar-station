using System.ComponentModel.DataAnnotations;

namespace Aevatar.Account;

public class CheckEmailRegisteredDto
{
    [Required]
    public string EmailAddress { get; set; }
}