using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
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
        
        string tempFile = null;
        
        try
        {
            // 检查Python命令是否存在
            string pythonCommand = "python3";
            try
            {
                var checkPythonInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = pythonCommand,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                var checkPythonProcess = new Process { StartInfo = checkPythonInfo };
                checkPythonProcess.Start();
                var pythonPath = checkPythonProcess.StandardOutput.ReadToEnd().Trim();
                checkPythonProcess.WaitForExit();
                
                if (string.IsNullOrEmpty(pythonPath))
                {
                    // 尝试使用python命令
                    pythonCommand = "python";
                    checkPythonInfo.Arguments = pythonCommand;
                    checkPythonProcess = new System.Diagnostics.Process { StartInfo = checkPythonInfo };
                    checkPythonProcess.Start();
                    pythonPath = checkPythonProcess.StandardOutput.ReadToEnd().Trim();
                    checkPythonProcess.WaitForExit();
                    
                    if (string.IsNullOrEmpty(pythonPath))
                    {
                        return StatusCode(500, new { error = "Python interpreter not found. Please install Python." });
                    }
                }
                
                _logger.LogInformation("Using Python interpreter at: {PythonPath}", pythonPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Python interpreter");
                return StatusCode(500, new { error = $"Error checking Python interpreter: {ex.Message}" });
            }
            
            // 创建临时Python文件
            var tempDir = Path.GetTempPath();
            tempFile = Path.Combine(tempDir, $"sandbox_{Guid.NewGuid():N}.py");
            _logger.LogInformation("Creating temporary file: {TempFile}", tempFile);
            System.IO.File.WriteAllText(tempFile, request.Code);
            
            // 执行Python代码
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonCommand,
                Arguments = $"\"{tempFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            _logger.LogInformation("Starting Python process: {Command} {Arguments}", startInfo.FileName, startInfo.Arguments);
            var process = new Process { StartInfo = startInfo };
            process.Start();
            
            // 在进程退出前获取处理器时间
            var startTime = DateTime.Now;
            
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            
            _logger.LogInformation("Python process output: {Output}", output);
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogError("Python process error: {Error}", error);
            }
            
            process.WaitForExit();
            var executionTime = (DateTime.Now - startTime).TotalSeconds;
            _logger.LogInformation("Python process exited with code: {ExitCode}", process.ExitCode);
            
            // 删除临时文件
            try
            {
                if (System.IO.File.Exists(tempFile))
                {
                    System.IO.File.Delete(tempFile);
                    _logger.LogInformation("Temporary file deleted: {TempFile}", tempFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting temporary file: {TempFile}", tempFile);
            }
            
            return Ok(new 
            {
                message = "Code execution successful",
                code = request.Code,
                language = request.Language,
                output = string.IsNullOrEmpty(error) ? output : error,
                exitCode = process.ExitCode,
                executionTime = executionTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Python code");
            
            // 尝试清理临时文件
            try
            {
                if (!string.IsNullOrEmpty(tempFile) && System.IO.File.Exists(tempFile))
                {
                    System.IO.File.Delete(tempFile);
                    _logger.LogInformation("Temporary file deleted during error handling: {TempFile}", tempFile);
                }
            }
            catch { /* 忽略清理过程中的错误 */ }
            
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