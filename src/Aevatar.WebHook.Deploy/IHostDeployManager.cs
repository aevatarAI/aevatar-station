namespace Aevatar.WebHook.Deploy;

public interface IHostDeployManager
{
    Task<string> CreateNewWebHookAsync(string appId, string version, string imageName);
    Task DestroyWebHookAsync(string appId, string version);
    Task RestartWebHookAsync(string appId,string version);
    
    Task CreateApplicationAsync(string appId, string version, string corsUrls, Guid tenantId);
    Task DestroyApplicationAsync(string appId, string version);
    Task UpgradeApplicationAsync(string appId, string version, string corsUrls, Guid tenantId);
    
    Task RestartHostAsync(string appId,string version);
    public Task UpdateDeploymentImageAsync(string appId, string version, string newImage);

}