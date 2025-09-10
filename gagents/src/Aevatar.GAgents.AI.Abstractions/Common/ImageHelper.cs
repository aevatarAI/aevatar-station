using System.IO;

namespace Aevatar.GAgents.AI.Common;

public class ImageHelper
{
    public static string GetMineType(string name)
    {
        var extension = Path.GetExtension(name).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
             _ => "image/jpeg" // Default fallback
        };
    }
}