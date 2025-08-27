using System.Threading.Tasks;
using Aevatar.Controllers;
using Aevatar.Permissions;
using Aevatar.Service;
using Aevatar.Webhook;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Admin.Controllers;

[RemoteService]
[ControllerName("App")]
[Route("api/webhook")]
public class WebhookController : AevatarController
{
    private readonly IWebhookService _webhookService;
    public WebhookController(IWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpPut]
    [Authorize(Policy = AevatarPermissions.AdminPolicy)]
    [Route("code/{webhookId}/{version}")]
    [RequestSizeLimit(209715200)]
    [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
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
  
}