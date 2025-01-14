
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aevatar.App.Deploy;

public interface IAppDeployManager
{
    Task<string> CreateNewAppAsync(string appId, string version, string imageName);
    Task DestroyAppAsync(string appId, string version);
    Task RestartAppAsync(string appId,string version);
}