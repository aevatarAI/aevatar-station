using Aevatar.Application.Grains.Common.Constants;

namespace Aevatar.GodGPT.Dtos;

public class UpdateUserSubscriptionsInput
{
    public Guid UserId { get; set; }
    public PlanType PlanType { get; set; }
    public bool IsUltimate { get; set; }
}