using System.Collections.Generic;

namespace Aevatar.BlobStorings;

public class SaveBlobResponse
{
    public string OriginalFileName { get; set; } = string.Empty;
    public List<ThumbnailInfo> Thumbnails { get; set; } = new();
    public long OriginalSize { get; set; }
}

public class ThumbnailInfo
{
    public string FileName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSize { get; set; }
} 