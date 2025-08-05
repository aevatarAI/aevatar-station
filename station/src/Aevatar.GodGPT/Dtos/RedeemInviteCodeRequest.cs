using System.ComponentModel.DataAnnotations;

namespace Aevatar.GodGPT.Dtos;

public class RedeemInviteCodeRequest
{
    [Required]
    [StringLength(20)]
    public string InviteCode { get; set; }
    public Guid UserId{ get; set; }

}

public class RedeemInviteCodeResponse
{
    public bool IsValid { get; set; }
}