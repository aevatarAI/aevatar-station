namespace Aevatar.Kubernetes.Interfaces;

public interface IHostDeployManager
{
    Task CreateApplicationAsync(string appId, string version, string corsUrls, Guid tenantId);
    
    Task<string> CreateNewWebHookAsync(string appId, string version, string imageName);
    
    Task UpgradeApplicationAsync(string appId, string version, string corsUrls, Guid tenantId);
    
    Task RestartHostAsync(string appId, string version);
    
    Task RestartWebHookAsync(string appId, string version);
    
    Task DestroyApplicationAsync(string appId, string version);
    
    Task DestroyWebHookAsync(string appId, string version);
    
    Task UpdateDeploymentImageAsync(string appId, string version, string newImage);
} 