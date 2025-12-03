namespace Aevatar.Core.Abstractions;

/// <summary>
/// Interface for grains that can be activated.
/// </summary>
public interface IActivatable
{
    /// <summary>
    /// Activates the grain and initializes its core functionality.
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    Task ActivateAsync();
}
