
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.App.Deploy;

public class DefaultAppDeployManager : IAppDeployManager
{
    public async Task<string> CreateNewAppAsync(string appId, string version, string imageName)
    {
        return string.Empty;
    }

    public async Task DestroyAppAsync(string appId, string version)
    {
        return;
    }

    public async Task RestartAppAsync(string appId, string version)
    {
        return;
    }
  
}