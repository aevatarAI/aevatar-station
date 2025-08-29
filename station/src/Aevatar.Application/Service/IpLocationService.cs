using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ipdb;
using MaxMind.GeoIP2;

namespace Aevatar.Application.Service;

/// <summary>
/// IP location
/// </summary>
public class IpLocationService : IIpLocationService
{
    private readonly ILogger<IpLocationService> _logger;
    private readonly City _cityDb;
    private readonly DatabaseReader _maxMindReader;

    public IpLocationService(ILogger<IpLocationService> logger)
    {
        _logger = logger;
        var ipdbFilePath = "/app/geoip/ipipfree.ipdb";//"/Users/**/Downloads/ipipfreedb/ipipfree.ipdb";
        var maxMindFilePath ="/app/geoip/GeoLite2-City.mmdb";// "/Users/**/Downloads/GeoLite2-City_20250819/GeoLite2-City.mmdb";
        
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
        
        try
        {
            _logger.LogDebug("Loading MaxMind database file: {FilePath}", maxMindFilePath);
            _maxMindReader = new DatabaseReader(maxMindFilePath);
            _logger.LogDebug("MaxMind database file loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load MaxMind database file: {FilePath}", maxMindFilePath);
            throw;
        }
    }

    /// <summary>
    /// check IP belong china mainland
    /// </summary>
    public Task<bool> IsIpInMainlandChinaAsync(string ipAddress)
    {
       // return true;
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
    /// Check if IP belongs to mainland China using MaxMind database
    /// </summary>
    public async Task<bool> IsIpInMainlandChinaMaxMindAsync(string ipAddress)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!IsValidIpAddress(ipAddress))
                {
                    _logger.LogDebug("Invalid IP address format: {IpAddress}", ipAddress);
                    return false;
                }

                var response = _maxMindReader.City(ipAddress);
                
                // Check if country is China
                if (response.Country?.IsoCode != "CN")
                {
                    return false;
                }

                // Check if it's Hong Kong, Macau, or Taiwan
                var regionName = response.MostSpecificSubdivision?.Name?.ToLower();
                var regionIsoCode = response.MostSpecificSubdivision?.IsoCode?.ToLower();
                
                if (string.IsNullOrEmpty(regionName) && string.IsNullOrEmpty(regionIsoCode))
                {
                    return true; // If no region info, assume mainland China
                }

                // Check for Hong Kong
                if (regionName?.Contains("hong kong") == true || 
                    regionIsoCode == "hk" ||
                    regionName?.Contains("香港") == true)
                {
                    return false;
                }

                // Check for Taiwan
                if (regionName?.Contains("taiwan") == true || 
                    regionIsoCode == "tw" ||
                    regionName?.Contains("台湾") == true)
                {
                    return false;
                }

                // Check for Macau
                if (regionName?.Contains("macau") == true || 
                    regionIsoCode == "mo" ||
                    regionName?.Contains("澳门") == true)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if IP {IpAddress} is in mainland China using MaxMind", ipAddress);
                return false;
            }
        });
    }

    /// <summary>
    /// Get IP location using MaxMind database
    /// </summary>
    public Task<IpLocationInfo> GetIpLocationMaxMindAsync(string ipAddress)
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

                var response = _maxMindReader.City(ipAddress);
                
                return new IpLocationInfo
                {
                    Country = response.Country?.Name ?? "unknown",
                    Region = response.MostSpecificSubdivision?.Name ?? "unknown",
                    City = response.City?.Name ?? "unknown",
                    Isp = response.Traits?.Isp ?? "unknown"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting location for IP {IpAddress} using MaxMind", ipAddress);
                return new IpLocationInfo { Country = "unknown" };
            }
        });
    }

    public Task<bool> IsInMainlandChinaAsync(string ipAddress)
    {
        return IsIpInMainlandChinaMaxMindAsync(ipAddress);

    }

    public Task<IpLocationInfo> GetLocationAsync(string ipAddress)
    {
        return GetIpLocationMaxMindAsync(ipAddress);
    }

    /// <summary>
    /// IsValidIpAddress
    /// </summary>
    private bool IsValidIpAddress(string ipAddress)
    {
        return System.Net.IPAddress.TryParse(ipAddress, out _);
    }
} 