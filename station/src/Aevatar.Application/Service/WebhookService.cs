using System;
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

{
    private readonly IClusterClient _clusterClient;
    private readonly IHostDeployManager _hostDeployManager;
    private readonly WebhookDeployOptions _webhookDeployOptions;
        IOptions<WebhookDeployOptions> webhookDeployOptions)
    {
        _clusterClient = clusterClient;
        _hostDeployManager = hostDeployManager;
        _webhookDeployOptions = webhookDeployOptions.Value;
    }

    }

    public async Task DestroyWebhookAsync(string inputWebhookId, string inputVersion)
    {
