using System.Threading.Tasks;
using Aevatar.Domain.Shared;

namespace Aevatar.Application.Service;

/// <summary>
/// Null implementation of IIpLocationService for development environments
/// where GeoIP database files are not available
/// </summary>
public class NullIpLocationService : IIpLocationService
{
    public Task<bool> IsIpInMainlandChinaAsync(string ipAddress)
    {
        return Task.FromResult(false);
    }

    public Task<IpLocationInfo> GetIpLocationAsync(string ipAddress)
    {
        return Task.FromResult(new IpLocationInfo { Country = "unknown" });
    }

    public Task<bool> IsIpInMainlandChinaMaxMindAsync(string ipAddress)
    {
        return Task.FromResult(false);
    }

    public Task<IpLocationInfo> GetIpLocationMaxMindAsync(string ipAddress)
    {
        return Task.FromResult(new IpLocationInfo { Country = "unknown" });
    }

    public Task<bool> IsInMainlandChinaAsync(string ipAddress)
    {
        return Task.FromResult(false);
    }

    public Task<bool> IsInMainlandChinaAsync(string ipAddress, string appTypeString)
    {
        return Task.FromResult(false);
    }

    public Task<IpLocationInfo> GetLocationAsync(string ipAddress)
    {
        return Task.FromResult(new IpLocationInfo { Country = "unknown" });
    }
}
