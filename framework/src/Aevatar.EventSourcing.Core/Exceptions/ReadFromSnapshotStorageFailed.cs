using Orleans;
using Orleans.EventSourcing.Common;

namespace Aevatar.EventSourcing.Core.Exceptions;

[Serializable]
[GenerateSerializer]
public sealed class ReadFromSnapshotStorageFailed : PrimaryOperationFailed
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"read state from snapshot storage failed: caught {Exception.GetType().Name}: {Exception.Message}";
    }
}