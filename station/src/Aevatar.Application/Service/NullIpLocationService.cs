// ABOUTME: Null implementation of IIpLocationService for environments without GeoIP databases
// ABOUTME: Returns default values when GeoIP files are not available

using System.Threading.Tasks;
using Aevatar.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Aevatar.Application.Service;

/// <summary>
/// Null implementation of IIpLocationService for environments where GeoIP files are not available
/// </summary>
public class NullIpLocationService : IIpLocationService
{
    private readonly ILogger<NullIpLocationService> _logger;

    public NullIpLocationService(ILogger<NullIpLocationService> logger)
    {
        _logger = logger;
        _logger.LogWarning("Using NullIpLocationService - GeoIP databases not available");
    }

    public Task<bool> IsIpInMainlandChinaAsync(string ipAddress)
    {
        _logger.LogDebug("NullIpLocationService: IsIpInMainlandChinaAsync called for {IpAddress}", ipAddress);
        return Task.FromResult(false);
    }

    public Task<IpLocationInfo> GetIpLocationAsync(string ipAddress)
    {
        _logger.LogDebug("NullIpLocationService: GetIpLocationAsync called for {IpAddress}", ipAddress);
        return Task.FromResult(new IpLocationInfo
        {
            Country = "Unknown",
            Region = "Unknown",
            City = "Unknown",
            Isp = "Unknown"
        });
    }

    public Task<bool> IsIpInMainlandChinaMaxMindAsync(string ipAddress)
    {
        _logger.LogDebug("NullIpLocationService: IsIpInMainlandChinaMaxMindAsync called for {IpAddress}", ipAddress);
        return Task.FromResult(false);
    }

    public Task<IpLocationInfo> GetIpLocationMaxMindAsync(string ipAddress)
    {
        _logger.LogDebug("NullIpLocationService: GetIpLocationMaxMindAsync called for {IpAddress}", ipAddress);
        return Task.FromResult(new IpLocationInfo
        {
            Country = "Unknown",
            Region = "Unknown",
            City = "Unknown",
            Isp = "Unknown"
        });
    }

    public Task<bool> IsInMainlandChinaAsync(string ipAddress)
    {
        _logger.LogDebug("NullIpLocationService: IsInMainlandChinaAsync called for {IpAddress}", ipAddress);
        return Task.FromResult(false);
    }

    public Task<IpLocationInfo> GetLocationAsync(string ipAddress)
    {
        _logger.LogDebug("NullIpLocationService: GetLocationAsync called for {IpAddress}", ipAddress);
        return Task.FromResult(new IpLocationInfo
        {
            Country = "Unknown",
            Region = "Unknown",
            City = "Unknown",
            Isp = "Unknown"
        });
    }
}
