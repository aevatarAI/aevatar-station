using System;
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.Code;
using Aevatar.Common;
using Aevatar.Options;
using Aevatar.WebHook.Deploy;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.Application.Services;

namespace Aevatar.Service;


public interface IWebhookService
{
    Task UploadCodeAsync(string webhookId, string version, byte[]? codeBytes);
    Task<string> GetWebhookCodeAsync(string webhookId, string version);
}

public class WebhookService: ApplicationService, IWebhookService
{
    private readonly IClusterClient _clusterClient;
    private readonly IWebhookDeployManager _webhookDeployManager;
    private readonly WebhookDeployOptions _webhookDeployOptions;
    public WebhookService(IClusterClient clusterClient,IWebhookDeployManager webhookDeployManager,
        IOptions<WebhookDeployOptions> webhookDeployOptions)
    {
        _clusterClient = clusterClient;
        _webhookDeployManager = webhookDeployManager;
        _webhookDeployOptions = webhookDeployOptions.Value;
    }

    public async Task UploadCodeAsync(string webhookId, string version, byte[]? codeBytes)
    {
        if (codeBytes !=null)
        {
            await _clusterClient.GetGrain<ICodeGAgent>(GuidUtil.StringToGuid(webhookId)).UploadCodeAsync(
                webhookId,version,codeBytes);
        }
       await _webhookDeployManager.CreateNewWebHookAsync(webhookId, version,_webhookDeployOptions.WebhookImageName);
    }

    public async Task<string> GetWebhookCodeAsync(string webhookId, string version)
    {
        var webhookCode = await _clusterClient.GetGrain<ICodeGAgent>(GuidUtil.StringToGuid(webhookId)).GetStateAsync();
        return Convert.ToBase64String(webhookCode.Code);
    }
}


