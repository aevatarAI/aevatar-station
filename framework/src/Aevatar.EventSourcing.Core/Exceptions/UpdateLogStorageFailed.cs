using Orleans;
using Orleans.EventSourcing.Common;

namespace Aevatar.EventSourcing.Core.Exceptions;

[Serializable]
[GenerateSerializer]
public sealed class UpdateLogStorageFailed : PrimaryOperationFailed
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"write logs to storage failed: caught {Exception.GetType().Name}: {Exception.Message}";
    }
}