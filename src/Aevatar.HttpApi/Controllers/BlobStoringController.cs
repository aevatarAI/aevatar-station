using System.Threading.Tasks;
using Aevatar.BlobStorings;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.BlobStoring;

namespace Aevatar.Controllers;

[RemoteService]
[ControllerName("Blob")]
[Route("api/blob")]
[Authorize]
public class BlobStoringController : AevatarController
{
    private readonly IBlobContainer _blobContainer;

    public BlobStoringController(IBlobContainer blobContainer)
    {
        _blobContainer = blobContainer;
    }

    [HttpPost]
    public async Task SaveAsync([FromForm] SaveBlobInput input)
    {
        await _blobContainer.SaveAsync(input.File.FileName, input.File.OpenReadStream(), true);
    }
}