using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aevatar.Sandbox.HttpApi.Host.Controllers;

[ApiController]
[Route("api/test")]
[AllowAnonymous]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Hello from test endpoint!" });
    }
    
    [HttpPost("execute")]
    public IActionResult Execute([FromBody] TestRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request is null");
        }
        
        return Ok(new 
        {
            message = "Code execution successful",
            code = request.Code,
            language = request.Language,
            output = "Hello from sandbox!",
            executionTime = 0.123
        });
    }
}

public class TestRequest
{
    public string Code { get; set; }
    public string Language { get; set; }
    public ResourceLimits ResourceLimits { get; set; }
}

public class ResourceLimits
{
    public double CpuLimitCores { get; set; }
    public long MemoryLimitBytes { get; set; }
    public int TimeoutSeconds { get; set; }
}