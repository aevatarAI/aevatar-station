using System.Net;

namespace Aevatar.Cli.Http;

public class CliHttpClientHandler : HttpClientHandler
{
    public CliHttpClientHandler()
    {
        Proxy = WebRequest.GetSystemWebProxy();
        DefaultProxyCredentials = CredentialCache.DefaultCredentials;
    }
}