namespace Aevatar.AuthServer.Grants.Providers;

public static class AppleConstants
{
    private const string AuthorityUrl = "https://appleid.apple.com";
    public const string TokenEndpoint = AuthorityUrl + "/auth/token";
    public const string JwksEndpoint = AuthorityUrl + "/auth/keys";
    public const string ValidIssuer = AuthorityUrl;
    
    public static class Claims
    {
        public const string Kid = "kid";
    }
} 