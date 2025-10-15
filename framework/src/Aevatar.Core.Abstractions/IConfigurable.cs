using Orleans.Concurrency;

namespace Aevatar.Core.Abstractions;

/// <summary>
/// Interface for grains that can be configured.
/// </summary>
public interface IConfigurable
{
    /// <summary>
    /// Configures the grain with the provided configuration.
    /// </summary>
    /// <param name="configuration">The configuration object to apply</param>
    /// <returns>Task representing the async operation</returns>
    Task ConfigAsync(ConfigurationBase configuration);

    /// <summary>
    /// Get the type of configuration this grain uses.
    /// </summary>
    /// <returns>The type of configuration this grain uses</returns>
    [ReadOnly]
    Task<Type?> GetConfigurationTypeAsync();
}
