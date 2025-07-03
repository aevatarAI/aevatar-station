using System.Threading.Tasks;
using Aevatar.BlobStorings;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
    private readonly BlobStoringOptions _blobStoringOptions;

    public BlobStoringController(IBlobContainer blobContainer, IOptionsSnapshot<BlobStoringOptions> blobStoringOptions)
    {
        _blobContainer = blobContainer;
        _blobStoringOptions = blobStoringOptions.Value;
    }

    [HttpPost]
    public async Task SaveAsync([FromForm] SaveBlobInput input)
    {
        var file = input.File.OpenReadStream();
        if (file.Length > _blobStoringOptions.MaxSizeBytes)
        {
            throw new UserFriendlyException(
                $"The file is too large, with a maximum of {_blobStoringOptions.MaxSizeBytes} bytes.");
        }

        await _blobContainer.SaveAsync(input.File.FileName, input.File.OpenReadStream(), true);
    }
}