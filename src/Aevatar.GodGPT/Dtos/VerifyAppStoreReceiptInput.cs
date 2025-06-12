namespace Aevatar.GodGPT.Dtos;

public class VerifyAppStoreReceiptInput
{
    public bool SandboxMode { get; set; } = false;
    public string ProductId { get; set; }
    public string ReceiptData { get; set; }
}