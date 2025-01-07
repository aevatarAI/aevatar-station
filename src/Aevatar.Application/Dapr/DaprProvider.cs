using System;
using System.Threading.Tasks;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Dapr;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Dapr;

public class DaprProvider : IDaprProvider, ISingletonDependency
{
    private readonly ILogger<DaprProvider> _logger;
    private readonly DaprClient _daprClient;
    private readonly AbpDaprOptions _daprOptions;

    public DaprProvider(ILogger<DaprProvider> logger, DaprClient daprClient, IOptions<AbpDaprOptions> options)
    {
        _logger = logger;
        _daprClient = daprClient;
        _daprOptions = options.Value;
    }

    public async Task PublishEventAsync<T>(string pubsubName, string topicName, T message)
    {
        try
        {
            await _daprClient.PublishEventAsync(pubsubName, topicName, message);
            _logger.LogInformation("Dapr event published. pubsubName: {pubsub}, topicName: {topicName}, message: {message}",
                pubsubName, topicName, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event. pubsubName: {pubsub}, topicName: {topicName},, message: {message}",
                pubsubName, topicName,message);
            ;
        }
    }
}