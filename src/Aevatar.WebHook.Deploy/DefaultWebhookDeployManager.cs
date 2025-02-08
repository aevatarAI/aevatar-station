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

    public async Task<string> CreateNewDaippAsync(string appId, string version, string imageName)
    {
        return string.Empty;
    }

    public async Task DestroyDaippAsync(string appId, string version)
    {
        return;
    }

    public async Task RestartDaippAsync(string appId, string version)
    {
        return;
    }
}