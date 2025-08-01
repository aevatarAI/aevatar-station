using System.Collections.Generic;

namespace Aevatar.Options;

public class SystemLLMConfigOptions
{
    public List<string> SystemLLMConfigs { get; set; } = new() { "OpenAI", "DeepSeek", "AzureOpenAI", "AzureOpenAIEmbeddings", "OpenAIEmbeddings" };
} 