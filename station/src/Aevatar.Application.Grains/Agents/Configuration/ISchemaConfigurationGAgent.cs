using System.Threading.Tasks;
using Aevatar.Core.Abstractions;
using Aevatar.Schema;
using Orleans;

namespace Aevatar.Application.Grains.Agents.Configuration;

/// <summary>
/// Schema configuration grain interface for providing dynamic dropdown context from silo configuration
/// </summary>
public interface ISchemaConfigurationGAgent : IStateGAgent<SchemaConfigurationGAgentState>, IGrainWithStringKey
{
    /// <summary>
    /// Get dynamic dropdown context containing AI model configurations from silo's SystemLLMConfigOptions
    /// </summary>
    /// <returns>DynamicDropDownContext with AI model configurations</returns>
    Task<DynamicDropDownContext> GetSchemaContextAsync();
}
