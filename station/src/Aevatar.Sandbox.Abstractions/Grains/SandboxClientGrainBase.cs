using System;
using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;
using Aevatar.Sandbox.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Aevatar.Sandbox.Abstractions.Grains;

[GenerateSerializer]
public class SandboxExecutionState
{
    [Id(0)]
    public ExecutionStatus Status { get; set; }

    [Id(1)]
    public DateTime? StartTime { get; set; }

    [Id(2)]
    public DateTime? EndTime { get; set; }

    [Id(3)]
    public SandboxExecutionResult? Result { get; set; }

    [Id(4)]
    public string Error { get; set; } = string.Empty;

    [Id(5)]
    public SandboxLogs? Logs { get; set; }
}

public abstract class SandboxClientGrainBase : Grain, ISandboxExecutionClientGrain
{
    private readonly IPersistentState<SandboxExecutionState> _state;
    private readonly ISandboxService _sandboxService;
    private readonly ILogger _logger;

    protected SandboxClientGrainBase(
        [PersistentState("execution")] IPersistentState<SandboxExecutionState> state,
        ISandboxService sandboxService,
        ILogger logger)
    {
        _state = state;
        _sandboxService = sandboxService;
        _logger = logger;
    }

    public virtual async Task<SandboxExecutionResult> ExecuteAsync(SandboxExecutionClientParams parameters)
    {
        try
        {
            _state.State.Status = ExecutionStatus.Running;
            _state.State.StartTime = DateTime.UtcNow;
            await _state.WriteStateAsync();

            var result = await _sandboxService.ExecuteAsync(
                parameters.Code,
                parameters.Timeout,
                parameters.Resources);

            _state.State.Status = result.Status;
            _state.State.EndTime = result.EndTime;
            _state.State.Result = result;
            await _state.WriteStateAsync();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute sandbox code");
            _state.State.Status = ExecutionStatus.Failed;
            _state.State.EndTime = DateTime.UtcNow;
            _state.State.Error = ex.Message;
            await _state.WriteStateAsync();

            return new SandboxExecutionResult
            {
                ExecutionId = this.GetPrimaryKeyString(),
                Status = ExecutionStatus.Failed,
                StartTime = _state.State.StartTime ?? DateTime.UtcNow,
                EndTime = _state.State.EndTime ?? DateTime.UtcNow,
                Error = ex.Message
            };
        }
    }

    public virtual async Task<SandboxExecutionResult> GetResultAsync()
    {
        if (_state.State.Result != null)
        {
            return _state.State.Result;
        }

        return new SandboxExecutionResult
        {
            ExecutionId = this.GetPrimaryKeyString(),
            Status = _state.State.Status,
            StartTime = _state.State.StartTime ?? DateTime.UtcNow,
            EndTime = _state.State.EndTime ?? DateTime.UtcNow,
            Error = _state.State.Error
        };
    }

    public virtual async Task<SandboxLogs> GetLogsAsync(LogQueryOptions? options = null)
    {
        if (_state.State.Logs != null)
        {
            return _state.State.Logs;
        }

        var logs = await _sandboxService.GetLogsAsync(this.GetPrimaryKeyString(), options);
        _state.State.Logs = logs;
        await _state.WriteStateAsync();

        return logs;
    }

    public virtual async Task CancelAsync()
    {
        try
        {
            await _sandboxService.CancelAsync(this.GetPrimaryKeyString());
            _state.State.Status = ExecutionStatus.Cancelled;
            _state.State.EndTime = DateTime.UtcNow;
            await _state.WriteStateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel sandbox execution");
            throw;
        }
    }
}