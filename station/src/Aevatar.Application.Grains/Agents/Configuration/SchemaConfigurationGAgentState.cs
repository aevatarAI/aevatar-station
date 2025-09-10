using Aevatar.Core.Abstractions;
using Orleans;

namespace Aevatar.Application.Grains.Agents.Configuration;

/// <summary>
/// State for Schema Configuration Agent
/// Simple state for configuration reading agent - minimal state required
/// </summary>
[GenerateSerializer]
public class SchemaConfigurationGAgentState : StateBase
{
    // This agent only reads configuration, no complex state needed
}
