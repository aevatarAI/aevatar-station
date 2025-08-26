using System.Collections.Generic;

namespace Aevatar.Options;

public class SystemLLMMetaInfoOptions
{
    public List<SystemLLMConfigDto> SystemLLMConfigs { get; set; } = new ()
    {
        new ()
        {
            Name = "OpenAI",
            Provider = "OpenAI",
            Type = "GPT-4",
            Strengths = new List<string> { "Multi-modal capabilities", "Advanced reasoning", "Code generation" },
            BestFor = new List<string> { "Complex conversations", "Programming tasks", "Creative writing" },
            Speed = "Fast"
        },
        new ()
        {
            Name = "BytePlus",
            Provider = "BytePlus",
            Type = "Doubao Pro",
            Strengths = new List<string> { "Chinese language support", "Fast inference", "Cost-effective" },
            BestFor = new List<string> { "Chinese content", "Real-time applications", "Budget projects" },
            Speed = "Very Fast"
        },
        new ()
        {
            Name = "DeepSeek",
            Provider = "DeepSeek",
            Type = "DeepSeek-R1",
            Strengths = new List<string> { "Mathematical reasoning", "Scientific analysis", "Research capabilities" },
            BestFor = new List<string> { "Research tasks", "Data analysis", "Technical documentation" },
            Speed = "Medium"
        }
    };
}

public class SystemLLMConfigDto
{
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> BestFor { get; set; } = new();
    public string Speed { get; set; } = string.Empty;
}