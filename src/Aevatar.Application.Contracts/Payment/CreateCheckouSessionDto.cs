namespace Aevatar.Payment;

public class CreateCheckouSessionDto
{
    public string PriceId { get; set; }
    public string Mode { get; set; }
    public long Quantity { get; set; }
}