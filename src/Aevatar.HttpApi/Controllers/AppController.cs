using System.Threading.Tasks;
using Aevatar.Listener;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("App")]
[Route("api/listener")]
public class AppController : AevatarController
{

    public AppController()
    {
    }

    [HttpPut]
    [Authorize]
    [Route("code/{listenerId}/{version}")]
    [RequestSizeLimit(209715200)]
    [RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
    public async Task UpdateCodeAsync(string version, [FromForm]CreateListenerDto input)
    {
        byte[] codeBytes = null;
        if (input.Code != null && input.Code.Length > 0)
        {
            codeBytes = input.Code.GetAllBytes();
        }
    }


    [HttpGet("code")]
    public async Task<string> GetAppCodeAsync(string listenerId, string version)
    {
        return "code";
    }
  
}