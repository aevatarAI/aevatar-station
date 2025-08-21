using System.Threading.Tasks;

namespace Aevatar.Application.Service;

public interface IIpLocationService
{
    Task<bool> IsIpInMainlandChinaAsync(string ipAddress);
    Task<IpLocationInfo> GetIpLocationAsync(string ipAddress);
}


public class IpLocationInfo
{
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Isp { get; set; } = string.Empty;
}