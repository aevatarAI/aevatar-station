using System.Threading.Tasks;
using Aevatar.Service;
using Aevatar.Webhook;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("App")]
[Route("api/daipp")]
public class DaippController : AevatarController
{
    private readonly IDaippService _daippService;
    public DaippController(IDaippService daippService)
    {
        _daippService = daippService;
    }

    [HttpPost]
    [Route("create")]
   // [Authorize(Policy = "OnlyAdminAccess")]
    public async Task CreateAppAsync(DestroyWebhookDto input)
    {
        await _daippService.CreateDaippAsync(input.WebhookId, input.Version);
    }
    
    [HttpPost]
    [Route("destroy")]
   // [Authorize(Policy = "OnlyAdminAccess")]
    public async Task DestroyAppAsync(DestroyWebhookDto input)
    {
        await _daippService.DestroyDaippAsync(input.WebhookId, input.Version);
    }
  
}