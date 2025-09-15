using System.Collections.Generic;

namespace Aevatar.Options;

public class SystemLLMConfigDto
{
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> BestFor { get; set; } = new();
    public string Speed { get; set; } = string.Empty;
}