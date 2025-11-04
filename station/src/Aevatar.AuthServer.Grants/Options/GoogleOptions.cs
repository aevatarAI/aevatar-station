namespace Aevatar.AuthServer.Grants.Options;

public class GoogleOptions
{
    public string ClientId { get; set; }
    public string IOSClientId { get; set; }
    public string AndroidClientId { get; set; }
    public Dictionary<string, AppClientConfig> AppConfigs { get; set; } = new ();
}

public class AppClientConfig
{
    public string ClientId { get; set; }
    public string IOSClientId { get; set; }
    public string AndroidClientId { get; set; }
}