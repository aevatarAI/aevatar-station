using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ipdb;

namespace Aevatar.Application.Service;

/// <summary>
/// IP location
/// </summary>
public class IpLocationService : IIpLocationService
{
    private readonly ILogger<IpLocationService> _logger;
    private readonly City _cityDb;

    public IpLocationService(ILogger<IpLocationService> logger)
    {
        _logger = logger;
        var ipdbFilePath = "/geoip/ipipfree.ipdb";//"/Users/**/Downloads/ipipfreedb/ipipfree.ipdb";
        
        try
        {
            _logger.LogDebug("Loading IPDB file: {FilePath}", ipdbFilePath);
            _cityDb = new City(ipdbFilePath);
            _logger.LogDebug("IPDB file loaded successfully. Build time: {BuildTime}, Fields: {Fields}", 
                _cityDb.buildTime(), string.Join(",", _cityDb.fields()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load IPDB file: {FilePath}", ipdbFilePath);
            throw;
        }
    }

    /// <summary>
    /// check IP belong china mainland
    /// </summary>
    public Task<bool> IsIpInMainlandChinaAsync(string ipAddress)
    {
        return Task.Run(() =>
        {
            try
            {
                if (!IsValidIpAddress(ipAddress))
                {
                    _logger.LogDebug("Invalid IP address format: {IpAddress}", ipAddress);
                    return false;
                }

                var cityInfo = _cityDb.findInfo(ipAddress, "CN");
                if (cityInfo == null)
                {
                    _logger.LogDebug("No location info found for IP: {IpAddress}", ipAddress);
                    return false;
                }

                if (cityInfo.CountryName != "中国" && cityInfo.CountryCode != "CN")
                {
                    return false;
                }

                var regionName = cityInfo.RegionName?.ToLower();
                if (string.IsNullOrEmpty(regionName))
                {
                    return true;
                }

                if (regionName.Contains("香港") || regionName.Contains("hong kong") || regionName.Contains("hk"))
                    return false;
                if (regionName.Contains("台湾") || regionName.Contains("taiwan") || regionName.Contains("tw"))
                    return false;
                if (regionName.Contains("澳门") || regionName.Contains("macau") || regionName.Contains("mo"))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if IP {IpAddress} is in mainland China", ipAddress);
                return false;
            }
        });
    }

    /// <summary>
    /// GetIpLocationAsync
    /// </summary>
    public Task<IpLocationInfo> GetIpLocationAsync(string ipAddress)
    {
        return Task.Run(() =>
        {
            try
            {
                if (!IsValidIpAddress(ipAddress))
                {
                    _logger.LogWarning("Invalid IP address format: {IpAddress}", ipAddress);
                    return new IpLocationInfo { Country = "unknown" };
                }

                var cityInfo = _cityDb.findInfo(ipAddress, "CN");
                if (cityInfo == null)
                {
                    _logger.LogWarning("No location info found for IP: {IpAddress}", ipAddress);
                    return new IpLocationInfo { Country = "unknown" };
                }

                return new IpLocationInfo
                {
                    Country = cityInfo.CountryName ?? "unknown",
                    Region = cityInfo.RegionName ?? "unknown",
                    City = cityInfo.CityName ?? "unknown",
                    Isp = cityInfo.IspDomain ?? "unknown"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location for IP {IpAddress}", ipAddress);
                return new IpLocationInfo { Country = "unknown" };
            }
        });
    }

    /// <summary>
    /// IsValidIpAddress
    /// </summary>
    private bool IsValidIpAddress(string ipAddress)
    {
        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }
} 