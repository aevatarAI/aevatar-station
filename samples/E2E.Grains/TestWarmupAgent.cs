using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace E2E.Grains;

/// <summary>
/// Test agent implementation for warmup validation
/// Tracks activation time and access patterns
/// </summary>
public class TestWarmupAgent : Grain, ITestWarmupAgent
{
    private readonly ILogger<TestWarmupAgent> _logger;
    private DateTime _activationTime;
    private int _accessCount;
    private bool _isWarmedUp;

    public TestWarmupAgent(ILogger<TestWarmupAgent> logger)
    {
        _logger = logger;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _activationTime = DateTime.UtcNow;
        _accessCount = 0;
        _isWarmedUp = false;
        
        _logger.LogInformation("TestWarmupAgent {AgentId} activated at {ActivationTime}", 
            this.GetPrimaryKey(), _activationTime);
        
        return base.OnActivateAsync(cancellationToken);
    }

    public Task<string> PingAsync()
    {
        _accessCount++;
        var response = $"Pong from agent {this.GetPrimaryKey()} at {DateTime.UtcNow:HH:mm:ss.fff}";
        
        _logger.LogDebug("Ping received by agent {AgentId}, access count: {AccessCount}", 
            this.GetPrimaryKey(), _accessCount);
        
        return Task.FromResult(response);
    }

    public Task<DateTime> GetActivationTimeAsync()
    {
        _accessCount++;
        return Task.FromResult(_activationTime);
    }

    public Task<int> ComputeAsync(int input)
    {
        _accessCount++;
        
        // Simulate some computation work
        var result = input * 2 + _accessCount;
        
        _logger.LogDebug("Computation performed by agent {AgentId}: {Input} -> {Result}", 
            this.GetPrimaryKey(), input, result);
        
        return Task.FromResult(result);
    }

    public Task<int> GetAccessCountAsync()
    {
        return Task.FromResult(_accessCount);
    }

    public async Task<string> SimulateDatabaseOperationAsync(int delayMs = 100)
    {
        _accessCount++;
        
        _logger.LogDebug("Simulating database operation for agent {AgentId} with {DelayMs}ms delay", 
            this.GetPrimaryKey(), delayMs);
        
        // Simulate database operation delay
        await Task.Delay(delayMs);
        
        return $"Database operation completed for agent {this.GetPrimaryKey()} after {delayMs}ms";
    }

    public Task<AgentMetadata> GetMetadataAsync()
    {
        _accessCount++;
        
        // Use a simpler approach for silo address - just use a placeholder for now
        string siloAddress = "TestSilo";
        
        var metadata = new AgentMetadata
        {
            AgentId = this.GetPrimaryKey(),
            ActivationTime = _activationTime,
            AccessCount = _accessCount,
            SiloAddress = siloAddress,
            IsWarmedUp = _isWarmedUp
        };
        
        return Task.FromResult(metadata);
    }

    /// <summary>
    /// Marks this agent as warmed up (called during warmup process)
    /// </summary>
    public void MarkAsWarmedUp()
    {
        _isWarmedUp = true;
        _logger.LogDebug("Agent {AgentId} marked as warmed up", this.GetPrimaryKey());
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var activeDuration = DateTime.UtcNow - _activationTime;
        
        _logger.LogInformation("TestWarmupAgent {AgentId} deactivating after {Duration}ms, " +
                              "access count: {AccessCount}, reason: {Reason}", 
            this.GetPrimaryKey(), activeDuration.TotalMilliseconds, _accessCount, reason);
        
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
} 