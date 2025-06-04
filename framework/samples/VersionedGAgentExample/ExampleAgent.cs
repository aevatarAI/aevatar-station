using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Orleans.Concurrency;

namespace VersionedGAgentExample;

// Example state
public class ExampleState : StateBase
{
    public string Name { get; set; } = string.Empty;
    public int ProcessedCount { get; set; } = 0;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

// Example event
public class ExampleEvent : EventBase
{
    public string Action { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}

// Example response event
public class ExampleResponseEvent : EventBase
{
    public string Result { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
}

// Example configuration
public class ExampleConfiguration : ConfigurationBase
{
    public int MaxProcessingLimit { get; set; } = 100;
    public TimeSpan ProcessingDelay { get; set; } = TimeSpan.FromMilliseconds(100);
}

// State log event for example
public class ExampleStateLogEvent : StateLogEventBase<ExampleStateLogEvent>
{
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Example interface showing how to define a version-stable GAgent interface.
/// This interface can be called like a regular Orleans Grain and supports all Orleans attributes.
/// </summary>
public interface IExampleAgent : IVersionedGAgent<ExampleState, ExampleEvent, ExampleConfiguration>
{
    /// <summary>
    /// Processes data and returns a result.
    /// This method demonstrates Orleans attribute usage.
    /// </summary>
    [AlwaysInterleave] // This method can run concurrently with other operations
    Task<string> ProcessDataAsync(string data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current processing count.
    /// This is a read-only operation that doesn't modify state.
    /// </summary>
    [ReadOnly] // This method only reads state and can be called concurrently
    Task<int> GetProcessingCountAsync();

    /// <summary>
    /// Resets the agent state.
    /// </summary>
    Task ResetAsync();

    /// <summary>
    /// Processes data and waits for confirmation.
    /// Demonstrates event publishing with response.
    /// </summary>
    Task<ExampleResponseEvent> ProcessWithConfirmationAsync(string data, TimeSpan? timeout = null);
}

/// <summary>
/// Example implementation of a version-stable GAgent.
/// This implementation is shielded from GAgentBase changes through the IVersionedGAgent interface.
/// </summary>
[GAgent]
public class ExampleAgent : VersionedGAgentBase<ExampleState, ExampleStateLogEvent, ExampleEvent, ExampleConfiguration>, IExampleAgent
{
    private ExampleConfiguration? _configuration;

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult($"Example versioned GAgent - processed {State.ProcessedCount} items");
    }

    protected override async Task PerformConfigAsync(ExampleConfiguration configuration)
    {
        _configuration = configuration;
        Logger.LogInformation("ExampleAgent configured with MaxProcessingLimit: {Limit}", 
            configuration.MaxProcessingLimit);
        
        await base.PerformConfigAsync(configuration);
    }

    [AlwaysInterleave]
    public async Task<string> ProcessDataAsync(string data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Check processing limit
        if (_configuration != null && State.ProcessedCount >= _configuration.MaxProcessingLimit)
        {
            throw new InvalidOperationException($"Processing limit of {_configuration.MaxProcessingLimit} reached");
        }

        try
        {
            // Simulate processing with configurable delay
            if (_configuration?.ProcessingDelay != null)
            {
                await Task.Delay(_configuration.ProcessingDelay, cancellationToken);
            }

            // Update state
            State.ProcessedCount++;
            State.LastUpdated = DateTime.UtcNow;
            State.Name = $"Processed: {data}";

            // Raise state log event
            RaiseEvent(new ExampleStateLogEvent 
            { 
                Action = "ProcessData", 
                Timestamp = DateTime.UtcNow 
            });

            // Publish event
            await PublishEventAsync(new ExampleEvent 
            { 
                Action = "DataProcessed", 
                Data = data 
            }, cancellationToken);

            var result = $"Successfully processed '{data}' (count: {State.ProcessedCount})";
            Logger.LogInformation("ProcessDataAsync completed: {Result}", result);

            return result;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("ProcessDataAsync was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process data: {Data}", data);
            throw;
        }
    }

    [ReadOnly]
    public Task<int> GetProcessingCountAsync()
    {
        return Task.FromResult(State.ProcessedCount);
    }

    public async Task ResetAsync()
    {
        Logger.LogInformation("Resetting ExampleAgent state");
        
        State.ProcessedCount = 0;
        State.Name = string.Empty;
        State.LastUpdated = DateTime.UtcNow;

        // Raise state log event
        RaiseEvent(new ExampleStateLogEvent 
        { 
            Action = "Reset", 
            Timestamp = DateTime.UtcNow 
        });

        // Publish reset event
        await PublishEventAsync(new ExampleEvent 
        { 
            Action = "AgentReset", 
            Data = "State has been reset" 
        });
    }

    public async Task<ExampleResponseEvent> ProcessWithConfirmationAsync(string data, TimeSpan? timeout = null)
    {
        try
        {
            // Create request event
            var requestEvent = new ExampleEvent 
            { 
                Action = "ProcessWithConfirmation", 
                Data = data 
            };

            // Publish event and wait for response
            var response = await PublishEventWithResponseAsync<ExampleEvent, ExampleResponseEvent>(
                requestEvent, 
                timeout ?? TimeSpan.FromSeconds(30));

            Logger.LogInformation("Received confirmation response: {Result}", response.Result);
            return response;
        }
        catch (TimeoutException)
        {
            Logger.LogWarning("ProcessWithConfirmationAsync timed out for data: {Data}", data);
            return new ExampleResponseEvent 
            { 
                Result = "Timeout occurred", 
                Success = false 
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ProcessWithConfirmationAsync failed for data: {Data}", data);
            return new ExampleResponseEvent 
            { 
                Result = $"Error: {ex.Message}", 
                Success = false 
            };
        }
    }

    // Event handlers
    [EventHandler]
    public async Task HandleExampleEventAsync(ExampleEvent evt)
    {
        Logger.LogInformation("Handling ExampleEvent: Action={Action}, Data={Data}", 
            evt.Action, evt.Data);

        // Simulate some processing
        await Task.Delay(50);

        // If this is a confirmation request, send response
        if (evt.Action == "ProcessWithConfirmation")
        {
            var response = new ExampleResponseEvent 
            { 
                Result = $"Confirmed processing of: {evt.Data}", 
                Success = true 
            };

            await PublishEventAsync(response);
        }
    }

    protected override async Task OnGAgentActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnGAgentActivateAsync(cancellationToken);
        Logger.LogInformation("ExampleAgent {GrainId} activated successfully", this.GetGrainId());
    }

    protected override async Task HandleStateChangedAsync()
    {
        await base.HandleStateChangedAsync();
        Logger.LogDebug("ExampleAgent state changed - ProcessedCount: {Count}, LastUpdated: {LastUpdated}", 
            State.ProcessedCount, State.LastUpdated);
    }
}