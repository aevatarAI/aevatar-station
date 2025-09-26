using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Aevatar.Cli.Http;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> GetHttpResponseMessageWithRetryAsync<T>
    (
        this HttpClient httpClient,
        string url,
        CancellationToken? cancellationToken = null,
        ILogger<T>? logger = null,
        IEnumerable<TimeSpan>? sleepDurations = null
    )
    {
        sleepDurations ??=
        [
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(7)
        ];

        if (cancellationToken == null)
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(httpClient.Timeout);
            cancellationToken = cancellationTokenSource.Token;
        }

        return await HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => !msg.IsSuccessStatusCode)
            .WaitAndRetryAsync(sleepDurations,
                (responseMessage, timeSpan, retryCount, context) =>
                {
                    if (responseMessage.Exception != null)
                    {
                        string httpErrorCode = responseMessage.Result == null
                            ? httpErrorCode = string.Empty
                            : "HTTP-" + (int)responseMessage.Result.StatusCode + ", ";

                        logger?.LogWarning(
                            $"{retryCount}. HTTP request attempt failed to {url} with an error: {httpErrorCode}{responseMessage.Exception.Message}. " +
                            $"Waiting {timeSpan.TotalSeconds} secs for the next try...");
                    }
                    else if (responseMessage.Result != null)
                    {
                        logger?.LogWarning(
                            $"{retryCount}. HTTP request attempt failed to {url} with an error: {(int)responseMessage.Result.StatusCode}-{responseMessage.Result.ReasonPhrase}. " +
                            $"Waiting {timeSpan.TotalSeconds} secs for the next try...");
                    }
                })
            .ExecuteAsync(async () => await httpClient.GetAsync(url, cancellationToken.Value));
    }
}