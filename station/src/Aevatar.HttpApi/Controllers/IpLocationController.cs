using System.Threading.Tasks;
using Aevatar.Application.Service;
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
            
            var result = await _ipLocationService.IsIpInMainlandChinaAsync(ip);
            
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
            
            var locationInfo = await _ipLocationService.GetIpLocationAsync(ip);
            
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
    
} 