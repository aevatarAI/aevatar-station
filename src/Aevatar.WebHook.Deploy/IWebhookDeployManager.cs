namespace Aevatar.WebHook.Deploy;

public interface IWebhookDeployManager
{
    Task<string> CreateNewWebHookAsync(string appId, string version, string imageName);
    Task DestroyWebHookAsync(string appId, string version);
    Task RestartWebHookAsync(string appId,string version);
    
    Task<string> CreateNewDaippAsync(string appId, string version, string imageName);
    Task DestroyDaippAsync(string appId, string version);
    Task RestartDaippAsync(string appId,string version);
}