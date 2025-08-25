using System;
using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;
using Aevatar.Sandbox.Abstractions.Grains;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace Aevatar.Sandbox.Controllers;

[RemoteService]
[Route("api/sandbox")]
[ApiController]
[Produces("application/json")]
[ApiExplorerSettings(GroupName = "Sandbox")]
public class SandboxController : AbpController
{
    private readonly IGrainFactory _grainFactory;

    public SandboxController(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    /// <summary>
    /// Executes code in a sandbox environment
    /// </summary>
    /// <param name="request">The code execution request</param>
    /// <returns>The execution result with execution ID</returns>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(SandboxExecutionResult), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    public async Task<ActionResult<SandboxExecutionResult>> ExecuteAsync([FromBody] SandboxExecutionRequest request)
    {
        var executionId = Guid.NewGuid().ToString("N");
        var grain = _grainFactory.GetGrain<ISandboxExecutionClientGrain>(executionId);

        var parameters = new SandboxExecutionClientParams
        {
            Code = request.Code,
            Timeout = request.Timeout,
            Language = request.Language,
            Resources = new ResourceLimits
            {
                CpuLimitCores = request.Resources?.CpuLimitCores ?? 1.0,
                MemoryLimitBytes = request.Resources?.MemoryLimitBytes ?? 512 * 1024 * 1024,
                TimeoutSeconds = request.Timeout
            }
        };

        var result = await grain.ExecuteAsync(parameters);
        result.ExecutionId = executionId;
        
        return Ok(result);
    }

    /// <summary>
    /// Gets the status of a code execution
    /// </summary>
    /// <param name="executionId">The execution ID</param>
    /// <returns>The execution status and result</returns>
    [HttpGet("status/{executionId}")]
    [ProducesResponseType(typeof(SandboxExecutionResult), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult<SandboxExecutionResult>> GetStatusAsync(string executionId)
    {
        var grain = _grainFactory.GetGrain<ISandboxExecutionClientGrain>(executionId);
        var result = await grain.GetResultAsync();
        return Ok(result);
    }

    /// <summary>
    /// Gets the logs for a code execution
    /// </summary>
    /// <param name="executionId">The execution ID</param>
    /// <returns>The execution logs</returns>
    [HttpGet("logs/{executionId}")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult<string>> GetLogsAsync(string executionId)
    {
        var grain = _grainFactory.GetGrain<ISandboxExecutionClientGrain>(executionId);
        var logs = await grain.GetLogsAsync();
        return Ok(logs);
    }

    /// <summary>
    /// Cancels a running code execution
    /// </summary>
    /// <param name="executionId">The execution ID</param>
    /// <returns>Success or failure message</returns>
    [HttpPost("cancel/{executionId}")]
    [ProducesResponseType(typeof(string), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<ActionResult> CancelAsync(string executionId)
    {
        var grain = _grainFactory.GetGrain<ISandboxExecutionClientGrain>(executionId);
        await grain.CancelAsync();
        return Ok();
    }
}