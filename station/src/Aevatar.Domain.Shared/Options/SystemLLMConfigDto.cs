using System.Collections.Generic;

namespace Aevatar.Options;

public class SystemLLMConfigDto
{
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Strengths { get; set; } = string.Empty;
    public string BestFor { get; set; } = string.Empty;
    public string Speed { get; set; } = string.Empty;
}