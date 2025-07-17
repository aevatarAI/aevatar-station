using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Aevatar.BlobStorings;

public interface IThumbnailService
{
    Task<SaveBlobResponse> SaveWithThumbnailsAsync(IFormFile file, string fileName);
    Task<List<ThumbnailInfo>> GenerateThumbnailsAsync(Stream imageStream, string baseFileName, string fileExtension);
    bool IsImageFile(string fileName);
} 