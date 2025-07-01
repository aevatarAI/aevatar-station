using Aevatar.Application.Grains.Invitation;

namespace Aevatar.GodGPT.Dtos;

public class GetInvitationInfoResponse
{
    public string InviteCode { get; set; }
    public int TotalInvites { get; set; }
    public int ValidInvites { get; set; }
    public int TotalCreditsEarned { get; set; }
    public List<RewardTierDto> RewardTiers { get; set; }
    public bool IsBound { get; set; }
}