using Orleans;

namespace Aevatar.GAgents.AI.Options;

[GenerateSerializer]
public class StreamingConfig
{
    [Id(0)] public long TimeOutInternal { get; set; } //ms
    [Id(1)] public int BufferingSize { get; set; }
    public bool Equals(StreamingConfig other)
    {
        return TimeOutInternal == other.TimeOutInternal && BufferingSize == other.BufferingSize;
    }
}