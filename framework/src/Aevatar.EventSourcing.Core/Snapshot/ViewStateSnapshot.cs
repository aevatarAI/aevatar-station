using Orleans;

namespace Aevatar.EventSourcing.Core.Snapshot;

[Serializable]
[GenerateSerializer]
public sealed class ViewStateSnapshot<TLogView> : IGrainState<ViewStateSnapshotWithMetadata<TLogView>>
    where TLogView : class, new()
{
    [Id(0)] public string ETag { get; set; } = null!;
    [Id(1)] public bool RecordExists { get; set; }
    [Id(2)] public ViewStateSnapshotWithMetadata<TLogView> State { get; set; } = new();

    public ViewStateSnapshot()
    {
        
    }

    public ViewStateSnapshot(TLogView initialView)
    {
        State = new ViewStateSnapshotWithMetadata<TLogView>(initialView);
    }

    public override string ToString()
    {
        return $"v{State.SnapshotVersion} Flags={State.WriteVector} ETag={ETag} Data={State.Snapshot}";
    }
}