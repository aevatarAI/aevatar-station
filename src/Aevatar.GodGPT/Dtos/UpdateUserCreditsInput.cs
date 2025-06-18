namespace Aevatar.GodGPT.Dtos;

public class UpdateUserCreditsInput
{
    public Guid UserId { get; set; }
    public int Credits { get; set; }
}