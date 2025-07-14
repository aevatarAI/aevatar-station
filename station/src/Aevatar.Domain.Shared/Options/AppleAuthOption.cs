using System.Collections.Generic;

namespace Aevatar.Options;

public class AppleAuthOption
{
    public Dictionary<string, string> RedirectUrls { get; set; } = new();
}