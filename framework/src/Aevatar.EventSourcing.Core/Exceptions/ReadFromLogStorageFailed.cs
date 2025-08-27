using Orleans;
using Orleans.EventSourcing.Common;

namespace Aevatar.EventSourcing.Core.Exceptions;

[Serializable]
[GenerateSerializer]
public sealed class ReadFromLogStorageFailed : PrimaryOperationFailed
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"read logs from storage failed: caught {Exception.GetType().Name}: {Exception.Message}";
    }
}