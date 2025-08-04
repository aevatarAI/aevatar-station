using System.Collections.Generic;

namespace Aevatar.Options;

public class AgentDefaultValuesOptions
{
    public List<string> SystemLLMConfigs { get; set; } = new () { "OpenAI", "DeepSeek","AzureOpenAI","AzureOpenAIEmbeddings","OpenAIEmbeddings" };
}
