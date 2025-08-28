using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Aevatar.Sandbox.HttpApi.Host.Controllers;

[Route("api/noauth")]
[ApiController]
[AllowAnonymous]
public class NoAuthController : ControllerBase
{
    private readonly ILogger<NoAuthController> _logger;

    public NoAuthController(ILogger<NoAuthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("NoAuthController.Get called");
        return Ok(new { message = "Hello from NoAuthController!" });
    }

    [HttpPost("execute")]
    public IActionResult Execute([FromBody] ExecuteRequest request)
    {
        _logger.LogInformation("NoAuthController.Execute called with code: {Code}", request?.Code);
        
        if (request == null)
        {
            return BadRequest("Request is null");
        }
        
        try
        {
            // 创建临时Python文件
            var tempFile = Path.GetTempFileName() + ".py";
            System.IO.File.WriteAllText(tempFile, request.Code);
            
            // 执行Python代码
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                Arguments = tempFile,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            var process = new System.Diagnostics.Process { StartInfo = startInfo };
            process.Start();
            
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            // 删除临时文件
            System.IO.File.Delete(tempFile);
            
            return Ok(new 
            {
                message = "Code execution successful",
                code = request.Code,
                language = request.Language,
                output = string.IsNullOrEmpty(error) ? output : error,
                exitCode = process.ExitCode,
                executionTime = process.TotalProcessorTime.TotalSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Python code");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class ExecuteRequest
{
    public string Code { get; set; }
    public string Language { get; set; }
    public ExecuteResourceLimits ResourceLimits { get; set; }
}

public class ExecuteResourceLimits
{
    public double CpuLimitCores { get; set; }
    public long MemoryLimitBytes { get; set; }
    public int TimeoutSeconds { get; set; }
}