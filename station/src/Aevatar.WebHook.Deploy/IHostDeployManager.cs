using Aevatar.Enum;

namespace Aevatar.WebHook.Deploy;

public interface IHostDeployManager
{
    Task<string> CreateNewWebHookAsync(string appId, string version, string imageName);
    Task DestroyWebHookAsync(string appId, string version);
    Task RestartWebHookAsync(string appId,string version);
    
    Task<string> CreateHostAsync(string appId, string version, string corsUrls);
    Task DestroyHostAsync(string appId, string version);
    Task RestartHostAsync(string appId,string version);

    public Task UpdateDockerImageAsync(string appId, string version, string newImage);

    /// <summary>
    /// Updates existing K8s ConfigMaps with the latest business configuration for specific host type
    /// </summary>
    /// <param name="hostId">Host identifier</param>
    /// <param name="version">Host version</param>
    /// <param name="hostType">Host type to update</param>
    Task UpdateBusinessConfigurationAsync(string hostId, string version, HostTypeEnum hostType);
}