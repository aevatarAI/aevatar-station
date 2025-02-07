namespace Aevatar.WebHook.Deploy;

public class DefaultWebhookDeployManager : IWebhookDeployManager
{
    public async Task<string> CreateNewWebHookAsync(string appId, string version, string imageName)
    {
        return string.Empty;
    }

    public async Task DestroyWebHookAsync(string appId, string version)
    {
        return;
    }

    public async Task RestartWebHookAsync(string appId, string version)
    {
        return;
    }

    public async Task<string> CreateNewAippAsync(string appId, string version, string imageName)
    {
        return string.Empty;
    }

    public async Task DestroyAippAsync(string appId, string version)
    {
        return;
    }

    public async Task RestartAippAsync(string appId, string version)
    {
        return;
    }
}