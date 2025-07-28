namespace Aevatar.Silo.IdGeneration
{
    /// <summary>
    /// Interface for generating deterministic IDs
    /// </summary>
    public interface IDeterministicIdGenerator
    {
        /// <summary>
        /// Creates a deterministic GUID based on a string input
        /// </summary>
        Guid CreateDeterministicGuid(string input);
    }
} 