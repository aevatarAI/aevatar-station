using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Orleans.Streams;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aevatar.GAgents.Executor;

// Event to inform execution result.
[GenerateSerializer]
public class ExecutionCompletedEvent
{
    [Id(0)] public string ExecutionId { get; set; } = string.Empty;
    [Id(1)] public string Result { get; set; } = string.Empty;
}

public class GAgentExecutor : IGAgentExecutor
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IClusterClient _clusterClient;
    private readonly IGAgentService _gAgentService;

    public GAgentExecutor(IClusterClient clusterClient, IGAgentService gAgentService)
    {
        _gAgentFactory = new GAgentFactory(clusterClient);
        _clusterClient = clusterClient;
        _gAgentService = gAgentService;
    }

    public async Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, EventBase @event,
        Type? expectedResultType = null)
    {
        var resultGAgent = await _gAgentFactory.GetGAgentAsync<IResultGAgent>();
        var publishingGAgent = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>();

        var executionId = Guid.NewGuid().ToString();

        var streamProvider = _clusterClient.GetStreamProvider(AevatarCoreConstants.StreamProvider);
        var resultStream =
            streamProvider.GetStream<ExecutionCompletedEvent>(
                AevatarGAgentExecutorConstants.GAgentExecutorStreamNamespace, executionId);

        var resultTask = new TaskCompletionSource<string>();
        var subscription = await resultStream.SubscribeAsync((result, token) =>
        {
            resultTask.SetResult(result.Result);
            return Task.CompletedTask;
        });

        try
        {
            await resultGAgent.ConfigAsync(new ResultGAgentConfiguration
            {
                ExecutionId = executionId,
                StreamProvider = AevatarCoreConstants.StreamProvider,
                StreamNamespace = AevatarGAgentExecutorConstants.GAgentExecutorStreamNamespace,
                ExpectedResultType = expectedResultType
            });

            // Subscribe ResultGAgent to the target GAgent to receive results
            await gAgent.RegisterAsync(resultGAgent);

            // Also subscribe the target GAgent to PublishingGAgent to receive the event
            await publishingGAgent.RegisterAsync(gAgent);

            // Publish the event through PublishingGAgent 
            // The target GAgent will receive and process it, then publish any result events
            await publishingGAgent.PublishEventAsync(@event);

            return await resultTask.Task.WaitAsync(AevatarGAgentExecutorConstants.GAgentExecutorTimeout);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"ExecuteGAgentEventHandler timeout for execution {executionId}");
        }
        finally
        {
            await subscription.UnsubscribeAsync();
            // Unregister ResultGAgent from the target GAgent
            await gAgent.UnregisterAsync(resultGAgent);
            // Unregister target GAgent from PublishingGAgent
            await publishingGAgent.UnregisterAsync(gAgent);
        }
    }

    public async Task<string> ExecuteGAgentEventHandler(GrainId grainId, EventBase @event,
        Type? expectedResultType = null)
    {
        var targetGAgent = await _gAgentFactory.GetGAgentAsync(grainId);
        return await ExecuteGAgentEventHandler(targetGAgent, @event, expectedResultType);
    }

    public async Task<string> ExecuteGAgentEventHandler(GrainType grainType, EventBase @event,
        Type? expectedResultType = null)
    {
        var grainId = GrainId.Create(grainType, Guid.NewGuid().ToString());
        return await ExecuteGAgentEventHandler(grainId, @event, expectedResultType);
    }

    public async Task<string> ExecuteGAgentEventHandler(IGAgent gAgent, string eventTypeName, string eventJson,
        Type? expectedResultType = null)
    {
        var @event = await DeserializeEventAsync(gAgent.GetGrainId().Type, eventTypeName, eventJson);
        return await ExecuteGAgentEventHandler(gAgent, @event, expectedResultType);
    }

    public async Task<string> ExecuteGAgentEventHandler(GrainId grainId, string eventTypeName, string eventJson,
        Type? expectedResultType = null)
    {
        var @event = await DeserializeEventAsync(grainId.Type, eventTypeName, eventJson);
        return await ExecuteGAgentEventHandler(grainId, @event, expectedResultType);
    }

    public async Task<string> ExecuteGAgentEventHandler(GrainType grainType, string eventTypeName, string eventJson,
        Type? expectedResultType = null)
    {
        var @event = await DeserializeEventAsync(grainType, eventTypeName, eventJson);
        return await ExecuteGAgentEventHandler(grainType, @event, expectedResultType);
    }

    /// <summary>
    /// Deserializes an event from JSON using the event type name
    /// </summary>
    private async Task<EventBase> DeserializeEventAsync(GrainType grainType, string eventTypeName, string eventJson)
    {
        try
        {
            // Get all available GAgent information to find the event type
            var allGAgents = await _gAgentService.GetAllAvailableGAgentInformation();

            if (!allGAgents.TryGetValue(grainType, out var eventTypes))
            {
                throw new InvalidOperationException($"GAgent {grainType} not found");
            }

            // Find the event type by name
            var eventType = eventTypes.FirstOrDefault(t =>
                t.Name.Equals(eventTypeName, StringComparison.OrdinalIgnoreCase) ||
                t.FullName?.Equals(eventTypeName, StringComparison.OrdinalIgnoreCase) == true);

            if (eventType == null)
            {
                throw new InvalidOperationException($"Event type {eventTypeName} not found for GAgent {grainType}");
            }

            // Deserialize the event from JSON
            var @event = JsonSerializer.Deserialize(eventJson, eventType) as EventBase;
            if (@event == null)
            {
                throw new InvalidOperationException($"Failed to deserialize event {eventTypeName} from JSON");
            }

            return @event;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}