using System.Threading.Tasks;
using Aevatar.Application.Service;
using Aevatar.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aevatar.HttpApi.Controllers;

/// <summary>
/// IP location
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IpLocationController : ControllerBase
{
    private readonly IIpLocationService _ipLocationService;
    private readonly ILogger<IpLocationController> _logger;

    public IpLocationController(IIpLocationService ipLocationService, ILogger<IpLocationController> logger)
    {
        _ipLocationService = ipLocationService;
        _logger = logger;
    }
    
    [HttpGet("is-mainland-cn")]
    public async Task<IActionResult> IsIpInMainlandCN()
    {
        try
        {
            var ip = HttpContext.GetClientIpAddress();

            if (string.IsNullOrWhiteSpace(ip))
            {
                return BadRequest("IP address is required");
            }

            _logger.LogInformation("Checking if IP {IpAddress} is in mainland China", ip);
            
            var result = await _ipLocationService.IsInMainlandChinaAsync(ip);
            
            return Ok(new
            {
                Ip = ip,
                IsInMainlandChina = result,
                Message = result ? "The IP address belongs to Chinese Mainland" :"The IP address does not belong to Chinese Mainland"
            });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error checking IP {IpAddress}", HttpContext.GetClientIpAddress());
            return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
        }
    }
    
    [HttpGet("is-mainland-china")]
    public async Task<IActionResult> IsIpInMainlandChina([FromQuery] string ip)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return BadRequest("IP address is required");
            }

            _logger.LogInformation("Checking if IP {IpAddress} is in mainland China", ip);
            
            var result = await _ipLocationService.IsInMainlandChinaAsync(ip);
            
            return Ok(new
            {
                Ip = ip,
                IsInMainlandChina = result,
                Message = result ? "The IP address belongs to Chinese Mainland" :"The IP address does not belong to Chinese Mainland"
            });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error checking IP {IpAddress}", ip);
            return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
        }
    }
    
    [HttpGet("location")]
    public async Task<IActionResult> GetIpLocation([FromQuery] string ip)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return BadRequest("IP address is required");
            }

            _logger.LogInformation("Getting location for IP {IpAddress}", ip);
            
            var locationInfo = await _ipLocationService.GetLocationAsync(ip);
            
            return Ok(new
            {
                Ip = ip,
                Location = locationInfo
            });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error getting location for IP {IpAddress}", ip);
            return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
        }
    }
    
    [HttpGet("maxmind/is-mainland-china")]
    public async Task<IActionResult> IsIpInMainlandChinaMaxMind([FromQuery] string ip)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return BadRequest("IP address is required");
            }

            _logger.LogInformation("Checking if IP {IpAddress} is in mainland China using MaxMind", ip);
            
            var result = await _ipLocationService.IsIpInMainlandChinaMaxMindAsync(ip);
            
            return Ok(new
            {
                Ip = ip,
                IsInMainlandChina = result,
                Database = "MaxMind",
                Message = result ? "The IP address belongs to Chinese Mainland" : "The IP address does not belong to Chinese Mainland"
            });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error checking IP {IpAddress} using MaxMind", ip);
            return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
        }
    }
    
    [HttpGet("maxmind/location")]
    public async Task<IActionResult> GetIpLocationMaxMind([FromQuery] string ip)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return BadRequest("IP address is required");
            }

            _logger.LogInformation("Getting location for IP {IpAddress} using MaxMind", ip);
            
            var locationInfo = await _ipLocationService.GetIpLocationMaxMindAsync(ip);
            
            return Ok(new
            {
                Ip = ip,
                Database = "MaxMind",
                Location = locationInfo
            });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error getting location for IP {IpAddress} using MaxMind", ip);
            return StatusCode(500, new { Error = "Internal server error", Message = ex.Message });
        }
    }
} 