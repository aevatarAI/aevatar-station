<<<<<<< HEAD
=======
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
>>>>>>> origin/dev
using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Permissions;
using Aevatar.Service;
using Aevatar.Webhook;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
<<<<<<< HEAD
using Volo.Abp;
=======
using Microsoft.Extensions.Logging;
using Volo.Abp;
using System.ComponentModel.DataAnnotations;
>>>>>>> origin/dev

namespace Aevatar.Admin.Controllers;

[RemoteService]
[ControllerName("App")]
[Route("api/webhook")]
public class WebhookController : AevatarController
{
    private readonly IWebhookService _webhookService;
<<<<<<< HEAD
    public WebhookController(IWebhookService webhookService)
    {
        _webhookService = webhookService;
=======
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IWebhookService webhookService,
        ILogger<WebhookController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    private Dictionary<string, byte[]> ExtractCodeFiles(IFormFileCollection codeFiles)
    {
        var result = new Dictionary<string, byte[]>();
        if (codeFiles != null && codeFiles.Count > 0)
        {
            foreach (var file in codeFiles)
            {
                if (file.Length > 0)
                {
                    using (var stream = new MemoryStream())
                    {
                        file.CopyTo(stream);
                        result[file.FileName] = stream.ToArray();
                    }
                }
            }
        }
        return result;
>>>>>>> origin/dev
    }

    [HttpPut]
    [Authorize(Policy = AevatarPermissions.AdminPolicy)]
    [Route("code/{webhookId}/{version}")]
    [RequestSizeLimit(209715200)]
    [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
<<<<<<< HEAD
    public async Task UploadCodeAsync(string webhookId,string version, [FromForm]CreateWebhookDto input)
    {
        byte[] codeBytes = null;
        if (input.Code != null && input.Code.Length > 0)
        {
            codeBytes = input.Code.GetAllBytes();
        }
         await  _webhookService.CreateWebhookAsync(webhookId,version,codeBytes);
    }


    [HttpGet("code")]
    public async Task<string> GetWebhookCodeAsync(string webhookId, string version)
    {
        return await  _webhookService.GetWebhookCodeAsync(webhookId,version);
    }
    
    [HttpPost]
    [Route("destroy")]
    [Authorize(Policy = AevatarPermissions.AdminPolicy)]
    public async Task DestroyAppAsync(DestroyWebhookDto input)
    {
        await _webhookService.DestroyWebhookAsync(input.WebhookId, input.Version);
    }
  
=======
    public async Task UploadCodeAsync(
        [Required] string webhookId,
        [Required] string version,
        [Required] [FromForm] CreateWebhookDto input)
    {
        var codeFiles = ExtractCodeFiles(input.Code);
        await _webhookService.CreateWebhookAsync(webhookId, version, codeFiles);
    }

    [Authorize]
    [HttpPut("updateCode")]
    public async Task UpdateCodeAsync([Required] [FromForm] CreateWebhookDto input)
    {
        var clientId = CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;
        if (!clientId.IsNullOrEmpty() && clientId.Contains("Aevatar"))
        {
            _logger.LogWarning($"UpdateDockerImageAsync unSupport client {clientId} ");
            throw new UserFriendlyException("unSupport client");
        }

        var codeFiles = ExtractCodeFiles(input.Code);
        if (codeFiles.Count > 0)
        {
            await _webhookService.UpdateCodeAsync(clientId, "1", codeFiles);
        }
    }

    [HttpGet("code")]
    public async Task<Dictionary<string, string>> GetWebhookCodeAsync(
        [Required] string webhookId,
        [Required] string version)
    {
        return await _webhookService.GetWebhookCodeAsync(webhookId, version);
    }

    [HttpPost]
    [Route("destroy")]
    [Authorize(Policy = AevatarPermissions.AdminPolicy)]
    public async Task DestroyAppAsync([Required] DestroyWebhookDto input)
    {
        await _webhookService.DestroyWebhookAsync(input.WebhookId, input.Version);
    }
>>>>>>> origin/dev
}