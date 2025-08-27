using Orleans;
using Orleans.EventSourcing.Common;

namespace Aevatar.EventSourcing.Core.Exceptions;

[Serializable]
[GenerateSerializer]
public sealed class UpdateSnapshotStorageFailed : PrimaryOperationFailed
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"write state to snapshot storage failed: caught {Exception.GetType().Name}: {Exception.Message}";
    }
}