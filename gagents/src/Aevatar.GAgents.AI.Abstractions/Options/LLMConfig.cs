using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Orleans;

namespace Aevatar.GAgents.AI.Options;

[GenerateSerializer]
public class LLMConfig : LLMProviderConfig
{
    [Id(0)] public string ModelName { get; set; } = string.Empty;

    [Id(1)] public string Endpoint { get; set; } = string.Empty;

    [Id(2)] public string ApiKey { get; set; } = string.Empty;

    [Id(3)] public Dictionary<string, object>? Memo { get; set; } = null;
    
    [Id(4)] public int NetworkTimeoutInSeconds { get; set; } = 100;

    public bool Equal(LLMConfig other)
    {
        return ProviderEnum == other.ProviderEnum && ModelIdEnum == other.ModelIdEnum && ModelName == other.ModelName &&
               Endpoint == other.Endpoint && ApiKey == other.ApiKey && NetworkTimeoutInSeconds == other.NetworkTimeoutInSeconds;
    }
}