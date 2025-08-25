namespace Aevatar.GodGPT.Dtos;

public class SetUserProfileInput
{
    public string Gender { get; set; }
    public DateTime BirthDate { get; set; }
    public string BirthPlace { get; set; }
    public string FullName { get; set; } = string.Empty;
}