using System.Collections.Generic;
using Aevatar.GAgents.AI.Options;

namespace Aevatar.GAgents.AI.Abstractions.Configuration;

/// <summary>
/// Interface for LLM model configuration
/// Focuses on model parameters and behavior
/// </summary>
public interface ILLMModelConfig
{
    /// <summary>
    /// The model type using existing enum
    /// </summary>
    ModelIdEnum Model { get; }
    
    /// <summary>
    /// Gets model-specific parameters for API calls
    /// </summary>
    Dictionary<string, object> GetParameters();
    
    /// <summary>
    /// Gets provider-specific model identifier
    /// </summary>
    string GetModelIdentifier();
    
    /// <summary>
    /// Validates model-specific configuration
    /// </summary>
    bool IsValid();
}
