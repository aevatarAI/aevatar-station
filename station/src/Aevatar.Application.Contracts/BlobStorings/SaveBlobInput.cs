using Microsoft.AspNetCore.Http;

namespace Aevatar.BlobStorings;

public class SaveBlobInput
{
    public IFormFile File { get; set; }
}