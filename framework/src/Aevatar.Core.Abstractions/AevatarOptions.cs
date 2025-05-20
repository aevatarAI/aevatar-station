namespace Aevatar.Core.Abstractions;

public class AevatarOptions
{
    public string StreamNamespace { get; set; } = "Aevatar";
    public string StateProjectionStreamNamespace { get; set; } = "AevatarStateProjection";

    public string BroadcastStreamNamespace { get; set; } = "AevatarBroadcast";
    //public int ElasticSearchProcessors { get; set; } = 10;
}