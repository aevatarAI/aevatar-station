namespace Aevatar.GAgents.TestBase.Http;

public static class TestHttpClientFactoryProvider
{
    public static HttpMessageHandler? CustomHandler { get; set; }
}

public class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var handler = TestHttpClientFactoryProvider.CustomHandler ?? new HttpClientHandler();
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.twitter.com/2")
        };
        return client;
    }
}

