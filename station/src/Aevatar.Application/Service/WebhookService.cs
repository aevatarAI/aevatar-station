using System;
using System.Collections.Generic;
using System.IO;
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

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class WebhookService : ApplicationService, IWebhookService
{
    private readonly IClusterClient _clusterClient;
    private readonly IHostDeployManager _hostDeployManager;
    private readonly WebhookDeployOptions _webhookDeployOptions;

    public WebhookService(IClusterClient clusterClient, IHostDeployManager hostDeployManager,
        IOptions<WebhookDeployOptions> webhookDeployOptions)
    {
        _clusterClient = clusterClient;
        _hostDeployManager = hostDeployManager;
        _webhookDeployOptions = webhookDeployOptions.Value;
    }

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
    }

    public async Task DestroyWebhookAsync(string inputWebhookId, string inputVersion)
    {
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