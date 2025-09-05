using System.Threading.Tasks;
using Aevatar.Domain.Shared;

namespace Aevatar.Application.Service;

public interface IIpLocationService
{
    Task<bool> IsIpInMainlandChinaAsync(string ipAddress);
    Task<IpLocationInfo> GetIpLocationAsync(string ipAddress);
    
    // MaxMind specific methods
    Task<bool> IsIpInMainlandChinaMaxMindAsync(string ipAddress);
    Task<IpLocationInfo> GetIpLocationMaxMindAsync(string ipAddress);
    
    Task<bool> IsInMainlandChinaAsync(string ipAddress,string appTypeString);
    Task<IpLocationInfo> GetLocationAsync(string ipAddress);
}


public class IpLocationInfo
{
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Isp { get; set; } = string.Empty;
}