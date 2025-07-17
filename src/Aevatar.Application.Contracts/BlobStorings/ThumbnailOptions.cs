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
    public string Name { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public ResizeMode ResizeMode { get; set; } = ResizeMode.Max;
}

public enum ResizeMode
{
    Max,     // 保持比例，最大边不超过指定尺寸
    Crop,    // 裁剪到指定尺寸
    Stretch  // 拉伸到指定尺寸
} 