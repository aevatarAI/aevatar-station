using System.Threading.Tasks;
using Aevatar.Sandbox.Abstractions.Contracts;
using Aevatar.Sandbox.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aevatar.Sandbox.HttpApi.Host.Controllers;

[ApiController]
[Route("api/sandbox")]
[Authorize]
public class SandboxController : ControllerBase
{
    private readonly ISandboxService _pythonService; // Inject specific language service

    public SandboxController(ISandboxService pythonService)
    {
        _pythonService = pythonService;
    }

    [HttpPost("execute")]
    public async Task<ActionResult<SandboxExecutionHandle>> Execute(
        [FromBody] SandboxExecutionRequest request)
    {
        // TODO: Add request validation
        var handle = await _pythonService.StartAsync(request);
        return Ok(handle);
    }

    [HttpGet("result/{sandboxExecutionId}")]
    public async Task<ActionResult<SandboxExecutionResult>> GetResult(
        string sandboxExecutionId)
    {
        var result = await _pythonService.TryGetResultAsync(sandboxExecutionId);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("cancel/{sandboxExecutionId}")]
    public async Task<IActionResult> Cancel(string sandboxExecutionId)
    {
        var cancelled = await _pythonService.CancelAsync(sandboxExecutionId);
        if (!cancelled)
            return NotFound();

        return Ok();
    }

    [HttpGet("logs/{sandboxExecutionId}")]
    public async Task<ActionResult<SandboxLogs>> GetLogs(
        string sandboxExecutionId,
        [FromQuery] LogQueryOptions options)
    {
        var logs = await _pythonService.GetLogsAsync(sandboxExecutionId, options);
        return Ok(logs);
    }
}