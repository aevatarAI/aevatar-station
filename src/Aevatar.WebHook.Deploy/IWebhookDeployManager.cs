namespace Aevatar.WebHook.Deploy;

public interface IWebhookDeployManager
{
    Task<string> CreateNewWebHookAsync(string appId, string version, string imageName);
    Task DestroyWebHookAsync(string appId, string version);
    Task RestartWebHookAsync(string appId,string version);
    
    Task<string> CreateNewAippAsync(string appId, string version, string imageName);
    Task DestroyAippAsync(string appId, string version);
    Task RestartAippAsync(string appId,string version);
}