using System.Collections.Generic;

namespace Aevatar.BlobStorings;

public class ThumbnailOptions
{
    public List<ThumbnailSize> Sizes { get; set; } = new();
    public int Quality { get; set; } = 85;
    public bool EnableThumbnail { get; set; } = true;
    public string Format { get; set; } = "webp"; // Default to WebP for better compression
}

public class ThumbnailSize
{
    public int Width { get; set; }
    public int Height { get; set; }
    public ResizeMode ResizeMode { get; set; } = ResizeMode.Max;

    public string GetSizeName()
    {
        return $"{Width}-{Height}px";
    }
}

public enum ResizeMode
{
    Max,     // Generate Proportionally Scaled Thumbnails with Maximum Edge Constraint
    Crop,    // Crop to Specified Dimensions
    Stretch  // Stretch to Specified Dimensions
} 