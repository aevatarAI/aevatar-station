using System.Collections.Generic;

namespace Aevatar.BlobStorings;

public class CanUploadImageResponseDto
{
    public bool CanUpload { get; set; }
    public string Reason { get; set; } = string.Empty;
} 