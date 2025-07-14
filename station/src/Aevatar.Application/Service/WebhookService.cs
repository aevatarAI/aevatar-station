using System;
<<<<<<< HEAD
=======
using System.Collections.Generic;
using System.IO;
>>>>>>> origin/dev
using System.Threading.Tasks;
using Aevatar.Application.Grains.Agents.Code;
using Aevatar.Common;
using Aevatar.Options;
using Aevatar.WebHook.Deploy;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace Aevatar.Service;

<<<<<<< HEAD

public interface IWebhookService
{
    Task CreateWebhookAsync(string webhookId, string version, byte[]? codeBytes);
    Task<string> GetWebhookCodeAsync(string webhookId, string version);
    Task DestroyWebhookAsync(string inputWebhookId, string inputVersion);
}

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class WebhookService: ApplicationService, IWebhookService
=======
[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class WebhookService : ApplicationService, IWebhookService
>>>>>>> origin/dev
{
    private readonly IClusterClient _clusterClient;
    private readonly IHostDeployManager _hostDeployManager;
    private readonly WebhookDeployOptions _webhookDeployOptions;
<<<<<<< HEAD
    public WebhookService(IClusterClient clusterClient,IHostDeployManager hostDeployManager,
=======

    public WebhookService(IClusterClient clusterClient, IHostDeployManager hostDeployManager,
>>>>>>> origin/dev
        IOptions<WebhookDeployOptions> webhookDeployOptions)
    {
        _clusterClient = clusterClient;
        _hostDeployManager = hostDeployManager;
        _webhookDeployOptions = webhookDeployOptions.Value;
    }

<<<<<<< HEAD
    public async Task CreateWebhookAsync(string webhookId, string version, byte[]? codeBytes)
    {
        if (codeBytes !=null)
        {
            await _clusterClient.GetGrain<ICodeGAgent>(GuidUtil.StringToGuid(webhookId)).UploadCodeAsync(
                webhookId,version,codeBytes);
            await _hostDeployManager.CreateNewWebHookAsync(webhookId, version,_webhookDeployOptions.WebhookImageName);

        }
    }

    public async Task<string> GetWebhookCodeAsync(string webhookId, string version)
    {
        var webhookCode = await _clusterClient.GetGrain<ICodeGAgent>(GuidUtil.StringToGuid(webhookId)).GetStateAsync();
        return Convert.ToBase64String(webhookCode.Code);
=======
    public async Task CreateWebhookAsync(string webhookId, string version, Dictionary<string, byte[]> codeFiles)
    {
        if (codeFiles != null && codeFiles.Count > 0)
        {
            await _clusterClient.GetGrain<ICodeGAgent>(GuidUtil.StringToGuid(webhookId)).UploadCodeAsync(
                webhookId, version, codeFiles);
            await _hostDeployManager.CreateNewWebHookAsync(webhookId, version, _webhookDeployOptions.WebhookImageName);
        }
    }

    public async Task<Dictionary<string, string>> GetWebhookCodeAsync(string webhookId, string version)
    {
        var webhookCode = await _clusterClient.GetGrain<ICodeGAgent>(GuidUtil.StringToGuid(webhookId)).GetStateAsync();
        var result = new Dictionary<string, string>();
        foreach (var file in webhookCode.CodeFiles)
        {
            result[file.Key] = Convert.ToBase64String(file.Value);
        }
        return result;
>>>>>>> origin/dev
    }

    public async Task DestroyWebhookAsync(string inputWebhookId, string inputVersion)
    {
<<<<<<< HEAD
        await _hostDeployManager.DestroyWebHookAsync(inputWebhookId, inputVersion);
    }
}


=======
        // Clear all CodeFiles in ICodeGAgent by uploading an empty dictionary
        await _clusterClient.GetGrain<ICodeGAgent>(GuidUtil.StringToGuid(inputWebhookId)).UploadCodeAsync(
            inputWebhookId, inputVersion, new Dictionary<string, byte[]>());
        
        await _hostDeployManager.DestroyWebHookAsync(inputWebhookId, inputVersion);
    }

    public async Task UpdateCodeAsync(string webhookId, string version, Dictionary<string, byte[]> codeFiles)
    {
        if (codeFiles != null && codeFiles.Count > 0)
        {
            await _clusterClient.GetGrain<ICodeGAgent>(GuidUtil.StringToGuid(webhookId)).UploadCodeAsync(
                webhookId, version, codeFiles);
        }
    }
}
>>>>>>> origin/dev
