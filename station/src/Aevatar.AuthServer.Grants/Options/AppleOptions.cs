namespace Aevatar.AuthServer.Grants.Options;

public class AppleOptions
{
    public Dictionary<string, AppleAppOptions> APPs { get; set; } = new ();
}

public class AppleAppOptions
{
    public string NativeClientId { get; set; }
    public string WebClientId { get; set; }
    public string KeyId { get; set; }
    public string TeamId { get; set; }
    public string RedirectUri { get; set; }
    public string MobileRedirectUri { get; set; }
    public string Pk { get; set; }
}