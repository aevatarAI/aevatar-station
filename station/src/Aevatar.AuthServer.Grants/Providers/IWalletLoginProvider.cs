namespace Aevatar.AuthServer.Grants.Providers;

public interface IWalletLoginProvider
{
    List<string> CheckParams(string publicKeyVal, string signatureVal, string chainId, 
        string timestamp);
    string GetErrorMessage(List<string> errors);
    Task<string> VerifySignatureAndParseWalletAddressAsync(string publicKeyVal, string signatureVal, string timestampVal,
        string caHash,  string chainId);
}