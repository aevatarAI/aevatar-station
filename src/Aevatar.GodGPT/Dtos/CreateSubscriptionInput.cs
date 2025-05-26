namespace Aevatar.GodGPT.Dtos;

public class CreateSubscriptionInput
{
    public string PriceId { get; set; }
    public int Quantity { get; set; } = 1;
    public string PaymentMethodId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; } = new Dictionary<string, string>();
    public int? TrialPeriodDays { get; set; }

    public string? DevicePlatform { get; set; } = "android";//android/ios
}