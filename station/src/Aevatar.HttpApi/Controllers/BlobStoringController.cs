using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.Application.Constants;
using Aevatar.Application.Contracts.Services;
using Aevatar.BlobStorings;
using Aevatar.Extensions;
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
    private readonly ILocalizationService _localizationService;


    public BlobStoringController(IBlobContainer blobContainer, IOptionsSnapshot<BlobStoringOptions> blobStoringOptions, ILocalizationService localizationService)
    {
        _blobContainer = blobContainer;
        _blobStoringOptions = blobStoringOptions.Value;
        _localizationService = localizationService;
    }

    [HttpPost]
    public async Task SaveAsync([FromForm] SaveBlobInput input)
    {
        var file = input.File.OpenReadStream();
        if (file.Length > _blobStoringOptions.MaxSizeBytes)
        {
            var language = HttpContext.GetGodGPTLanguage();
            var parameters = new Dictionary<string, string>
            {
                ["MaxSizeBytes"] = _blobStoringOptions.MaxSizeBytes.ToString()
            };
            var localizedMessage = _localizationService.GetLocalizedException(ExceptionMessageKeys.FileTooLarge, language, parameters);

            throw new UserFriendlyException(localizedMessage);
        }

        await _blobContainer.SaveAsync(input.File.FileName, input.File.OpenReadStream(), true);
    }
}