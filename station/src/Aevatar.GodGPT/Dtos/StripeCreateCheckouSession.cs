namespace Aevatar.GodGPT.Dtos;

public class CreateCheckoutSessionInput
{
    public string PriceId { get; set; }
    public string Mode { get; set; }
    public long Quantity { get; set; }
    public string UiMode { get; set; }
    public string CancelUrl { get; set; } = string.Empty;
}