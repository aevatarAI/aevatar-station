using Google.Apis.Auth;

namespace Aevatar.AuthServer.Grants.Providers;

public interface IGoogleProvider
{
    Task<GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken, string clientId);
    Task<string> GetClientIdAsync(string source);
} 