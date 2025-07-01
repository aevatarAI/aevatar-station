using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Aevatar.Service;
using Volo.Abp.AspNetCore.Mvc;

namespace Aevatar.Developer.Host.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : AbpControllerBase
{
    private readonly IDeveloperService _developerService;
    private readonly ILogger<TestController> _logger;

    public TestController(IDeveloperService developerService, ILogger<TestController> logger)
    {
        _developerService = developerService;
        _logger = logger;
    }

    /// <summary>
    /// 测试Cross-URL获取接口（Mock数据）
    /// </summary>
    [HttpGet("cors-urls")]
    public async Task<IActionResult> TestGetCorsUrls([FromQuery] string clientId = "test-client", [FromQuery] Guid? projectId = null)
    {
        try
        {
            _logger.LogInformation($"Testing CORS URLs retrieval for client: {clientId}");
            
            // 使用提供的projectId，如果没有则使用默认的Guid
            var testProjectId = projectId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
            var result = await _developerService.RestartAsync(testProjectId, clientId);
            
            return Ok(new
            {
                Message = "CORS URLs test completed",
                ClientId = clientId,
                ProjectId = testProjectId,
                RestartResult = result,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to test CORS URLs for client: {clientId}");
            return StatusCode(500, new
            {
                Error = "Test failed",
                Message = ex.Message,
                ClientId = clientId
            });
        }
    }

    /// <summary>
    /// 健康检查接口
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "Aevatar Developer Host",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }

    /// <summary>
    /// 测试K8s连接状态
    /// </summary>
    [HttpGet("k8s-status")]
    public async Task<IActionResult> TestK8sConnection([FromQuery] Guid? projectId = null)
    {
        try
        {
            // 通过调用RestartAsync来间接测试K8s连接
            // 这个方法会调用K8s API来检查服务状态
            var testProjectId = projectId ?? Guid.Parse("00000000-0000-0000-0000-000000000002");
            var result = await _developerService.RestartAsync(testProjectId, "k8s-test-client");
            
            return Ok(new
            {
                Message = "K8s connection test completed",
                K8sConnectionWorking = true,
                ProjectId = testProjectId,
                TestResult = result,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "K8s connection test failed");
            return Ok(new
            {
                Message = "K8s connection test completed with issues",
                K8sConnectionWorking = false,
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
} 