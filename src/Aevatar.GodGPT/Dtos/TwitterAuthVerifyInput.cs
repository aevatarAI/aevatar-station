namespace Aevatar.GodGPT.Dtos;

public class TwitterAuthVerifyInput
{
    public string Code { get; set; }
    public string State { get; set; }
    public string Platform { get; set; }
}