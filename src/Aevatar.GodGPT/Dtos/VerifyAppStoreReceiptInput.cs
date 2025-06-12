namespace Aevatar.GodGPT.Dtos;

public class VerifyAppStoreReceiptInput
{
    public bool SandboxMode { get; set; } = false;
    public string ProductId { get; set; } = string.Empty;
    public string ReceiptData { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
}