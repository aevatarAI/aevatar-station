using System.Collections.Generic;
using System.Threading.Tasks;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AI.Abstractions.Configuration;

/// <summary>
/// Interface for LLM provider configuration
/// Focuses on connection and authentication concerns
/// </summary>
public interface ILLMProviderConfig
{
    /// <summary>
    /// The provider type using existing enum
    /// </summary>
    LLMProviderEnum Provider { get; }
    
    /// <summary>
    /// Gets the endpoint URL for this provider
    /// </summary>
    Task<string> GetEndpointAsync();
    
    /// <summary>
    /// Gets authentication headers required by this provider
    /// </summary>
    Task<Dictionary<string, string>> GetAuthHeadersAsync(string scopeId);
    
    /// <summary>
    /// Validates provider-specific configuration
    /// </summary>
    bool IsValid();
}
