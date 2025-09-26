using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace Aevatar.Cli.Http;

public class HttpClientFactory : ISingletonDependency
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    private readonly IHttpClientFactory _clientFactory;
    private readonly ICancellationTokenProvider _cancellationTokenProvider;

    public HttpClientFactory(IHttpClientFactory clientFactory,
        ICancellationTokenProvider cancellationTokenProvider)
    {
        _clientFactory = clientFactory;
        _cancellationTokenProvider = cancellationTokenProvider;
    }

    public HttpClient CreateClient(bool needsAuthentication = true, TimeSpan? timeout = null, string clientName = null)
    {
        var httpClient = _clientFactory.CreateClient(clientName ?? AevatarCliConstants.HttpClientName);
        httpClient.Timeout = timeout ?? DefaultTimeout;

        // Authentication is handled in BaseHttpCommand.CreateAuthenticatedClientAsync()

        return httpClient;
    }

    public CancellationToken GetCancellationToken(TimeSpan? timeout = null)
    {
        if (timeout == null)
        {
            if (_cancellationTokenProvider == null)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(DefaultTimeout);
                return cancellationTokenSource.Token;
            }
            else
            {
                return _cancellationTokenProvider.Token;
            }
        }
        else
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(Convert.ToInt32(timeout.Value.TotalMilliseconds));
            return cancellationTokenSource.Token;
        }
    }
}